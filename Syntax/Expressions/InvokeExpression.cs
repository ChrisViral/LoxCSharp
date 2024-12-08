using System.Collections.ObjectModel;
using CodeCrafters.Interpreter.Scanning;

namespace CodeCrafters.Interpreter.Syntax.Expressions;

/// <summary>
/// Invoke expression
/// </summary>
/// <param name="Target">Invocation target</param>
/// <param name="Parameters">Invocation parameters</param>
/// <param name="Terminator">Invocation terminating token</param>
public sealed record InvokeExpression(LoxExpression Target, ReadOnlyCollection<LoxExpression> Parameters, Token Terminator) : LoxExpression
{
    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitInvokeExpression(this);

    /// <inheritdoc />
    public override string ToString() => $"{this.Target}({string.Join(", ", this.Parameters)})";
}
