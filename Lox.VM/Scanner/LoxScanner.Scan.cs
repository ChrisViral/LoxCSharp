using FastEnumUtility;
using Lox.Common;

namespace Lox.VM.Scanner;

public sealed partial class LoxScanner
{
    #region Methods
    /// <summary>
    /// Tries to scan the next token in the source
    /// </summary>
    /// <param name="token">Scanned token</param>
    /// <returns><see langword="true"/> if a new token was found, otherwise <see langword="false"/> when reaching the end of the file</returns>
    /// <exception cref="InvalidOperationException">If the scanner doesn't have the source code pinned for scanning</exception>
    public bool ScanNextToken(out Token token)
    {
        ThrowIfScanningState(!this.IsScanning, "Cannot run the scanner without first pinning the source");

        if (this.IsEOF)
        {
            if (this.returnedEof)
            {
                token = default;
                return false;
            }

            this.returnedEof = true;
            token = new Token(TokenType.EOF, this.currentLine);
            return true;
        }

        ResetStart();
        token = ScanToken();
        return true;
    }

    /// <summary>
    /// Scans the next token
    /// </summary>
    /// <returns>The scanned token</returns>
    private Token ScanToken()
    {
        // Skip whitespace
        if (!SkipWhitespace(out char current))
        {
            this.returnedEof = true;
            return new Token(TokenType.EOF, this.currentLine);
        }

        // Check if the token is implicitly defined
        TokenType castedType = (TokenType)current;
        // ReSharper disable once InvertIf
        if (FastEnum.IsDefined<TokenType, TokenTypeBooster>(castedType))
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (castedType)
            {
                // Double character equality operator
                case TokenType.BANG:
                case TokenType.EQUAL:
                case TokenType.GREATER:
                case TokenType.LESS:
                    return new Token(MatchNext('=') ? castedType + (int)TokenType.EQUALITY : castedType, this.currentLine);

                default:
                    return new Token(castedType, this.currentLine);
            }
        }

