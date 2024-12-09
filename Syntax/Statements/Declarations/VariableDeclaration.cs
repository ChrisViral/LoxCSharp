using Lox.Scanning;
using Lox.Syntax.Expressions;

namespace Lox.Syntax.Statements.Declarations;

/// <summary>
/// Variable declaration statement
/// </summary>
/// <param name="Identifier">Variable identifier</param>
/// <param name="Initializer">Variable initializing expression</param>
public sealed record VariableDeclaration(Token Identifier, LoxExpression? Initializer) : LoxStatement
{
    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitVariableDeclaration(this);

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Initializer is not null ? $"var {this.Identifier.Lexeme} = {this.Initializer};" : $"var {this.Identifier.Lexeme};";
    }
}
