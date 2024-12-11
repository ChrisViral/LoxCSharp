using Lox.Interpreter.Scanner;
using Lox.Interpreter.Syntax.Expressions;

namespace Lox.Interpreter.Syntax.Statements.Declarations;

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
