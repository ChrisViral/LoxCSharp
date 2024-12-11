using Lox.Interpreter.Scanner;

namespace Lox.Interpreter.Syntax.Expressions;

/// <summary>
/// Unary expression
/// </summary>
/// <param name="Operator">Expression operator</param>
/// <param name="InnerExpression">Expression operand</param>
public sealed record UnaryExpression(in Token Operator, LoxExpression InnerExpression) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitUnaryExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitUnaryExpression(this);
}
