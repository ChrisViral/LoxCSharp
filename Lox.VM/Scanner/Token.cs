using JetBrains.Annotations;
using Lox.Common;
using Lox.Common.Utils;

namespace Lox.VM.Scanner;

/// <summary>
/// Scanning token
/// </summary>
/// <param name="Type">Token type</param>
/// <param name="Lexeme">Token's lexeme</param>
/// <param name="Line">Token line</param>
[PublicAPI]
public readonly record struct Token(TokenType Type, string Lexeme, int Line)
{
    #region Properties
    /// <summary>
    /// If this is an End of File token
    /// </summary>
    public bool IsEOF => this.Type is TokenType.EOF;

    /// <summary>
    /// If the token is a statement start keyword
    /// </summary>
    public bool IsStatementStart => this.Type > TokenType.STATEMENTS;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new token with a static lexeme
    /// </summary>
    /// <param name="type">Token type</param>
    /// <param name="line">Token line</param>
    public Token(in TokenType type, in int line) : this(type, type.GetStaticLexeme(), line) { }
    #endregion

    #region Methods
    /// <inheritdoc />
    public override string ToString() => $"{EnumUtils.ToString(this.Type)} {this.Lexeme} {this.Line}";
    #endregion
}
