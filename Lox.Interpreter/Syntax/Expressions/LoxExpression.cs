namespace Lox.Interpreter.Syntax.Expressions;

/// <summary>
/// Expression base class
/// </summary>
public abstract record LoxExpression
{
    /// <summary>
    /// Handles an expression visitor
    /// </summary>
    /// <param name="visitor">Expression visitor</param>
    public abstract void Accept(IExpressionVisitor visitor);

    /// <summary>
    /// Handles an expression visitor
    /// </summary>
    /// <param name="visitor">Expression visitor</param>
    /// <typeparam name="T">Visitor return type</typeparam>
    /// <returns>The visitor result</returns>
    public abstract T Accept<T>(IExpressionVisitor<T> visitor);
}
