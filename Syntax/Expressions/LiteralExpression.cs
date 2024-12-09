using Lox.Runtime;

namespace Lox.Syntax.Expressions;

/// <summary>
/// Literal expression
/// </summary>
/// <param name="Value">The represented literal value</param>
public sealed record LiteralExpression(LoxValue Value) : LoxExpression
{
    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitLiteralExpression(this);

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Value.Type is LoxValue.LiteralType.STRING
               ? $"\"{this.Value.StringValue}\""
               : this.Value.ToString();
    }
}
