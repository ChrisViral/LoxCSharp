using CodeCrafters.Interpreter.Runtime;
using CodeCrafters.Interpreter.Utils;

namespace CodeCrafters.Interpreter.Scanning;

/// <summary>
/// Lox Token
/// </summary>
/// <param name="Type">Token type</param>
/// <param name="Lexeme">Token lexeme value</param>
/// <param name="Literal">Token literal value</param>
/// <param name="Line">Token source line</param>
public readonly record struct Token(TokenType Type, string Lexeme, LoxValue Literal, int Line)
{
    /// <summary>
    /// If this is an EOF token
    /// </summary>
    public bool IsEOF => this.Type == TokenType.EOF;

    /// <summary>
    /// If the token is a literal
    /// </summary>
    public bool IsLiteral => this.Type is >= TokenType.NIL and <= TokenType.STRING;

    /// <summary>
    /// If the token is a statement start keyword
    /// </summary>
    public bool IsStatementStart => this.Type > TokenType.STATEMENTS;

    /// <summary>
    /// Creates a token from it's static lexeme value
    /// </summary>
    /// <param name="type">Token type</param>
    /// <param name="line">Token source line</param>
    public Token(TokenType type, int line)
        : this(type,
               type.GetStaticLexeme(),
               type switch
               {
                   TokenType.NIL   => LoxValue.Nil,
                   TokenType.TRUE  => LoxValue.True,
                   TokenType.FALSE => LoxValue.False,
                   _               => LoxValue.Invalid
               },
               line) { }

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() => $"{EnumUtils.ToString(this.Type)} {this.Lexeme} {this.Literal.TokenString()}";
}
