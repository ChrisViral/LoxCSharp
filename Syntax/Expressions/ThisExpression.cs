using Lox.Scanning;

namespace Lox.Syntax.Expressions;

/// <summary>
/// This expression
/// </summary>
/// <param name="Keyword"><see langword="this"/> keyword</param>
public sealed record ThisExpression(in Token Keyword) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitThisExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitThisExpression(this);
}
