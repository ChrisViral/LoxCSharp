﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lox.Common;
using Lox.Common.Exceptions;
using Lox.VM.Bytecode;
using Lox.VM.Runtime;
using Lox.VM.Scanner;
using Lox.VM.Utils;

namespace Lox.VM.Compiler;

public sealed partial class LoxCompiler : IDisposable
{
    private readonly LoxScanner scanner = new();
    private readonly Dictionary<string, int> interned = new(StringComparer.Ordinal);
    private Token currentToken;
    private Token previousToken;

    public LoxChunk Chunk { get; } = [];

    public bool IsDisposed { get; private set; }

    public bool HadCompilationErrors { get; private set; }

    /// <summary>
    /// Checks if the current token is EOF
    /// </summary>
    /// <returns><see langword="true"/> if the current token is <see cref="TokenType.EOF"/>, otherwise <see langword="false"/></returns>
    private bool IsEOF
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.currentToken.IsEOF;
    }

    ~LoxCompiler() => Dispose();

    public bool Compile(string source, [MaybeNullWhen(false)] out Dictionary<string, LoxValue> internedStrings)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);

        InitializeCompilation();
        using LoxScanner.PinScope _ = this.scanner.OpenPinScope(source);
        try
        {
            MoveNextToken();
            while (!this.IsEOF)
            {
                ParseDeclaration();
            }
            EnsureNextToken(TokenType.EOF, "Expected end of file.");
            EndCompilation(out internedStrings);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            this.HadCompilationErrors = true;
            internedStrings           = null;
        }

        return !this.HadCompilationErrors;
    }

    private void InitializeCompilation()
    {
        this.Chunk.Clear();
        this.previousToken        = default;
        this.currentToken         = default;
        this.HadCompilationErrors = false;
    }

    private void EndCompilation(out Dictionary<string, LoxValue> internedStrings)
    {
        EmitOpcode(LoxOpcode.RETURN);

        // Copy interned strings to a new dictionary for the VM
        internedStrings = new Dictionary<string, LoxValue>(this.interned.Count, StringComparer.Ordinal);
        foreach ((string value, int index) in this.interned)
        {
            internedStrings.Add(value, this.Chunk.GetConstant(index));
        }
        this.interned.Clear();

        #if DEBUG_PRINT
        if (!this.HadCompilationErrors)
        {
            BytecodePrinter.PrintChunk(this.Chunk, "code");
        }
        #endif
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.IsDisposed) return;

        this.scanner.Dispose();
        GC.SuppressFinalize(this);
        this.IsDisposed = true;
    }

    #region Methods
    /// <summary>
    /// Requests the next token from the scanner
    /// </summary>
    /// <returns>Next scanned token</returns>
    private Token MoveNextToken()
    {
        this.previousToken = this.currentToken;
        this.scanner.ScanNextToken(out this.currentToken);
        if (CheckCurrentToken(TokenType.ERROR))
        {
            Token firstError = this.currentToken;
            do
            {
                this.scanner.ScanNextToken(out this.currentToken);
            }
            while (CheckCurrentToken(TokenType.ERROR));

            ReportParseError(firstError);
        }

        return this.currentToken;
    }

    /// <summary>
    /// Checks if the current token matches the specified token type
    /// </summary>
    /// <param name="tokenType">Token type to match</param>
    /// <param name="matchedToken">The matched token, if any</param>
    /// <returns><see langword="true"/> if a token was matched, otherwise <see langword="false"/></returns>
    private bool TryMatchToken(TokenType tokenType, out Token matchedToken)
    {
        if (CheckCurrentToken(tokenType))
        {
            matchedToken = MoveNextToken();
            return true;
        }

        matchedToken = default;
        return false;
    }

    /// <summary>
    /// Checks if the current token matches any of the specified types
    /// </summary>
    /// <param name="matchedToken">The matched token, if any</param>
    /// <param name="validTypes">Valid types to match</param>
    /// <returns><see langword="true"/> if a token was matched, otherwise <see langword="false"/></returns>
    private bool TryMatchToken(out Token matchedToken, params ReadOnlySpan<TokenType> validTypes)
    {
        foreach (TokenType type in validTypes)
        {
            if (!CheckCurrentToken(type)) continue;

            matchedToken = MoveNextToken();
            return true;
        }

        matchedToken = default;
        return false;
    }

    /// <summary>
    /// Ensures the next token is of the specified type, otherwise throws
    /// </summary>
    /// <param name="requiredType">The required type the next token should be</param>
    /// <param name="errorMessage">Invalid token type error message</param>
    /// <returns>Then next token</returns>
    /// <exception cref="LoxParseException">If the current token did not match <paramref name="requiredType"/></exception>
    private Token EnsureNextToken(TokenType requiredType, string errorMessage)
    {
        if (!CheckCurrentToken(requiredType))
        {
            ReportCompileError(this.currentToken, errorMessage);
        }

        return MoveNextToken();
    }

    /// <summary>
    /// Synchronizes the parser back to a valid state
    /// </summary>
    private void Synchronize()
    {
        for (MoveNextToken(); !this.IsEOF; MoveNextToken())
        {
            if (this.previousToken.Type is TokenType.SEMICOLON)
            {
                return;
            }

            if (this.currentToken.IsStatementStart)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Check if the current token matches the specified type
    /// </summary>
    /// <param name="type">Token type to match</param>
    /// <returns><see langword="true"/> if the current token matches <paramref name="type"/>, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckCurrentToken(TokenType type) => this.currentToken.Type == type;

    /// <summary>
    /// Reports a parsing error from the scanner at the current token
    /// </summary>
    /// <exception cref="LoxParseException">Always throws</exception>
    [DoesNotReturn]
    private void ReportParseError() => ReportParseError(this.currentToken);

    /// <summary>
    /// Reports a parsing error from the scanner at the given token
    /// </summary>
    /// <param name="token">Error token</param>
    /// <exception cref="LoxParseException">Always throws</exception>
    [DoesNotReturn]
    private void ReportParseError(in Token token) => ReportError($"[line {token.Line}] Error: {token.Lexeme}", token.Line);

    /// <summary>
    /// Reports a compilation error at the provided token
    /// </summary>
    /// <param name="token">Error token</param>
    /// <param name="message">Error message</param>
    /// <exception cref="LoxParseException">Always throws</exception>
    [DoesNotReturn]
    private void ReportCompileError(in Token token, string message) => ReportError($"Error at {(token.IsEOF ? "end" : token.Lexeme)}: {message}", token.Line);

    /// <summary>
    /// Reports an error message
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="line">Error line</param>
    /// <exception cref="LoxParseException">Always throws</exception>
    [DoesNotReturn]
    private void ReportError(string message, int line)
    {
        this.HadCompilationErrors = true;
        Console.Error.WriteLine($"[line {line}] {message}");
        throw new LoxParseException(message);
    }
    #endregion
}
