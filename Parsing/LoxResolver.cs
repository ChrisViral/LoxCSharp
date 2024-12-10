using System.Collections.ObjectModel;
using Lox.Runtime;
using Lox.Runtime.Types.Functions;
using Lox.Scanning;
using Lox.Syntax.Expressions;
using Lox.Syntax.Statements;
using Lox.Syntax.Statements.Declarations;
using Lox.Utils;

namespace Lox.Parsing;

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
    /// Variable data
    /// </summary>
    /// <param name="State">Variable initialization state</param>
    /// <param name="Usages">Variable usages</param>
    /// <param name="Identifier">Variable identifier</param>
    private readonly record struct VariableData(Token Identifier, State State, int Usages);

    /// <summary>
    /// Resolver scope
    /// </summary>
    private sealed class Scope() : Dictionary<string, VariableData>(DEFAULT_CAPACITY, StringComparer.Ordinal);

    #region Constants
    /// <summary>
    /// Default collection capacity
    /// </summary>
    private const int DEFAULT_CAPACITY = 4;
    #endregion

    #region Fields
    /// <summary>
    /// Interpreter instance
    /// </summary>
    private readonly LoxInterpreter interpreter = interpreter;
    /// <summary>
    /// Variable definition stack
    /// </summary>
    private readonly Stack<Scope> scopes = new(DEFAULT_CAPACITY);
    /// <summary>
    /// Current resolver function kind
    /// </summary>
    private FunctionKind currentFunctionKind;
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
    private void CloseScope()
    {
        Scope popped = this.scopes.Pop();
        foreach ((Token identifier, _, int usages) in popped.Values)
        {
            if (usages is 0)
            {
                LoxErrorUtils.ReportParseWarning(identifier, $"Unused variable '{identifier.Lexeme}'.");
            }
        }
    }

    /// <summary>
    /// Declares a variable in the current scope
    /// </summary>
    /// <param name="identifier">Variable identifier</param>
    /// <param name="state">Variable initial state, defaults to <see cref="State.UNDEFINED"/></param>
    private void DeclareVariable(in Token identifier, in State state = State.UNDEFINED)
    {
        if (this.scopes.TryPeek(out Scope? scope) && !scope.TryAdd(identifier.Lexeme, new VariableData(identifier, state, 0)))
        {
            LoxErrorUtils.ReportParseError(identifier, "Already a variable with this name in this scope.");
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
            scope[identifier.Lexeme] = new VariableData(identifier, State.DEFINED, 0);
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
            if (scope.TryGetValue(identifier.Lexeme, out VariableData data))
            {
                this.interpreter.SetResolveDepth(expression, ^depth);
                scope[identifier.Lexeme] = data with { Usages = data.Usages + 1 };
                return;
            }
            depth++;
        }
    }

    /// <summary>
    /// Resolves a function
    /// </summary>
    /// <param name="function">Function declaration</param>
    /// <param name="kind">Entered function kind</param>
    private void ResolveFunction(FunctionDeclaration function, FunctionKind kind)
    {
        FunctionKind enclosingKind = this.currentFunctionKind;
        this.currentFunctionKind = kind;
        OpenScope();
        foreach (Token parameter in function.Parameters)
        {
            DeclareVariable(parameter, State.DEFINED);
        }
        Resolve(function.Body.Statements);
        CloseScope();
        this.currentFunctionKind = enclosingKind;
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
        if (this.currentFunctionKind is FunctionKind.NONE)
        {
            LoxErrorUtils.ReportParseError(statement.Keyword, "Can't return from top-level code.");
        }

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
        DeclareVariable(declaration.Identifier, State.DEFINED);
        ResolveFunction(declaration, FunctionKind.FUNCTION);
    }

    /// <inheritdoc />
    public void VisitMethodDeclaration(MethodDeclaration declaration)
    {
        DeclareVariable(declaration.Identifier, State.DEFINED);
        ResolveFunction(declaration, FunctionKind.METHOD);
    }

    /// <inheritdoc />
    public void VisitClassDeclaration(ClassDeclaration declaration) => DeclareVariable(declaration.Identifier, State.DEFINED);
    #endregion

    #region Expression visitor
    /// <inheritdoc />
    public void VisitLiteralExpression(LiteralExpression expression) { }

    /// <inheritdoc />
    public void VisitVariableExpression(VariableExpression expression)
    {
        if (this.scopes.TryPeek(out Scope? scope)
         && scope.TryGetValue(expression.Identifier.Lexeme, out VariableData data)
         && data.State is State.UNDEFINED)
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
    public void VisitAccessExpression(AccessExpression expression) => Resolve(expression.Target);

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
