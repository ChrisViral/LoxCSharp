﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lox.Common;
using Lox.Common.Exceptions;
using Lox.VM.Bytecode;
using Lox.VM.Runtime;
using Lox.VM.Scanner;
using Lox.VM.Utils;

namespace Lox.VM.Compiler;

/// <summary>
/// Lox bytecode compiler
/// </summary>
public sealed partial class LoxCompiler : IDisposable
{
    /// <summary>
    /// Local variable defined state
    /// </summary>
    private enum State
    {
        UNDEFINED,
        DEFINED
    }

    /// <summary>
    /// Local declaration
    /// </summary>
    /// <param name="Identifier">Local identifier</param>
    /// <param name="Index">Local stack index</param>
    /// <param name="State">Local defined state</param>
    private readonly record struct Local(in Token Identifier, int Index, State State);

    #region Fields
    private readonly LoxScanner scanner = new();
    private readonly Dictionary<string, ushort> interned = new(StringComparer.Ordinal);
    private readonly List<Dictionary<string, Local>> localsPerScope = [];
    private int totalLocalsCount;
    private int scopeDepth;
    private Token currentToken;
    private Token previousToken;
    #endregion

    #region Properties
    /// <summary>
    /// Compiled bytecode chunk
    /// </summary>
    public LoxChunk Chunk { get; } = [];

    /// <summary>
    /// If the compiler is disposed
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// If any compilation errors occured
    /// </summary>
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

    /// <summary>
    /// If we're currently in the global scope or not
    /// </summary>
    private bool IsGlobalScope
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.scopeDepth is 0;
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Compiler finalizer
    /// </summary>
    ~LoxCompiler() => Dispose();
    #endregion

    #region Methods
    /// <summary>
    /// Compiles a given source code string
    /// </summary>
    /// <param name="source">Source code to compile</param>
    /// <param name="internedStrings">A dictionary of interned strings from the compiler</param>
    /// <returns><see langword="true"/> If compilation was successful, otherwise <see langword="false"/></returns>
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

    /// <summary>
    /// Initializes the compilation process
    /// </summary>
    private void InitializeCompilation()
    {
        this.Chunk.Clear();
        this.totalLocalsCount     = 0;
        this.scopeDepth           = 0;
        this.previousToken        = default;
        this.currentToken         = default;
        this.HadCompilationErrors = false;
    }

    /// <summary>
    /// Cleans up the compilation process
    /// </summary>
    /// <param name="internedStrings">Dictionary of strings interned by the compiler</param>
    private void EndCompilation(out Dictionary<string, LoxValue> internedStrings)
    {
        EmitOpcode(LoxOpcode.RETURN);

        // Copy interned strings to a new dictionary for the VM
        internedStrings = new Dictionary<string, LoxValue>(this.interned.Count, StringComparer.Ordinal);
        foreach ((string value, ushort index) in this.interned)
        {
            internedStrings.Add(value, this.Chunk.GetConstant(index));
        }

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
        this.Chunk.Dispose();
        GC.SuppressFinalize(this);
        this.IsDisposed = true;
    }

    /// <summary>
    /// Requests the next token from the scanner
    /// </summary>
    /// <returns>Next scanned token</returns>
    private Token MoveNextToken()
    {
        this.previousToken = this.currentToken;
        this.scanner.ScanNextToken(out this.currentToken);

        // ReSharper disable once InvertIf
        if (CheckCurrentToken(TokenType.ERROR))
        {
            Token firstError = this.currentToken;
            do
            {
                this.scanner.ScanNextToken(out this.currentToken);
            }
            while (CheckCurrentToken(TokenType.ERROR));

            ReportErrorToken(firstError);
        }

        return this.previousToken;
    }

