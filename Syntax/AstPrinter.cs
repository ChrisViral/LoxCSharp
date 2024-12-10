using Lox.Syntax.Expressions;

namespace Lox.Syntax;

/// <summary>
/// Abstract Syntax Tree printer
/// </summary>
public class AstPrinter : IExpressionVisitor<string>
{
    #region Methods
    /// <summary>
    /// Prints the given expression's AST representation to the log
    /// </summary>
    /// <param name="expression">Expression to print</param>
    public void Print(LoxExpression expression) => Console.WriteLine(expression.Accept(this));
    #endregion

    #region Expression visitor
    /// <inheritdoc />
    public string VisitLiteralExpression(LiteralExpression expression) => expression.Value.ASTString();

    /// <inheritdoc />
    public string VisitVariableExpression(VariableExpression expression) => expression.Identifier.Lexeme;

    /// <inheritdoc />
    public string VisitGroupingExpression(GroupingExpression expression) => $"(group {expression.InnerExpression.Accept(this)})";

    /// <inheritdoc />
    public string VisitUnaryExpression(UnaryExpression expression) => $"({expression.Operator.Lexeme} {expression.InnerExpression.Accept(this)})";

    /// <inheritdoc />
    public string VisitBinaryExpression(BinaryExpression expression) => $"({expression.Operator.Lexeme} {expression.LeftExpression.Accept(this)} {expression.RightExpression.Accept(this)})";

    /// <inheritdoc />
    public string VisitLogicalExpression(LogicalExpression expression) => VisitBinaryExpression(expression);

    /// <inheritdoc />
    public string VisitAssignmentExpression(AssignmentExpression expression) => $"(assign {expression.Identifier.Lexeme} {expression.Value.Accept(this)})";

    /// <inheritdoc />
    public string VisitAccessExpression(AccessExpression expression) => $"(access {expression.Target.Accept(this)}.{expression.Identifier.Lexeme})";

    /// <inheritdoc />
    public string VisitSetExpression(SetExpression expression) => $"(set {expression.Target.Accept(this)}.{expression.Identifier.Lexeme} {expression.Value.Accept(this)})";

    /// <inheritdoc />
    public string VisitInvokeExpression(InvokeExpression expression)
    {
        return expression.Arguments.Count > 0
                   ? $"(invoke {expression.Target.Accept(this)} {string.Join(' ', expression.Arguments.Select(a => a.Accept(this)))})"
                   : $"(invoke {expression.Target.Accept(this)})";
    }
    #endregion
}
