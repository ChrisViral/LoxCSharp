using System.Runtime.Serialization;
using FastEnumUtility;
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
    [EnumMember(Value = "")]
    NONE = 0,                       // Invalid token

    // Matched tokens
    [EnumMember(Value = "(")]
    LEFT_PAREN  = '(',              // (
    [EnumMember(Value = ")")]
    RIGHT_PAREN = ')',              // )
    [EnumMember(Value = "{")]
    LEFT_BRACE  = '{',              // {
    [EnumMember(Value = "}")]
    RIGHT_BRACE = '}',              // }

    // Delimiters
    [EnumMember(Value = ",")]
    COMMA     = ',',                // ,
    [EnumMember(Value = ".")]
    DOT       = '.',                // .
    [EnumMember(Value = ";")]
    SEMICOLON = ';',                // ;

    // Mathematical operation symbols
    [EnumMember(Value = "+")]
    PLUS  = '+',                    // +
    [EnumMember(Value = "-")]
    MINUS = '-',                    // -
    [EnumMember(Value = "*")]
    STAR  = '*',                    // *
    [EnumMember(Value = "/")]
    SLASH = '/',                    // /

    // Equality operator offset
    EQUALITY   = 500,

    // Equality operation symbols
    [EnumMember(Value = "!")]
    BANG          = '!',            // !
    [EnumMember(Value = "!=")]
    BANG_EQUAL    = '!' + EQUALITY, // !=
    [EnumMember(Value = "=")]
    EQUAL         = '=',            // =
    [EnumMember(Value = "==")]
    EQUAL_EQUAL   = '=' + EQUALITY, // ==
    [EnumMember(Value = ">")]
    GREATER       = '>',            // >
    [EnumMember(Value = ">=")]
    GREATER_EQUAL = '>' + EQUALITY, // >=
    [EnumMember(Value = "<")]
    LESS          = '<',            // <
    [EnumMember(Value = "<=")]
    LESS_EQUAL    = '<' + EQUALITY, // <=

    // Literals
    [EnumMember(Value = LoxUtils.NilString)]
    NIL        = 1000,              // nil
    [EnumMember(Value = LoxUtils.TrueString)]
    TRUE       = 1001,              // true
    [EnumMember(Value = LoxUtils.FalseString)]
    FALSE      = 1002,              // false
    NUMBER     = 1003,              // 123
    STRING     = 1004,              // "foo"
    IDENTIFIER = 1005,              // bar

    // Conditional keywords
    [EnumMember(Value = "and")]
    AND  = 1010,                    // and
    [EnumMember(Value = "or")]
    OR   = 1011,                    // or
    [EnumMember(Value = "else")]
    ELSE = 1013,                    // else

    // OOP keywords
    [EnumMember(Value = "this")]
    THIS  = 1020,                   // this
    [EnumMember(Value = "super")]
    SUPER = 1021,                   // super

    // Statement keywords marker
    STATEMENTS = IF - 1,

    // Branching keywords
    [EnumMember(Value = "if")]
    IF    = 1100,                   // if
    [EnumMember(Value = "for")]
    FOR   = 1101,                   // for
    [EnumMember(Value = "while")]
    WHILE = 1102,                   // while

    // Functional keywords
    [EnumMember(Value = "var")]
    VAR    = 1110,                  // var
    [EnumMember(Value = "for")]
    FUN    = 1111,                  // fun
    [EnumMember(Value = "return")]
    RETURN = 1112,                  // return
    [EnumMember(Value = "print")]
    PRINT  = 1113,                  // print

    // Object keywords
    [EnumMember(Value = "class")]
    CLASS = 1120,                   // class

    // End of File
    [EnumMember(Value = "")]
    EOF = -1,                       // EOF

    // Error token
    ERROR = int.MaxValue            // Error
}

[FastEnum<TokenType>, PublicAPI]
public sealed partial class TokenTypeBooster;
