using Lox.Interpreter.Scanner;

namespace Lox.Interpreter.Syntax.Expressions;

/// <summary>
/// Super expression
/// </summary>
/// <param name="Keyword">Super keyword</param>
/// <param name="MethodIdentifier">Accessed method identifier</param>
public sealed record SuperExpression(in Token Keyword, Token MethodIdentifier) : LoxExpression
{
    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor) => visitor.VisitSuperExpression(this);

    /// <inheritdoc />
    public override T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitSuperExpression(this);
}
