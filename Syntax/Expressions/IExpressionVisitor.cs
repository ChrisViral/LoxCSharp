namespace CodeCrafters.Interpreter.Syntax.Expressions;

/// <summary>
/// Expression visitor interface
/// </summary>
/// <typeparam name="T">Visitor return type</typeparam>
public interface IExpressionVisitor<out T>
{
    /// <summary>
    /// Handles <see cref="LiteralExpression"/> visits
    /// </summary>
    /// <param name="expression">Expression visited</param>
    /// <returns>The return value for the given expression</returns>
    T VisitLiteralExpression(LiteralExpression expression);

    /// <summary>
    /// Handles <see cref="VariableExpression"/> visits
    /// </summary>
    /// <param name="expression">Expression visited</param>
    /// <returns>The return value for the given expression</returns>
    T VisitVariableExpression(VariableExpression expression);

    /// <summary>
    /// Handles <see cref="GroupingExpression"/> visits
    /// </summary>
    /// <param name="expression">Expression visited</param>
    /// <returns>The return value for the given expression</returns>
    T VisitGroupingExpression(GroupingExpression expression);

    /// <summary>
    /// Handles <see cref="UnaryExpression"/> visits
    /// </summary>
    /// <param name="expression">Expression visited</param>
    /// <returns>The return value for the given expression</returns>
    T VisitUnaryExpression(UnaryExpression expression);

    /// <summary>
    /// Handles <see cref="BinaryExpression"/> visits
    /// </summary>
    /// <param name="expression">Expression visited</param>
    /// <returns>The return value for the given expression</returns>
    T VisitBinaryExpression(BinaryExpression expression);

    /// <summary>
    /// Handles <see cref="BinaryExpression"/> visits
    /// </summary>
    /// <param name="expression">Expression visited</param>
    /// <returns>The return value for the given expression</returns>
    T VisitLogicalExpression(LogicalExpression expression);

    /// <summary>
    /// Handles <see cref="AssignmentExpression"/> visits
    /// </summary>
    /// <param name="expression">Expression visited</param>
    /// <returns>The return value for the given expression</returns>
    T VisitAssignmentExpression(AssignmentExpression expression);

    /// <summary>
    /// Handles <see cref="InvokeExpression"/> visits
    /// </summary>
    /// <param name="expression">Expression visited</param>
    /// <returns>The return value for the given expression</returns>
    T VisitInvokeExpression(InvokeExpression expression);
}
