using Lox.Scanning;

namespace Lox.Syntax.Expressions;

/// <summary>
/// Variable expression
/// </summary>
/// <param name="Target">Access target</param>
/// <param name="Identifier">Access identifier</param>
/// <param name="Value">Value to set</param>
public sealed record SetExpression(LoxExpression Target, in Token Identifier, LoxExpression Value) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitSetExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitSetExpression(this);
}
