using Lox.Scanning;
using Lox.Syntax.Expressions;

namespace Lox.Syntax.Statements;

/// <summary>
/// Return statement
/// </summary>
/// <param name="Keyword">Return keyword</param>
/// <param name="Value">Expression to print</param>
public sealed record ReturnStatement(Token Keyword, LoxExpression? Value) : LoxStatement
{
    /// <inheritdoc />
    public override void Accept(IStatementVisitor visitor) => visitor.VisitReturnStatement(this);

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitReturnStatement(this);

    /// <inheritdoc />
    public override string ToString() => this.Value is not null ? $"return {this.Value};" : "return;";
}