        return current switch
        {
            '"'                                    => TokenizeString(),
            char _ when char.IsAsciiDigit(current) => TokenizeNumber(),
            char _ when IsWordChar(current)        => TokenizeIdentifier(),
            _                                      => Token.MakeError($"Unexpected character {current}.", this.currentLine)
        };
    }

    /// <summary>
    /// Skips whitespace characters until a non-whitespace character is found, or the end of the file is reached
    /// </summary>
    /// <param name="current">Current character after skipping whitespace</param>
    /// <returns><see langword="true"/> if a non-whitespace character has been found, or <see langword="false"/> if the end of file has been reached</returns>
    private bool SkipWhitespace(out char current)
    {
        for (current = NextChar(); ; current = NextChar())
        {
            switch (current)
            {
                case '\n':
                    this.currentLine++;
                    continue;

                case char.MinValue:
                    Rewind();
                    return false;

                case '/' when MatchNext('/'):
                    ConsumeUntil('\n');
                    this.currentLine++;
                    continue;

                case char _ when char.IsWhiteSpace(current):
                    continue;

                default:
                    ResetStartToPrevious();
                    return true;
            }
        }
    }

    /// <summary>
    /// Tokenizes a string from the current point in the source
    /// </summary>
    /// <returns>The next <see cref="TokenType.STRING"/> token</returns>
    private Token TokenizeString()
    {
        // Consume all characters until string ends
        for (char current = NextChar(); current is not '"'; current = NextChar())
        {
            switch (current)
            {
                case '\n':
                    this.currentLine++;
                    break;

                case char.MinValue:
                    return Token.MakeError("Unterminated string.", this.currentLine);
            }
        }

        return new Token(TokenType.STRING, GetCurrentLexeme(), this.currentLine);
    }

    /// <summary>
    /// Tokenizes a number from the current point in the source
    /// </summary>
    /// <returns>The next <see cref="TokenType.NUMBER"/> token</returns>
    private Token TokenizeNumber()
    {
        char current;
        for (current = CurrentChar(); char.IsAsciiDigit(current); current = SkipChar());

        // ReSharper disable once InvertIf
        if (current is '.' && char.IsAsciiDigit(PeekChar()))
        {
            for (current = SkipChar(2); char.IsAsciiDigit(current); current = SkipChar());
        }

        return new Token(TokenType.NUMBER, GetCurrentLexeme(), this.currentLine);
    }

    /// <summary>
    /// Tokenizes a number from the current point in the source
    /// </summary>
    /// <returns>The next <see cref="TokenType.NUMBER"/> token</returns>
    private Token TokenizeIdentifier()
    {
        for (char current = CurrentChar(); IsIdentifierChar(current); current = SkipChar());

        TokenType type = GetIdentifierType();
        return type is TokenType.IDENTIFIER
                   ? new Token(TokenType.IDENTIFIER, GetCurrentLexeme(), this.currentLine)
                   : new Token(type, this.currentLine);
    }

    /// <summary>
    /// Gets the identifier type for the given identifier token
    /// </summary>
    /// <returns>Current token identifier type</returns>
    private unsafe TokenType GetIdentifierType()
    {
        // There are no keywords of less than two characters, or seven or more characters
        if (this.CurrentTokenLength is < 2 or > 7) return TokenType.IDENTIFIER;

        char* keywordStart = this.tokenStart;
        return *keywordStart++ switch
        {
            // ReSharper disable StringLiteralTypo
            'a' => ValidateKeyword(keywordStart, "nd", TokenType.AND),
            'c' => ValidateKeyword(keywordStart, "lass", TokenType.CLASS),
            'e' => ValidateKeyword(keywordStart, "lse", TokenType.ELSE),
            'f' => *keywordStart++ switch
            {
                'a' => ValidateKeyword(keywordStart, "lse", TokenType.FALSE),
                'o' => ValidateKeyword(keywordStart, 'r', TokenType.FOR),
                'u' => ValidateKeyword(keywordStart, 'n', TokenType.FUN),
                _   => TokenType.IDENTIFIER
            },
            'i' => ValidateKeyword(keywordStart, 'f', TokenType.IF),
            'n' => ValidateKeyword(keywordStart, "il", TokenType.NIL),
            'o' => ValidateKeyword(keywordStart, 'r', TokenType.OR),
            'p' => ValidateKeyword(keywordStart, "rint", TokenType.PRINT),
            'r' => ValidateKeyword(keywordStart, "eturn", TokenType.RETURN),
            's' => ValidateKeyword(keywordStart, "uper", TokenType.SUPER),
            't' => *keywordStart++ switch
            {
                'h' => ValidateKeyword(keywordStart, "is", TokenType.THIS),
                'r' => ValidateKeyword(keywordStart, "ue", TokenType.TRUE),
                _   => TokenType.IDENTIFIER
            },
            'v' => ValidateKeyword(keywordStart, "ar", TokenType.VAR),
            'w' => ValidateKeyword(keywordStart, "hile", TokenType.WHILE),
            _   => TokenType.IDENTIFIER
            // ReSharper enable StringLiteralTypo
        };
    }

    /// <summary>
    /// Validates the the keyword from the given pointer is equal to the test character
    /// </summary>
    /// <param name="keywordRestStart">Keyword rest start pointer</param>
    /// <param name="keywordTest">Keyword test character</param>
    /// <param name="type">Keyword type</param>
    /// <returns><paramref name="type"/> if the given keyword matched the test character, otherwise <see cref="TokenType.IDENTIFIER"/></returns>
    private unsafe TokenType ValidateKeyword(char* keywordRestStart, char keywordTest, TokenType type)
    {
        // ReSharper disable once ArrangeMethodOrOperatorBody
        return (this.currentChar - keywordRestStart) is 1L && *keywordRestStart == keywordTest ? type : TokenType.IDENTIFIER;
    }

    /// <summary>
    /// Validates the the keyword from the given pointer is equal to the test span
    /// </summary>
    /// <param name="keywordRestStart">Keyword rest start pointer</param>
    /// <param name="keywordTest">Keyword test span</param>
    /// <param name="type">Keyword type</param>
    /// <returns><paramref name="type"/> if the given keyword matched the test span, otherwise <see cref="TokenType.IDENTIFIER"/></returns>
    private unsafe TokenType ValidateKeyword(char* keywordRestStart, ReadOnlySpan<char> keywordTest, TokenType type)
    {
        int length = keywordTest.Length;
        if (length != (int)(this.currentChar - keywordRestStart)) return TokenType.IDENTIFIER;

        ReadOnlySpan<char> keywordRest = new(keywordRestStart, length);
        return keywordRest.SequenceEqual(keywordTest) ? type : TokenType.IDENTIFIER;
    }
    #endregion
}
