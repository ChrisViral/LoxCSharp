using Lox.Interpreter.Syntax.Expressions;

namespace Lox.Interpreter.Syntax.Statements;

/// <summary>
/// While statement
/// </summary>
/// <param name="Condition">Conditional expression to test</param>
/// <param name="BodyStatement">Statement to execute if the condition was true</param>
public sealed record WhileStatement(LoxExpression Condition, LoxStatement BodyStatement) : LoxStatement
{
    /// <inheritdoc />
    public override void Accept(IStatementVisitor visitor) => visitor.VisitWhileStatement(this);

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitWhileStatement(this);

    /// <inheritdoc />
    public override string ToString() => $"while ({this.Condition})\n    {this.BodyStatement}";
}
