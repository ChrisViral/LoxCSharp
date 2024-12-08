using CodeCrafters.Interpreter.Syntax.Expressions;

namespace CodeCrafters.Interpreter.Syntax.Statements;

/// <summary>
/// While statement
/// </summary>
/// <param name="Initializer">For loop initializer</param>
/// <param name="Condition">Conditional expression to test</param>
/// <param name="Increment">For loop increment</param>
/// <param name="BodyStatement">Statement to execute if the condition was true</param>
public sealed record ForStatement(LoxStatement? Initializer, LoxExpression? Condition, ExpressionStatement? Increment, LoxStatement BodyStatement) : LoxStatement
{
    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitForStatement(this);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"for ({this.Initializer?.ToString() ?? ";"} {this.Condition?.ToString() ?? string.Empty}; {this.Increment?.Expression.ToString() ?? string.Empty})\n    {this.BodyStatement}";
    }
}