    /// <summary>
    /// Checks if the current token matches the specified token type
    /// </summary>
    /// <param name="tokenType">Token type to match</param>
    /// <returns><see langword="true"/> if a token was matched, otherwise <see langword="false"/></returns>
    private bool TryMatchToken(TokenType tokenType)
    {
        // ReSharper disable once InvertIf
        if (CheckCurrentToken(tokenType))
        {
            MoveNextToken();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the current token matches the specified token type
    /// </summary>
    /// <param name="tokenType">Token type to match</param>
    /// <param name="matchedToken">The matched token, if any</param>
    /// <returns><see langword="true"/> if a token was matched, otherwise <see langword="false"/></returns>
    private bool TryMatchToken(TokenType tokenType, out Token matchedToken)
    {
        // ReSharper disable once InvertIf
        if (CheckCurrentToken(tokenType))
        {
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
        while (!this.IsEOF)
        {
            Token current = MoveNextToken();
            if (this.previousToken.Type is TokenType.SEMICOLON)
            {
                return;
            }

            if (current.IsStatementStart)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Opens a new scope level
    /// </summary>
    private void OpenScope()
    {
        this.scopeDepth++;
        if (this.localsPerScope.Count <= this.scopeDepth)
        {
            this.localsPerScope.Add(new Dictionary<string, Local>(StringComparer.Ordinal));
        }
    }

    /// <summary>
    /// Closes one scope level
    /// </summary>
    private void CloseScope()
    {
        this.scopeDepth--;
        Dictionary<string, Local> scope = this.localsPerScope[this.scopeDepth];
        int poppedCount = scope.Count;

        switch (poppedCount)
        {
            case 0:
                return;

            case 1:
                this.totalLocalsCount--;
                scope.Clear();
                EmitOpcode(LoxOpcode.POP);
                break;

            default:
                this.totalLocalsCount -= poppedCount;
                scope.Clear();
                EmitOpcode(LoxOpcode.POPN, (ushort)poppedCount);
                break;
        }
    }

    /// <summary>
    /// Emits the given opcode to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to emit</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EmitOpcode(LoxOpcode opcode) => this.Chunk.AddOpcode(opcode, this.previousToken.Line);

    /// <summary>
    /// Emits the given opcode to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to emit</param>
    /// <param name="token">Token to emit the opcode for</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EmitOpcode(LoxOpcode opcode, in Token token) => this.Chunk.AddOpcode(opcode, token.Line);

    /// <summary>
    /// Emits the given opcode to the chunk, along with an operand, if the operand breaks the 8bit limit, the opcode is moved up to its 16bit version
    /// </summary>
    /// <param name="opcode">Opcode to emit</param>
    /// <param name="operand">Opcode operand</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EmitOpcode(LoxOpcode opcode, ushort operand)
    {
        if (operand <= byte.MaxValue)
        {
            this.Chunk.AddOpcode(opcode, (byte)operand, this.previousToken.Line);
        }
        else
        {
            this.Chunk.AddOpcode(opcode + 1, operand, this.previousToken.Line);
        }
    }

    /// <summary>
    /// Emits the given value to the chunk
    /// </summary>
    /// <param name="value">Value to emit</param>
    /// <param name="index">Index the constant was added at</param>
    /// <param name="type">Emitted constant type</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EmitConstant(in LoxValue value, out ushort index, ConstantType type)
    {
        // Check if the constant was successfully added
        if (!this.Chunk.AddConstant(value, this.previousToken.Line, out index, type))
        {
            ReportCompileError(this.currentToken, $"Constant limit ({ushort.MaxValue}) exceeded.");
        }
    }

    /// <summary>
    /// Emits the given string value to the chunk
    /// </summary>
    /// <param name="stringValue">Value to emit</param>
    /// <param name="type">Emitted constant type, defaults to <see cref="ConstantType.CONSTANT"/></param>
    private void EmitStringConstant(ReadOnlySpan<char> stringValue, ConstantType type)
    {
        // Check if we've already added a constant for the same string
        // ReSharper disable once SuggestVarOrType_Elsewhere
        var internedLookup = this.interned.GetAlternateLookup<ReadOnlySpan<char>>();
        if (internedLookup.TryGetValue(stringValue, out ushort index))
        {
            // Refer to existing constant instead
            this.Chunk.AddIndexedConstant(index, this.previousToken.Line, type);
        }
        else
        {
            // Allocate new string constant
            RawString value = RawString.Allocate(stringValue, out IntPtr _);
            EmitConstant(value, out index, type);

            // Keep track of that interned string
            internedLookup[stringValue] = index;
        }
    }

    /// <summary>
    /// Emits a jump instruction and provides the backpatching address
    /// </summary>
    /// <param name="jump">Jump opcode</param>
    /// <returns>The address at which the jump operand should be patched</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int EmitJump(LoxOpcode jump)
    {
        this.Chunk.AddOpcode(jump, ushort.MaxValue, this.previousToken.Line);
        return this.Chunk.Count - 2;
    }

    /// <summary>
    /// Patches a jump operand value
    /// </summary>
    /// <param name="controlFlowToken">Token from which the control flow originated</param>
    /// <param name="address">Operand address</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PatchJump(in Token controlFlowToken, int address)
    {
        int jump = this.Chunk.Count - address;
        if (jump > ushort.MaxValue)
        {
            ReportCompileError(controlFlowToken, $"Maximum jump length ({ushort.MaxValue}) exceeded");
        }

        this.Chunk.PatchJump(address, (ushort)jump);
    }

    /// <summary>
    /// Emits a loop instruction to the given address
    /// </summary>
    /// <param name="controlFlowToken">Token from which the control flow originated</param>
    /// <param name="address">address to jump to</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EmitLoop(in Token controlFlowToken, int address)
    {
        int jump = this.Chunk.Count - address + 1;
        if (jump > ushort.MaxValue)
        {
            ReportCompileError(controlFlowToken, $"Maximum jump length ({ushort.MaxValue}) exceeded");
        }
        this.Chunk.AddOpcode(LoxOpcode.LOOP, (ushort)jump, this.previousToken.Line);
    }

    /// <summary>
    /// Check if the current token matches the specified type
    /// </summary>
    /// <param name="type">Token type to match</param>
    /// <returns><see langword="true"/> if the current token matches <paramref name="type"/>, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckCurrentToken(TokenType type) => this.currentToken.Type == type;

    /// <summary>
    /// Reports a parsing error from the scanner at the given token
    /// </summary>
    /// <param name="token">Error token</param>
    /// <exception cref="LoxParseException">Always throws</exception>
    [DoesNotReturn]
    private void ReportErrorToken(in Token token) => ReportError($"[line {token.Line}] Error: {token.Lexeme}", token.Line);

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
