using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Lox.Runtime.Types;
using Lox.Utils;

namespace Lox.Scanning;

/// <summary>
/// Lox source file scanner
/// </summary>
public sealed class LoxScanner
{
    #region Fields
    /// <summary>
    /// Tokens for an empty source string
    /// </summary>
    private static readonly Token[] EmptyFileTokens = [new(TokenType.EOF, 1)];

    private List<Token> tokensBuffer = null!;
    private int currentLine = 1;
    private int startIndex;
    private int currentIndex;
    #endregion

    #region Properties
    private string source = null!;
    /// <summary>
    /// Scanner source code
    /// </summary>
    /// ReSharper disable once MemberCanBePrivate.Global
    public string Source
    {
        get => this.source;
        set
        {
            if (this.source == value) return;

            this.source       = value;
            this.currentLine  = 1;
            this.currentIndex = 0;
            this.IsTokenized  = string.IsNullOrWhiteSpace(this.source);
            this.tokensBuffer = new List<Token>(this.IsTokenized ? 0 : 100);
            this.Tokens       = new ReadOnlyCollection<Token>(this.IsTokenized ? EmptyFileTokens : this.tokensBuffer);
        }
    }

    /// <summary>
    /// If this scanner has tokenized it's source yet or not
    /// </summary>
    /// ReSharper disable once MemberCanBePrivate.Global
    public bool IsTokenized { get; private set; }

