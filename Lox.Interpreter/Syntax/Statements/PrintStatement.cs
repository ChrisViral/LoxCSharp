using Lox.Interpreter.Syntax.Expressions;

namespace Lox.Interpreter.Syntax.Statements;

/// <summary>
/// Print statement
/// </summary>
/// <param name="Expression">Expression to print</param>
public sealed record PrintStatement(LoxExpression Expression) : LoxStatement
{
    /// <inheritdoc />
    public override void Accept(IStatementVisitor visitor) => visitor.VisitPrintStatement(this);

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitPrintStatement(this);

    /// <inheritdoc />
    public override string ToString() => $"print {this.Expression};";
}
