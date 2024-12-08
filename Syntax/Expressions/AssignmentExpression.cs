using CodeCrafters.Interpreter.Scanning;

namespace CodeCrafters.Interpreter.Syntax.Expressions;

/// <summary>
/// Variable expression
/// </summary>
/// <param name="Identifier">Variable identifier</param>
public sealed record AssignmentExpression(Token Identifier, LoxExpression Value) : LoxExpression
{
    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitAssignmentExpression(this);

    /// <inheritdoc />
    public override string ToString() => this.Identifier.Lexeme;
}
