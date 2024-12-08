using CodeCrafters.Interpreter.Scanning;
using CodeCrafters.Interpreter.Syntax.Expressions;

namespace CodeCrafters.Interpreter.Syntax.Statements;

/// <summary>
/// Return statement
/// </summary>
/// <param name="Keyword">Return keyword</param>
/// <param name="Value">Expression to print</param>
public sealed record ReturnStatement(Token Keyword, LoxExpression? Value) : LoxStatement
{
    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitReturnStatement(this);

    /// <inheritdoc />
    public override string ToString() => this.Value is not null ? $"return {this.Value};" : "return;";
}
