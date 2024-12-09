using Lox.Syntax.Expressions;

namespace Lox.Syntax.Statements;

/// <summary>
/// Print statement
/// </summary>
/// <param name="Expression">Expression to print</param>
public sealed record PrintStatement(LoxExpression Expression) : LoxStatement
{
    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPrintStatement(this);

    /// <inheritdoc />
    public override string ToString() => $"print {this.Expression};";
}
