using System.Collections.ObjectModel;
using Lox.Scanning;

namespace Lox.Syntax.Expressions;

/// <summary>
/// Invoke expression
/// </summary>
/// <param name="Target">Invocation target</param>
/// <param name="Arguments">Invocation parameters</param>
/// <param name="Terminator">Invocation terminating token</param>
public sealed record InvokeExpression(LoxExpression Target, ReadOnlyCollection<LoxExpression> Arguments, Token Terminator) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitInvokeExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitInvokeExpression(this);
}
