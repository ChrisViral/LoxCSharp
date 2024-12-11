using System.Collections.ObjectModel;
using Lox.Interpreter.Runtime.Functions;
using Lox.Interpreter.Runtime.Types;
using Lox.Interpreter.Scanner;
using Lox.Interpreter.Syntax.Expressions;
using Lox.Interpreter.Syntax.Statements;
using Lox.Interpreter.Syntax.Statements.Declarations;
using Lox.Interpreter.Utils;

namespace Lox.Interpreter;

/// <summary>
/// Lox resolver
/// </summary>
/// <param name="interpreter">Interpreter instance</param>
public sealed partial class LoxResolver(LoxInterpreter interpreter) : IExpressionVisitor, IStatementVisitor
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
    /// <summary>
    /// Current resolver type kind
    /// </summary>
    private TypeKind currentTypeKind;
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
            if (identifier.Type is not TokenType.THIS and not TokenType.SUPER && usages is 0)
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
}
