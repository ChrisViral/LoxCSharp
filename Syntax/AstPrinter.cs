using Lox.Syntax.Expressions;
using Lox.Syntax.Statements;
using Lox.Syntax.Statements.Declarations;

namespace Lox.Syntax;

/// <summary>
/// Abstract Syntax Tree printer
/// </summary>
public class AstPrinter : IExpressionVisitor<string>, IStatementVisitor<string>
{
    #region Methods
    /// <summary>
    /// Prints the given statement's AST representation to the log
    /// </summary>
    /// <param name="statement">Statement to print</param>
    public void Print(LoxStatement statement) => Console.WriteLine(statement.Accept(this));

    /// <summary>
    /// Prints the given expression's AST representation to the log
    /// </summary>
    /// <param name="expression">Expression to print</param>
    public void Print(LoxExpression expression) => Console.WriteLine(expression.Accept(this));
    #endregion

    #region Statement visitor
    /// <inheritdoc />
    public string VisitExpressionStatement(ExpressionStatement statement)
    {
        return $"(expr {statement.Expression.Accept(this)})";
    }

    /// <inheritdoc />
    public string VisitPrintStatement(PrintStatement statement)
    {
        return $"(print {statement.Expression.Accept(this)})";
    }

    /// <inheritdoc />
    public string VisitReturnStatement(ReturnStatement statement)
    {
        return statement.Value is not null
                   ? $"(return {statement.Value.Accept(this)})"
                   : "(return)";
    }

    /// <inheritdoc />
    public string VisitIfStatement(IfStatement statement)
    {
        return statement.ElseBranch is not null
                   ? $"(if {statement.Condition.Accept(this)} {statement.IfBranch.Accept(this)} {statement.ElseBranch.Accept(this)})"
                   : $"(if {statement.Condition.Accept(this)} {statement.IfBranch.Accept(this)})";
    }

    /// <inheritdoc />
    public string VisitWhileStatement(WhileStatement statement)
    {
        return $"(while {statement.Condition.Accept(this)} {statement.BodyStatement.Accept(this)})";
    }

    /// <inheritdoc />
    public string VisitForStatement(ForStatement statement)
    {
        string result = "(for ";
        if (statement.Initializer is not null)
        {
            result += statement.Initializer.Accept(this) + " ";
        }
        if (statement.Condition is not null)
        {
            result += statement.Condition.Accept(this) + " ";
        }
        if (statement.Increment is not null)
        {
            result += statement.Increment.Accept(this) + " ";
        }
        return result + statement.BodyStatement.Accept(this) + ")";
    }

    /// <inheritdoc />
    public string VisitBlockStatement(BlockStatement block)
    {
        return block.Statements.Count > 0
                   ? $"(block {string.Join(' ', block.Statements.Select(s => s.Accept(this)))})"
                   : "(block)";
    }

    /// <inheritdoc />
    public string VisitVariableDeclaration(VariableDeclaration declaration)
    {
        return declaration.Initializer is not null
                   ? $"(var {declaration.Identifier.Lexeme} {declaration.Initializer.Accept(this)})"
                   : $"(var {declaration.Identifier.Lexeme})";
    }

    /// <inheritdoc />
    public string VisitFunctionDeclaration(FunctionDeclaration declaration)
    {
        string parameters = declaration.Parameters.Count > 0
                                ? $"(param {string.Join(' ', declaration.Parameters.Select(p => p.Lexeme))})"
                                : "(param)";
        return $"(fun {declaration.Identifier.Lexeme} {parameters} {declaration.Body.Accept(this)})";
    }
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
    public string VisitInvokeExpression(InvokeExpression expression)
    {
        return expression.Parameters.Count > 0
                   ? $"(invoke {expression.Target.Accept(this)} {string.Join(' ', expression.Parameters.Select(a => a.Accept(this)))})"
                   : $"(invoke {expression.Target.Accept(this)})";
    }
    #endregion
}
