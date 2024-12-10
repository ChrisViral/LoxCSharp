using Lox.Scanning;

namespace Lox.Syntax.Expressions;

/// <summary>
/// Variable expression
/// </summary>
/// <param name="Identifier">Variable identifier</param>
public sealed record VariableExpression(in Token Identifier) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitVariableExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitVariableExpression(this);
}