using CodeCrafters.Interpreter.Syntax.Expressions;

namespace CodeCrafters.Interpreter.Syntax.Statements;

/// <summary>
/// Expression statement
/// </summary>
/// <param name="Expression">Statement's expression</param>
public sealed record ExpressionStatement(LoxExpression Expression) : LoxStatement
{
    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitExpressionStatement(this);

    /// <inheritdoc />
    public override string ToString() => this.Expression + ";";
}
