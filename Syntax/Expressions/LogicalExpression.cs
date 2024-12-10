using Lox.Scanner;

namespace Lox.Syntax.Expressions;

/// <summary>
/// Logical expression
/// </summary>
/// <param name="LeftExpression">Left expression operand</param>
/// <param name="Operator">Expression operator</param>
/// <param name="RightExpression">Right expression operand</param>
public sealed record LogicalExpression(LoxExpression LeftExpression, in Token Operator, LoxExpression RightExpression) : BinaryExpression(LeftExpression, Operator, RightExpression)
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitLogicalExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitLogicalExpression(this);
}
