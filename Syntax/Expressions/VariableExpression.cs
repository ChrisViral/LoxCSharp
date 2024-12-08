using CodeCrafters.Interpreter.Scanning;

namespace CodeCrafters.Interpreter.Syntax.Expressions;

/// <summary>
/// Variable expression
/// </summary>
/// <param name="Identifier">Variable identifier</param>
public sealed record VariableExpression(Token Identifier) : LoxExpression
{
    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitVariableExpression(this);

    /// <inheritdoc />
    public override string ToString() => this.Identifier.Lexeme;
}