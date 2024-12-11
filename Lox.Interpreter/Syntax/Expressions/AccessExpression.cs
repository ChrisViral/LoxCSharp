using Lox.Interpreter.Scanner;

namespace Lox.Interpreter.Syntax.Expressions;

/// <summary>
/// Variable expression
/// </summary>
/// <param name="Target">Access target</param>
/// <param name="Identifier">Access identifier</param>
public sealed record AccessExpression(LoxExpression Target, in Token Identifier) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitAccessExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitAccessExpression(this);
}
