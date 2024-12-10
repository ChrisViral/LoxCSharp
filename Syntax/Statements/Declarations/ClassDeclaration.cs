using System.Collections.ObjectModel;
using Lox.Scanner;

namespace Lox.Syntax.Statements.Declarations;

/// <summary>
/// Class declaration statement
/// </summary>
/// <param name="Identifier">Class identifier</param>
/// <param name="Methods">Class functions</param>
public sealed record ClassDeclaration(in Token Identifier, ReadOnlyCollection<MethodDeclaration> Methods) : LoxDeclaration(Identifier)
{
    /// <inheritdoc />
    public override void Accept(IStatementVisitor visitor) => visitor.VisitClassDeclaration(this);

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitClassDeclaration(this);
}
