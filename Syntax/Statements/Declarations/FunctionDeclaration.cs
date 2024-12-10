using System.Collections.ObjectModel;
using Lox.Scanning;

namespace Lox.Syntax.Statements.Declarations;

/// <summary>
/// Function declaration statement
/// </summary>
/// <param name="Identifier">Function identifier</param>
/// <param name="Parameters">Function parameters</param>
/// <param name="Body">Function block body</param>
public record FunctionDeclaration(in Token Identifier, ReadOnlyCollection<Token> Parameters, BlockStatement Body) : LoxDeclaration(Identifier)
{
    /// <inheritdoc />
    public override void Accept(IStatementVisitor visitor) => visitor.VisitFunctionDeclaration(this);

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitFunctionDeclaration(this);
}
