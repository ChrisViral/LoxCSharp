using System.Collections.ObjectModel;
using Lox.Scanning;

namespace Lox.Syntax.Statements.Declarations;

/// <summary>
/// Method declaration statement
/// </summary>
/// <param name="Identifier">Method identifier</param>
/// <param name="Parameters">Method parameters</param>
/// <param name="Body">Method block body</param>
public sealed record MethodDeclaration(in Token Identifier, ReadOnlyCollection<Token> Parameters, BlockStatement Body) : FunctionDeclaration(Identifier, Parameters, Body)
{
    /// <inheritdoc />
    public override void Accept(IStatementVisitor visitor) => visitor.VisitMethodDeclaration(this);

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitMethodDeclaration(this);
}
