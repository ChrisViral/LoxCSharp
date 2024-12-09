using Lox.Syntax.Expressions;

namespace Lox.Syntax.Statements;

/// <summary>
/// If statement
/// </summary>
/// <param name="Condition">Conditional expression to test</param>
/// <param name="IfBranch">Statement to execute if the condition was true</param>
/// <param name="ElseBranch">Statement to execute if the condition was false</param>
public sealed record IfStatement(LoxExpression Condition, LoxStatement IfBranch, LoxStatement? ElseBranch) : LoxStatement
{
    /// <inheritdoc />
    public override void Accept(IStatementVisitor visitor) => visitor.VisitIfStatement(this);

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitIfStatement(this);
}
