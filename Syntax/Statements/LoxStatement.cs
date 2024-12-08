namespace CodeCrafters.Interpreter.Syntax.Statements;

/// <summary>
/// Expression base class
/// </summary>
public abstract record LoxStatement
{
    /// <summary>
    /// Handles an expression visitor
    /// </summary>
    /// <param name="visitor">Expression visitor</param>
    /// <typeparam name="T">Visitor return type</typeparam>
    /// <returns>The visitor result</returns>
    public abstract T Accept<T>(IStatementVisitor<T> visitor);

    /// <summary>
    /// Expression string representation
    /// </summary>
    /// <returns>The code-like string representation of the expression</returns>
    public override string ToString() => string.Empty;
}
