using System.Collections.ObjectModel;
using Lox.Runtime;
using Lox.Scanning;
using Lox.Syntax.Expressions;
using Lox.Syntax.Statements;
using Lox.Syntax.Statements.Declarations;
using Lox.Utils;

namespace Lox.Resolving;

/// <summary>
/// Lox resolver
/// </summary>
/// <param name="interpreter">Interpreter instance</param>
public sealed class LoxResolver(LoxInterpreter interpreter) : IExpressionVisitor, IStatementVisitor
{
    /// <summary>
    /// Variable initialization state
    /// </summary>
    private enum State
    {
        UNDEFINED,
        DEFINED
    }

    /// <summary>
    /// Resolver scope
    /// </summary>
    private sealed class Scope() : Dictionary<string, State>(StringComparer.Ordinal);

    #region Fields
    /// <summary>
    /// Interpreter instance
    /// </summary>
    private readonly LoxInterpreter interpreter = interpreter;
    private readonly Stack<Scope> scopes = [];
    #endregion

    #region Resolver
    /// <summary>
    /// Resolves a Lox program
    /// </summary>
    /// <param name="program">Lox program to resolve</param>
    public async Task ResolveAsync(ReadOnlyCollection<LoxStatement> program) => await Task.Run(() => Resolve(program));

    /// <summary>
    /// Resolves a Lox program
    /// </summary>
    /// <param name="program">Lox program to resolve</param>
    public void Resolve(ReadOnlyCollection<LoxStatement> program)
    {
        foreach (LoxStatement statement in program)
        {
            Resolve(statement);
        }
    }

    /// <summary>
    /// Resolves a Lox statement
    /// </summary>
    /// <param name="statement">Lox statement to resolve</param>
    public void Resolve(LoxStatement statement) => statement.Accept(this);

    /// <summary>
    /// Resolves a Lox expression
    /// </summary>
    /// <param name="expression">Lox expression to resolve</param>
    public void Resolve(LoxExpression expression) => expression.Accept(this);

    /// <summary>
    /// Opens a new scope
    /// </summary>
    private void OpenScope() => this.scopes.Push(new Scope());

    /// <summary>
    /// Closes an opened scope
    /// </summary>
    private void CloseScope() => this.scopes.Pop();

    /// <summary>
    /// Declares a variable in the current scope
    /// </summary>
    /// <param name="identifier">Variable identifier</param>
    private void DeclareVariable(in Token identifier)
    {
        if (this.scopes.TryPeek(out Scope? scope))
        {
            scope[identifier.Lexeme] = State.UNDEFINED;
        }
    }

    /// <summary>
    /// Defines a variable in the current scope
    /// </summary>
    /// <param name="identifier">Variable identifier</param>
    private void DefineVariable(in Token identifier)
    {
        if (this.scopes.TryPeek(out Scope? scope))
        {
            scope[identifier.Lexeme] = State.DEFINED;
        }
    }

    /// <summary>
    /// Resolves a local variable
    /// </summary>
    /// <param name="expression">Variable expression</param>
    /// <param name="identifier">Variable identifier</param>
    private void ResolveLocal(LoxExpression expression, in Token identifier)
    {
        int depth = 1;
        foreach (Scope scope in this.scopes)
        {
            if (scope.ContainsKey(identifier.Lexeme))
            {
                this.interpreter.SetResolveDepth(expression, ^depth);
                return;
            }
            depth++;
        }
    }

    /// <summary>
    /// Resolves a function
    /// </summary>
    /// <param name="function">Funciton declaration</param>
    private void ResolveFunction(FunctionDeclaration function)
    {
        OpenScope();
        foreach (Token parameter in function.Parameters)
        {
            DefineVariable(parameter);
        }
        Resolve(function.Body.Statements);
        CloseScope();
    }
    #endregion

    #region Statement visitor
    /// <inheritdoc />
    public void VisitExpressionStatement(ExpressionStatement statement) => Resolve(statement.Expression);

    /// <inheritdoc />
    public void VisitPrintStatement(PrintStatement statement) => Resolve(statement.Expression);

    /// <inheritdoc />
    public void VisitReturnStatement(ReturnStatement statement)
    {
        if (statement.Value is not null)
        {
            Resolve(statement.Value);
        }
    }

    /// <inheritdoc />
    public void VisitIfStatement(IfStatement statement)
    {
        Resolve(statement.Condition);
        Resolve(statement.IfBranch);
        if (statement.ElseBranch is not null)
        {
            Resolve(statement.ElseBranch);
        }
    }

    /// <inheritdoc />
    public void VisitWhileStatement(WhileStatement statement)
    {
        Resolve(statement.Condition);
        Resolve(statement.BodyStatement);
    }

    /// <inheritdoc />
    public void VisitForStatement(ForStatement statement)
    {
        bool hasScope = false;
        if (statement.Initializer is not null)
        {
            if (statement.Initializer is VariableDeclaration)
            {
                hasScope = true;
                OpenScope();
            }
            Resolve(statement.Initializer);
        }

        if (statement.Condition is not null)
        {
            Resolve(statement.Condition);
        }

        if (statement.Increment is not null)
        {
            Resolve(statement.Increment);
        }

        Resolve(statement.BodyStatement);

        if (hasScope)
        {
            CloseScope();
        }
    }

    /// <inheritdoc />
    public void VisitBlockStatement(BlockStatement block)
    {
        OpenScope();
        Resolve(block.Statements);
        CloseScope();
    }

    /// <inheritdoc />
    public void VisitVariableDeclaration(VariableDeclaration declaration)
    {
        DeclareVariable(declaration.Identifier);
        if (declaration.Initializer is not null)
        {
            Resolve(declaration.Initializer);
        }
        DefineVariable(declaration.Identifier);
    }

    /// <inheritdoc />
    public void VisitFunctionDeclaration(FunctionDeclaration declaration)
    {
        DefineVariable(declaration.Identifier);
        ResolveFunction(declaration);
    }
    #endregion

    #region Expression visitor
    /// <inheritdoc />
    public void VisitLiteralExpression(LiteralExpression expression) { }

    /// <inheritdoc />
    public void VisitVariableExpression(VariableExpression expression)
    {
        if (this.scopes.TryPeek(out Scope? scope)
         && scope.TryGetValue(expression.Identifier.Lexeme, out State state)
         && state is State.UNDEFINED)
        {
            LoxErrorUtils.ReportParseError(expression.Identifier, "Can't read local variable in its own initializer.");
        }

        ResolveLocal(expression, expression.Identifier);
    }

    /// <inheritdoc />
    public void VisitGroupingExpression(GroupingExpression expression) => Resolve(expression.InnerExpression);

    /// <inheritdoc />
    public void VisitUnaryExpression(UnaryExpression expression) => Resolve(expression.InnerExpression);

    /// <inheritdoc />
    public void VisitBinaryExpression(BinaryExpression expression)
    {
        Resolve(expression.LeftExpression);
        Resolve(expression.RightExpression);
    }

    /// <inheritdoc />
    public void VisitLogicalExpression(LogicalExpression expression)
    {
        Resolve(expression.LeftExpression);
        Resolve(expression.RightExpression);
    }

    /// <inheritdoc />
    public void VisitAssignmentExpression(AssignmentExpression expression)
    {
        Resolve(expression.Value);
        ResolveLocal(expression, expression.Identifier);
    }

    /// <inheritdoc />
    public void VisitInvokeExpression(InvokeExpression expression)
    {
        Resolve(expression.Target);
        foreach (LoxExpression argument in expression.Arguments)
        {
            Resolve(argument);
        }
    }
    #endregion
}
