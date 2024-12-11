using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lox.Interpreter.Exceptions;
using Lox.Interpreter.Scanner;
using Lox.Interpreter.Syntax.Statements;
using Lox.Interpreter.Utils;

namespace Lox.Interpreter;

/// <summary>
/// Lox source file parser
/// </summary>
public sealed partial class LoxParser
{
    #region Fields
    /// <summary>
    /// Tokens for an empty source
    /// </summary>
    private static readonly Token[] EmptySourceTokens = [new(TokenType.EOF, 1)];

    private int currentIndex;
    private Token[] sourceTokens = null!;
    private List<LoxStatement> statements = null!;
    #endregion

    #region Properties
    /// <summary>
    /// Source tokens of the program held within the parser
    /// </summary>
    public ReadOnlyCollection<Token> SourceTokens => new(this.sourceTokens);

    /// <summary>
    /// If the program held in the parser has been parsed or not
    /// </summary>
    public bool IsParsed { get; private set; }

    /// <summary>
    /// Code statements of the parsed Lox program
    /// </summary>
    public ReadOnlyCollection<LoxStatement> Program { get; private set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new empty parser
    /// </summary>
    public LoxParser() : this([]) { }

    /// <summary>
    /// Creates a new parser from the specified tokens
    /// </summary>
    /// <param name="sourceTokens">Source tokens</param>
    public LoxParser(IReadOnlyCollection<Token> sourceTokens) => UpdateSourceTokens(sourceTokens);
    #endregion

    #region Methods
    /// <summary>
    /// Parses the Lox program and returns the code statements
    /// </summary>
    /// <returns>Code statements of the parsed Lox program</returns>
    public async Task<ReadOnlyCollection<LoxStatement>> ParseAsync() => await Task.Run(Parse);

    /// <summary>
    /// Parses the Lox program and returns the code statements
    /// </summary>
    /// <returns>Code statements of the parsed Lox program</returns>
    public ReadOnlyCollection<LoxStatement> Parse()
    {
        if (this.IsParsed) return this.Program;

        ReadOnlySpan<Token> tokens = this.sourceTokens;

        while (!CheckEOF(tokens))
        {
            LoxStatement? statement = ParseDeclaration(tokens);
            if (statement is not null)
            {
                this.statements.Add(statement);
            }
        }

        this.IsParsed = true;
        return this.Program;
    }

    /// <summary>
    /// Updates the source tokens for the current parser
    /// </summary>
    /// <param name="newSourceTokens">New source tokens</param>
    public void UpdateSourceTokens(IReadOnlyCollection<Token> newSourceTokens)
    {
        this.sourceTokens = newSourceTokens.Count > 0 ? newSourceTokens.ToArray() : EmptySourceTokens;
        this.currentIndex = 0;
        this.IsParsed     = this.sourceTokens.Length is 1;
        this.statements   = new List<LoxStatement>(!this.IsParsed ? 100 : 0);
        this.Program      = new ReadOnlyCollection<LoxStatement>(!this.IsParsed ? this.statements : []);
    }

    /// <summary>
    /// Resets the parser
    /// </summary>
    public void Reset()
    {
        this.currentIndex = 0;
        this.IsParsed     = this.sourceTokens.Length is 1;
        this.statements.Clear();
    }

    /// <summary>
    /// Synchronizes the parser back to a valid state
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    private void Synchronize(in ReadOnlySpan<Token> tokens)
    {
        for (Token currentToken = MoveNextToken(tokens); !currentToken.IsEOF; currentToken = MoveNextToken(tokens))
        {
            if (currentToken.Type is TokenType.SEMICOLON)
            {
                return;
            }

            if (currentToken.IsStatementStart)
            {
                this.currentIndex--;
                return;
            }
        }
    }

    /// <summary>
    /// Checks if the current token matches the specified token type
    /// </summary>
    /// <param name="tokens">Source token span</param>
    /// <param name="tokenType">Token type to match</param>
    /// <param name="matchedToken">The matched token, if any</param>
    /// <returns><see langword="true"/> if a token was matched, otherwise <see langword="false"/></returns>
    private bool TryMatchToken(in ReadOnlySpan<Token> tokens, in TokenType tokenType, out Token matchedToken)
    {
        if (CheckCurrentToken(tokens, tokenType))
        {
            matchedToken = MoveNextToken(tokens);
            return true;
        }

        matchedToken = default;
        return false;
    }

    /// <summary>
    /// Checks if the current token matches any of the specified types
    /// </summary>
    /// <param name="tokens">Source token span</param>
    /// <param name="validTypes">Valid types to match</param>
    /// <param name="matchedToken">The matched token, if any</param>
    /// <returns><see langword="true"/> if a token was matched, otherwise <see langword="false"/></returns>
    private bool TryMatchToken(in ReadOnlySpan<Token> tokens, in ReadOnlySpan<TokenType> validTypes, out Token matchedToken)
    {
        foreach (TokenType type in validTypes)
        {
            if (!CheckCurrentToken(tokens, type)) continue;

            matchedToken = MoveNextToken(tokens);
            return true;
        }

        matchedToken = default;
        return false;
    }

    /// <summary>
    /// Ensures the next token is of the specified type, otherwise throws
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <param name="requiredType">The required type the next token should be</param>
    /// <param name="errorMessage">Invalid token type error message</param>
    /// <returns>Then next token</returns>
    /// <exception cref="LoxParseException">If the current token did not match <paramref name="requiredType"/></exception>
    private Token EnsureNextToken(in ReadOnlySpan<Token> tokens, in TokenType requiredType, string errorMessage)
    {
        Token currentToken = CurrentToken(tokens);
        if (currentToken.Type != requiredType)
        {
            ProduceException(currentToken, errorMessage);
        }

        this.currentIndex++;
        return currentToken;
    }

    /// <summary>
    /// Return the current token then move to the next
    /// </summary>
    /// <param name="tokens">Source token spans</param>
    /// <returns>The current source token</returns>
    private Token MoveNextToken(in ReadOnlySpan<Token> tokens)
    {
        Token currentToken = CurrentToken(tokens);
        if (currentToken.IsEOF) return currentToken;

        this.currentIndex++;
        return currentToken;
    }

    /// <summary>
    /// Check if the current token matches the specified type
    /// </summary>
    /// <param name="tokens">Source token span</param>
    /// <param name="type">Token type to match</param>
    /// <returns><see langword="true"/> if the current token matches <paramref name="type"/>, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckCurrentToken(in ReadOnlySpan<Token> tokens, in TokenType type) => CurrentToken(tokens).Type == type;

    /// <summary>
    /// Checks if the current token is EOF
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns><see langword="true"/> if the current token is <see cref="TokenType.EOF"/>, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckEOF(in ReadOnlySpan<Token> tokens) => CurrentToken(tokens).IsEOF;

    /// <summary>
    /// Returns the current token without moving to the next
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <returns>The current token</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token CurrentToken(in ReadOnlySpan<Token> tokens) => tokens[this.currentIndex];

    /// <summary>
    /// Peek at a given token without changing the current index
    /// </summary>
    /// <param name="tokens">Source tokens span</param>
    /// <param name="offset">Peek offset, defaults to 1</param>
    /// <returns>The token at the specified offset</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token PeekToken(in ReadOnlySpan<Token> tokens, in int offset = 1)
    {
        // Clamp to final index (EOF)
        int peekIndex = this.currentIndex + offset;
        return peekIndex < tokens.Length ? tokens[peekIndex] : tokens[^1];
    }
    #endregion

    #region Static methods
    /// <summary>
    /// Produces and logs a new <see cref="LoxParseException"/>
    /// </summary>
    /// <param name="token">Token causing the exception</param>
    /// <param name="message">Error message</param>
    /// <exception cref="LoxParseException">The generated parsing exception</exception>
    [DoesNotReturn]
    private static void ProduceException(in Token token, string message)
    {
        LoxErrorUtils.ReportParseError(token, message);
        throw new LoxParseException(message);
    }
    #endregion
}
