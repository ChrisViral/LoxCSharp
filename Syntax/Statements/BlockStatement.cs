using System.Collections.ObjectModel;
using System.Text;

namespace Lox.Syntax.Statements;

/// <summary>
/// Block statement
/// </summary>
/// <param name="Statements">Statements contained in block</param>
public sealed record BlockStatement(ReadOnlyCollection<LoxStatement> Statements) : LoxStatement
{
    /// <inheritdoc />
    public override void Accept(IStatementVisitor visitor) => visitor.VisitBlockStatement(this);

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitBlockStatement(this);
}
