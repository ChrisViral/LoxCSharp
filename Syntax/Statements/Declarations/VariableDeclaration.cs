using Lox.Scanning;
using Lox.Syntax.Expressions;

namespace Lox.Syntax.Statements.Declarations;

/// <summary>
/// Variable declaration statement
/// </summary>
/// <param name="Identifier">Variable identifier</param>
/// <param name="Initializer">Variable initializing expression</param>
public sealed record VariableDeclaration(in Token Identifier, LoxExpression? Initializer) : LoxDeclaration(Identifier)
{
    /// <inheritdoc />
    public override void Accept(IStatementVisitor visitor) => visitor.VisitVariableDeclaration(this);

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitVariableDeclaration(this);
}
