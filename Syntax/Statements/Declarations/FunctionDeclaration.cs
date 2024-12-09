using System.Collections.ObjectModel;
using System.Text;
using Lox.Scanning;

namespace Lox.Syntax.Statements.Declarations;

/// <summary>
/// Function declaration statement
/// </summary>
/// <param name="Identifier">Function identifier</param>
/// <param name="Parameters">Function parameters</param>
/// <param name="Body">Function block body</param>
public sealed record FunctionDeclaration(Token Identifier, ReadOnlyCollection<Token> Parameters, BlockStatement Body) : LoxStatement
{
    private const string INDENT = "    ";
    private static readonly StringBuilder FunctionBuilder = new();

    /// <inheritdoc />
    public override T Accept<T>(IStatementVisitor<T> visitor) => visitor.VisitFunctionDeclaration(this);

    /// <inheritdoc />
    public override string ToString()
    {
        FunctionBuilder.Append("fun ").Append(this.Identifier.Lexeme).Append('(');
        FunctionBuilder.AppendJoin(", ", this.Parameters.Select(p => p.Lexeme)).AppendLine(")");
        FunctionBuilder.Append(this.Body);
        string result = FunctionBuilder.ToString();
        FunctionBuilder.Clear();
        return result;
    }
}
