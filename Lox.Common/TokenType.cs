using System.Runtime.Serialization;
using FastEnumUtility;
using JetBrains.Annotations;
using Lox.Common.Utils;

namespace Lox.Common;

/// <summary>
/// Lox tokens
/// </summary>
[PublicAPI]
public enum TokenType : byte
{
    // Undefined/Invalid
    [EnumMember(Value = "")]
    NONE  = 0,                      // Invalid token

    // End of File
    [EnumMember(Value = "")]
    EOF   = 4,                      // EOF

    // Groupings
    [EnumMember(Value = "(")]
    LEFT_PAREN  = (byte)'(',        // (
    [EnumMember(Value = ")")]
    RIGHT_PAREN = (byte)')',        // )
    [EnumMember(Value = "{")]
    LEFT_BRACE  = (byte)'{',        // {
    [EnumMember(Value = "}")]
    RIGHT_BRACE = (byte)'}',        // }

    // Delimiters
    [EnumMember(Value = ",")]
    COMMA     = (byte)',',          // ,
    [EnumMember(Value = ".")]
    DOT       = (byte)'.',          // .
    [EnumMember(Value = ";")]
    SEMICOLON = (byte)';',          // ;

    // Mathematical operation symbols
    [EnumMember(Value = "+")]
    PLUS  = (byte)'+',              // +
    [EnumMember(Value = "-")]
    MINUS = (byte)'-',              // -
    [EnumMember(Value = "*")]
    STAR  = (byte)'*',              // *
    [EnumMember(Value = "/")]
    SLASH = (byte)'/',              // /

    // Equality operator offset
    EQUALITY   = 100,

    // Equality operation symbols
    [EnumMember(Value = "!")]
    BANG          = (byte)'!',      // !
    [EnumMember(Value = "!=")]
    BANG_EQUAL    = '!' + EQUALITY, // !=
    [EnumMember(Value = "=")]
    EQUAL         = (byte)'=',      // =
    [EnumMember(Value = "==")]
    EQUAL_EQUAL   = '=' + EQUALITY, // ==
    [EnumMember(Value = ">")]
    GREATER       = (byte)'>',      // >
    [EnumMember(Value = ">=")]
    GREATER_EQUAL = '>' + EQUALITY, // >=
    [EnumMember(Value = "<")]
    LESS          = (byte)'<',      // <
    [EnumMember(Value = "<=")]
    LESS_EQUAL    = '<' + EQUALITY, // <=

    // Literals
    [EnumMember(Value = LoxUtils.NilString)]
    NIL        = 200,               // nil
    [EnumMember(Value = LoxUtils.TrueString)]
    TRUE       = 201,               // true
    [EnumMember(Value = LoxUtils.FalseString)]
    FALSE      = 202,               // false
    NUMBER     = 203,               // 123
    STRING     = 204,               // "foo"
    IDENTIFIER = 205,               // bar

    // Conditional keywords
    [EnumMember(Value = "and")]
    AND  = 210,                     // and
    [EnumMember(Value = "or")]
    OR   = 211,                     // or
    [EnumMember(Value = "else")]
    ELSE = 213,                     // else

    // OOP keywords
    [EnumMember(Value = "this")]
    THIS  = 220,                    // this
    [EnumMember(Value = "super")]
    SUPER = 221,                    // super

    // Statement keywords marker
    STATEMENTS = IF - 1,

    // Branching keywords
    [EnumMember(Value = "if")]
    IF    = 230,                    // if
    [EnumMember(Value = "for")]
    FOR   = 231,                    // for
    [EnumMember(Value = "while")]
    WHILE = 232,                    // while

    // Functional keywords
    [EnumMember(Value = "var")]
    VAR    = 240,                   // var
    [EnumMember(Value = "fun")]
    FUN    = 241,                   // fun
    [EnumMember(Value = "return")]
    RETURN = 242,                   // return
    [EnumMember(Value = "print")]
    PRINT  = 243,                   // print

    // Object keywords
    [EnumMember(Value = "class")]
    CLASS = 250,                    // class

    // Error token
    ERROR = byte.MaxValue           // Error
}

[FastEnum<TokenType>, PublicAPI]
public sealed partial class TokenTypeBooster;
