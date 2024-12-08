using CodeCrafters.Interpreter.Scanning;

namespace CodeCrafters.Interpreter.Syntax.Expressions;

/// <summary>
/// Unary expression
/// </summary>
/// <param name="Operator">Expression operator</param>
/// <param name="InnerExpression">Expression operand</param>
public sealed record UnaryExpression(Token Operator, LoxExpression InnerExpression) : LoxExpression
{
    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitUnaryExpression(this);

    /// <inheritdoc />
    public override string ToString() => this.Operator.Lexeme + this.InnerExpression;
}