    /// <summary>
    /// Tokens readonly buffer
    /// </summary>
    public ReadOnlyCollection<Token> Tokens { get; private set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new empty Lox <see cref="LoxScanner"/>
    /// </summary>
    public LoxScanner() : this(string.Empty) { }

    /// <summary>
    /// Creates a new Lox <see cref="LoxScanner"/> from the given source code
    /// </summary>
    /// <param name="source">Source code to create the scanner for</param>
    public LoxScanner(string source) => this.Source = source;
    #endregion

    #region Methods
    /// <summary>
    /// Tokenizes the source
    /// </summary>
    /// <returns>The tokenized Lox source code</returns>
    public async Task<ReadOnlyCollection<Token>> TokenizeAsync() => await Task.Run(Tokenize);

    /// <summary>
    /// Tokenizes the source
    /// </summary>
    /// <returns>The tokenized Lox source code</returns>
    /// ReSharper disable once MemberCanBePrivate.Global
    public ReadOnlyCollection<Token> Tokenize()
    {
        if (this.IsTokenized) return this.Tokens;

        ReadOnlySpan<char> sourceSpan = this.Source;
        while (!CheckEOF(sourceSpan))
        {
            this.startIndex = this.currentIndex;
            Token token = ScanNextToken(sourceSpan);
            if (token.Type is not TokenType.NONE)
            {
                this.tokensBuffer.Add(token);
            }
        }

        this.tokensBuffer.Add(new Token(TokenType.EOF, this.currentLine));
        this.IsTokenized = true;
        return this.Tokens;
    }

    /// <summary>
    /// Scans the source and returns the next token from the current point in the source
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <returns>The next token in the source, a token with <see cref="TokenType.NONE"/> if the next token is invalid</returns>
    private Token ScanNextToken(in ReadOnlySpan<char> sourceSpan)
    {
        do
        {
            // Check if the token is implicitly defined
            char currentChar = NextChar(sourceSpan);
            TokenType castedType = (TokenType)currentChar;
            if (EnumUtils.IsDefined(castedType))
            {
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (castedType)
                {
                    // Comment
                    case TokenType.SLASH when MatchNext(sourceSpan, '/'):
                        // Consume entirety of comment then carry on
                        ConsumeUntil(sourceSpan, '\n');
                        continue;

                    // Double character equality operator
                    case TokenType.BANG    when MatchNext(sourceSpan, '='):
                    case TokenType.EQUAL   when MatchNext(sourceSpan, '='):
                    case TokenType.GREATER when MatchNext(sourceSpan, '='):
                    case TokenType.LESS    when MatchNext(sourceSpan, '='):
                        return new Token(castedType + (int)TokenType.EQUALITY, this.currentLine);

                    default:
                        // Simple single character token
                        return new Token(castedType, this.currentLine);
                }
            }

            // Test current token
            switch (currentChar)
            {
                // Strings
                case '"':
                    return TokenizeString(sourceSpan);

                // Numbers
                case char _ when char.IsAsciiDigit(currentChar):
                    return TokenizeNumber(sourceSpan);

                // Keywords and Identifiers
                case char _ when IsWordChar(currentChar):
                    return TokenizeKeywordOrIdentifier(sourceSpan);

                // Linebreaks
                case '\n':
                    this.currentLine++;
                    this.startIndex = this.currentIndex;
                    break;

                // Whitespace
                case char _ when char.IsWhiteSpace(currentChar):
                    this.startIndex = this.currentIndex;
                    break;

                default:
                    LoxErrorUtils.ReportError(this.currentLine, "Unexpected character: " + currentChar);
                    break;
            }
        }
        while (!CheckEOF(sourceSpan));

        return new Token(TokenType.NONE, this.currentLine);
    }

    /// <summary>
    /// Tokenizes a string from the current point in the source
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <returns>The next <see cref="TokenType.STRING"/> token</returns>
    private Token TokenizeString(in ReadOnlySpan<char> sourceSpan)
    {
        // Consume all characters until string ends
        ConsumeUntil(sourceSpan, '"');

        // If we're add the end of the source, the string did not terminate
        if (CheckEOF(sourceSpan))
        {
            LoxErrorUtils.ReportError(this.currentLine, "Unterminated string.");
            return new Token(TokenType.NONE, this.currentLine);
        }

        // Include final quote
        this.currentIndex++;
        ReadOnlySpan<char> stringLexeme  = sourceSpan[this.startIndex..this.currentIndex];
        ReadOnlySpan<char> stringLiteral = stringLexeme[1..^1];

        // Add all included linebreaks to line count
        this.currentLine += stringLiteral.Count('\n');
        return new Token(TokenType.STRING, stringLexeme.ToString(), new LoxValue(stringLiteral.ToString()), this.currentLine);
    }

    /// <summary>
    /// Tokenizes a number from the current point in the source
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <returns>The next <see cref="TokenType.NUMBER"/> token</returns>
    private Token TokenizeNumber(in ReadOnlySpan<char> sourceSpan)
    {
        while (char.IsAsciiDigit(PeekChar(sourceSpan)))
        {
            this.currentIndex++;
        }

        if (!CheckEOF(sourceSpan) && sourceSpan[this.currentIndex] is '.' && char.IsAsciiDigit(PeekNextChar(sourceSpan)))
        {
            this.currentIndex++;
            while (char.IsAsciiDigit(PeekChar(sourceSpan)))
            {
                this.currentIndex++;
            }
        }

        ReadOnlySpan<char> numberLiteral = sourceSpan[this.startIndex..this.currentIndex];
        return new Token(TokenType.NUMBER, numberLiteral.ToString(), new LoxValue(double.Parse(numberLiteral)), this.currentLine);
    }

    /// <summary>
    /// Tokenizes a keyword or identifier from the current point in the source
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <returns>The next <see cref="TokenType.IDENTIFIER"/> or keyword token</returns>
    private Token TokenizeKeywordOrIdentifier(in ReadOnlySpan<char> sourceSpan)
    {
        while (IsIdentifierChar(PeekChar(sourceSpan)))
        {
            this.currentIndex++;
        }

        string word = sourceSpan[this.startIndex..this.currentIndex].ToString();
        TokenType tokenType = word switch
        {
            "nil"    => TokenType.NIL,
            "true"   => TokenType.TRUE,
            "false"  => TokenType.FALSE,
            "and"    => TokenType.AND,
            "or"     => TokenType.OR,
            "if"     => TokenType.IF,
            "else"   => TokenType.ELSE,
            "for"    => TokenType.FOR,
            "while"  => TokenType.WHILE,
            "var"    => TokenType.VAR,
            "fun"    => TokenType.FUN,
            "return" => TokenType.RETURN,
            "print"  => TokenType.PRINT,
            "class"  => TokenType.CLASS,
            "this"   => TokenType.THIS,
            "super"  => TokenType.SUPER,
            _        => TokenType.IDENTIFIER
        };

        return tokenType is TokenType.IDENTIFIER ? new Token(TokenType.IDENTIFIER, word, LoxValue.Invalid, this.currentLine) : new Token(tokenType, this.currentLine);
    }

    /// <summary>
    /// Checks if the next character in the source matches the given char
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <param name="toMatch">Character to match</param>
    /// <returns><see langword="true"/> if the next source char matches <paramref name="toMatch"/>, otherwise <see langword="false"/></returns>
    private bool MatchNext(in ReadOnlySpan<char> sourceSpan, in char toMatch)
    {
        if (CheckEOF(sourceSpan) || sourceSpan[this.currentIndex] != toMatch) return false;

        this.currentIndex++;
        return true;
    }

    /// <summary>
    /// Consumes characters in the source until the specified terminator is found, or the source is fully consumed
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <param name="terminator">Terminator character to stop on</param>
    private void ConsumeUntil(in ReadOnlySpan<char> sourceSpan, in char terminator)
    {
        while (!CheckEOF(sourceSpan) && sourceSpan[this.currentIndex] != terminator)
        {
            this.currentIndex++;
        }
    }

    /// <summary>
    /// Returns the next character in the source and increments the current index
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <returns>The next source character</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char NextChar(in ReadOnlySpan<char> sourceSpan) => sourceSpan[this.currentIndex++];

    /// <summary>
    /// Peeks at the current character in the source without incrementing the current index
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <returns>The next source character, or if at the end of the source, <c>\0</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char PeekChar(in ReadOnlySpan<char> sourceSpan) => this.currentIndex < sourceSpan.Length ? sourceSpan[this.currentIndex] : char.MinValue;

    /// <summary>
    /// Peeks at the next character ahead in the source without incrementing the current index
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <param name="peekDistance">Look-ahead distance</param>
    /// <returns>The source code character at the given distance ahead of the current index, or <c>\0</c> if the peek index is outside of the source code bounds</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char PeekNextChar(in ReadOnlySpan<char> sourceSpan, in int peekDistance = 1)
    {
        int peekIndex = this.currentIndex + peekDistance;
        return peekIndex < sourceSpan.Length ? sourceSpan[peekIndex] : char.MinValue;
    }

    /// <summary>
    /// Check if we're at the end of the file
    /// </summary>
    /// <param name="sourceSpan">Source code span</param>
    /// <returns><see langword="true"/> if the scanner is at the end of the source code, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckEOF(in ReadOnlySpan<char> sourceSpan) => this.currentIndex >= sourceSpan.Length;

    /// <summary>
    /// Checks if the given character is a valid word character
    /// </summary>
    /// <param name="character">Character to test</param>
    /// <returns><see langword="true"/> if <paramref name="character"/> is an ascii letter or an underscore, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWordChar(in char character) => char.IsAsciiLetter(character) || character is '_';

    /// <summary>
    /// Checks if the given character is a valid identifier character
    /// </summary>
    /// <param name="character">Character to test</param>
    /// <returns><see langword="true"/> if <paramref name="character"/> is an ascii letter, ascii digit, or an underscore, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIdentifierChar(in char character) => IsWordChar(character) || char.IsAsciiDigit(character);
    #endregion
}
