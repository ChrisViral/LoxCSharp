using Lox.Syntax.Statements.Declarations;

namespace Lox.Syntax.Statements;

/// <summary>
/// Statement visitor interface
/// </summary>
/// <typeparam name="T">Visitor return type</typeparam>
public interface IStatementVisitor<out T>
{
    /// <summary>
    /// Handles <see cref="ExpressionStatement"/> visits
    /// </summary>
    /// <param name="statement">Statement visited</param>
    /// <returns>The return value for the given statement</returns>
    T VisitExpressionStatement(ExpressionStatement statement);

    /// <summary>
    /// Handles <see cref="PrintStatement"/> visits
    /// </summary>
    /// <param name="statement">Statement visited</param>
    /// <returns>The return value for the given statement</returns>
    T VisitPrintStatement(PrintStatement statement);

    /// <summary>
    /// Handles <see cref="ReturnStatement"/> visits
    /// </summary>
    /// <param name="statement">Statement visited</param>
    /// <returns>The return value for the given statement</returns>
    T VisitReturnStatement(ReturnStatement statement);

    /// <summary>
    /// Handles <see cref="IfStatement"/> visits
    /// </summary>
    /// <param name="statement">Statement visited</param>
    /// <returns>The return value for the given statement</returns>
    T VisitIfStatement(IfStatement statement);

    /// <summary>
    /// Handles <see cref="WhileStatement"/> visits
    /// </summary>
    /// <param name="statement">Statement visited</param>
    /// <returns>The return value for the given statement</returns>
    T VisitWhileStatement(WhileStatement statement);

    /// <summary>
    /// Handles <see cref="WhileStatement"/> visits
    /// </summary>
    /// <param name="statement">Statement visited</param>
    /// <returns>The return value for the given statement</returns>
    T VisitForStatement(ForStatement statement);

    /// <summary>
    /// Handles <see cref="BlockStatement"/> visits
    /// </summary>
    /// <param name="block">Block visited</param>
    /// <returns>The return value for the given statement</returns>
    T VisitBlockStatement(BlockStatement block);

    /// <summary>
    /// Handles <see cref="VariableDeclaration"/> visits
    /// </summary>
    /// <param name="declaration">Statement visited</param>
    /// <returns>The return value for the given statement</returns>
    T VisitVariableDeclaration(VariableDeclaration declaration);

    /// <summary>
    /// Handles <see cref="FunctionDeclaration"/> visits
    /// </summary>
    /// <param name="declaration">Declaration visited</param>
    /// <returns>The return value for the given declaration</returns>
    T VisitFunctionDeclaration(FunctionDeclaration declaration);
}
