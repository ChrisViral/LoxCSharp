using Lox.Runtime;
using Lox.Runtime.Types;

namespace Lox.Syntax.Expressions;

/// <summary>
/// Literal expression
/// </summary>
/// <param name="Value">The represented literal value</param>
public sealed record LiteralExpression(LoxValue Value) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitLiteralExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitLiteralExpression(this);
}
