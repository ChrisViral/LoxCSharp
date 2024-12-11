namespace Lox.Interpreter.Syntax.Expressions;

/// <summary>
/// Grouping expression
/// </summary>
/// <param name="InnerExpression">Contained expression</param>
public sealed record GroupingExpression(LoxExpression InnerExpression) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitGroupingExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitGroupingExpression(this);
}
