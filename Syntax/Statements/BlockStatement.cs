using System.Collections.ObjectModel;
using System.Text;

namespace CodeCrafters.Interpreter.Syntax.Statements;

/// <summary>
/// Block statement
/// </summary>
/// <param name="Statements">Statements contained in block</param>
public sealed record BlockStatement(ReadOnlyCollection<LoxStatement> Statements) : LoxStatement
{
    private const string INDENT = "    ";
    private static readonly StringBuilder BlockBuilder = new();

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitBlockStatement(this);

    /// <inheritdoc />
    public override string ToString()
    {
        BlockBuilder.AppendLine("{");
        foreach (LoxStatement statement in this.Statements)
        {
            BlockBuilder.Append(INDENT).AppendLine(statement.ToString());
        }
        BlockBuilder.Append('}');
        string block = BlockBuilder.ToString();
        BlockBuilder.Clear();
        return block;
    }
}
