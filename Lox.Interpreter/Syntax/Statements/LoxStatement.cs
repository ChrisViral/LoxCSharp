namespace Lox.Interpreter.Syntax.Statements;

/// <summary>
/// Expression base class
/// </summary>
public abstract record LoxStatement
{
    /// <summary>
    /// Handles an expression visitor
    /// </summary>
    /// <param name="visitor">Expression visitor</param>
    public abstract void Accept(IStatementVisitor visitor);

    /// <summary>
    /// Handles an expression visitor
    /// </summary>
    /// <param name="visitor">Expression visitor</param>
    /// <typeparam name="T">Visitor return type</typeparam>
    /// <returns>The visitor result</returns>
    public abstract T Accept<T>(IStatementVisitor<T> visitor);
}
