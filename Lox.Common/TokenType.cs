using System.ComponentModel;
using JetBrains.Annotations;
using Lox.Common.Utils;

namespace Lox.Common;

/// <summary>
/// Lox tokens
/// </summary>
[PublicAPI]
public enum TokenType
{
    // Undefined/Invalid
    NONE = 0,                       // Invalid token

    // Matched tokens
    LEFT_PAREN  = '(',              // (
    RIGHT_PAREN = ')',              // )
    LEFT_BRACE  = '{',              // {
    RIGHT_BRACE = '}',              // }

    // Delimiters
    COMMA     = ',',                // ,
    DOT       = '.',                // .
    SEMICOLON = ';',                // ;

    // Mathematical operation symbols
    PLUS  = '+',                    // +
    MINUS = '-',                    // -
    STAR  = '*',                    // *
    SLASH = '/',                    // /

    // Equality operator offset
    EQUALITY   = 500,

    // Equality operation symbols
    BANG          = '!',            // !
    BANG_EQUAL    = '!' + EQUALITY, // !=
    EQUAL         = '=',            // =
    EQUAL_EQUAL   = '=' + EQUALITY, // ==
    GREATER       = '>',            // >
    GREATER_EQUAL = '>' + EQUALITY, // >=
    LESS          = '<',            // <
    LESS_EQUAL    = '<' + EQUALITY, // <=

    // Literals
    NIL        = 1000,              // nil
    TRUE       = 1001,              // true
    FALSE      = 1002,              // false
    NUMBER     = 1003,              // 123
    STRING     = 1004,              // "foo"
    IDENTIFIER = 1005,              // bar

    // Conditional keywords
    AND  = 1010,                    // and
    OR   = 1011,                    // or
    ELSE = 1013,                    // else

    // OOP keywords
    THIS  = 1020,                   // this
    SUPER = 1021,                   // super

    // Statement keywords marker
    STATEMENTS = 1100,

    // Branching keywords
    IF    = 1101,                   // if
    FOR   = 1102,                   // for
    WHILE = 1103,                   // while

    // Functional keywords
    VAR    = 1110,                  // var
    FUN    = 1111,                  // fun
    RETURN = 1112,                  // return
    PRINT  = 1113,                  // print

    // Object keywords
    CLASS = 1120,                   // class

    // End of File
    EOF = -1,                       // EOF

    // Error token
    ERROR = int.MaxValue
}

/// <summary>
/// <see cref="TokenType"/> utility and extensions
/// </summary>
[PublicAPI]
public static class TokenTypeExtensions
{
    #region Extensions
    /// <summary>
    /// Gets the associated static Lexeme for the given <see cref="TokenType"/>
    /// </summary>
    /// <param name="tokenType">Token type to get the Lexeme for</param>
    /// <returns>The associated static lexeme for this <see cref="TokenType"/></returns>
    /// <exception cref="InvalidOperationException">If the token type does not have a static lexeme defined</exception>
    /// <exception cref="InvalidEnumArgumentException">For invalid values of <paramref name="tokenType"/></exception>
    public static string GetStaticLexeme(this TokenType tokenType) => tokenType switch
    {
        TokenType.LEFT_PAREN    => "(",
        TokenType.RIGHT_PAREN   => ")",
        TokenType.LEFT_BRACE    => "{",
        TokenType.RIGHT_BRACE   => "}",
        TokenType.COMMA         => ",",
        TokenType.DOT           => ".",
        TokenType.SEMICOLON     => ";",
        TokenType.MINUS         => "-",
        TokenType.PLUS          => "+",
        TokenType.SLASH         => "/",
        TokenType.STAR          => "*",
        TokenType.EQUALITY      => throw new InvalidOperationException("The equality offset constant is not a valid token type and has no lexeme"),
        TokenType.BANG          => "!",
        TokenType.BANG_EQUAL    => "!=",
        TokenType.EQUAL         => "=",
        TokenType.EQUAL_EQUAL   => "==",
        TokenType.GREATER       => ">",
        TokenType.GREATER_EQUAL => ">=",
        TokenType.LESS          => "<",
        TokenType.LESS_EQUAL    => "<=",
        TokenType.NIL           => LoxUtils.NilString,
        TokenType.TRUE          => LoxUtils.TrueString,
        TokenType.FALSE         => LoxUtils.FalseString,
        TokenType.NUMBER        => throw new InvalidOperationException("Number tokens do not have a static lexeme"),
        TokenType.STRING        => throw new InvalidOperationException("String tokens do not have a static lexeme"),
        TokenType.IDENTIFIER    => throw new InvalidOperationException("Identifier tokens do not have a static lexeme"),
        TokenType.AND           => "and",
        TokenType.OR            => "or",
        TokenType.ELSE          => "else",
        TokenType.THIS          => "this",
        TokenType.SUPER         => "super",
        TokenType.STATEMENTS    => throw new InvalidOperationException("The statements constant is not a valid token type and has no lexeme"),
        TokenType.IF            => "if",
        TokenType.FOR           => "for",
        TokenType.WHILE         => "while",
        TokenType.VAR           => "var",
        TokenType.FUN           => "fun",
        TokenType.RETURN        => "return",
        TokenType.PRINT         => "print",
        TokenType.CLASS         => "class",
        TokenType.EOF           => string.Empty,
        TokenType.ERROR         => throw new InvalidOperationException("Error token needs to generate it's own error lexeme"),
        TokenType.NONE          => string.Empty,
        _                       => throw new InvalidEnumArgumentException(nameof(tokenType), (int)tokenType, typeof(TokenType))
    };
    #endregion
}
