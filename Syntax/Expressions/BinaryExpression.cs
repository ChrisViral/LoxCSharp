using Lox.Scanning;

namespace Lox.Syntax.Expressions;

/// <summary>
/// Binary expression
/// </summary>
/// <param name="LeftExpression">Left expression operand</param>
/// <param name="Operator">Expression operator</param>
/// <param name="RightExpression">Right expression operand</param>
public record BinaryExpression(LoxExpression LeftExpression, Token Operator, LoxExpression RightExpression) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitBinaryExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
}
