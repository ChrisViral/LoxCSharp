using CodeCrafters.Interpreter.Exceptions;
using CodeCrafters.Interpreter.Exceptions.Runtime;
using CodeCrafters.Interpreter.Scanning;
using JetBrains.Annotations;

namespace CodeCrafters.Interpreter.Runtime;

/// <summary>
/// Lox runtime environment
/// </summary>
[PublicAPI]
public sealed partial class LoxEnvironment
{
    #region Constants
    /// <summary>
    /// Default stack capacity
    /// </summary>
    private const int DEFAULT_CAPACITY = 10;
    /// <summary>
    /// Stack depth limit
    /// </summary>
    private const int STACK_LIMIT = 100;
    /// <summary>
    /// Stack scope pool
    /// </summary>
    private static readonly Stack<Scope> ScopePool = [];
    #endregion

    #region Fields
    private readonly Scope globalScope;
    private readonly Stack<Scope> stack;
    private readonly IEqualityComparer<string> identifierComparer;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new Lox environment
    /// </summary>
    public LoxEnvironment() : this(DEFAULT_CAPACITY, StringComparer.Ordinal) { }

    /// <summary>
    /// Creates a new Lox environment
    /// </summary>
    /// <param name="stackCapacity">Stack depth initial capacity</param>
    public LoxEnvironment(int stackCapacity) : this(stackCapacity, StringComparer.Ordinal) { }

    /// <summary>
    /// Creates a new Lox environment
    /// </summary>
    /// <param name="identifierComparer">Variable name comparer</param>
    public LoxEnvironment(IEqualityComparer<string> identifierComparer) : this(DEFAULT_CAPACITY, identifierComparer) { }

    /// <summary>
    /// Creates a new Lox environment
    /// </summary>
    /// <param name="stackCapacity">Stack depth initial capacity</param>
    /// <param name="identifierComparer">Variable name comparer</param>
    public LoxEnvironment(int stackCapacity, IEqualityComparer<string> identifierComparer) : this(stackCapacity, identifierComparer, new Scope(identifierComparer)) { }

    /// <summary>
    /// Creates a new Lox environment from a parent global environment
    /// </summary>
    /// <param name="parent">Parent environment</param>
    /// <param name="closure">Environment closure</param>
    public LoxEnvironment(LoxEnvironment parent, Scope closure) : this(DEFAULT_CAPACITY, parent.identifierComparer, parent.globalScope) => PushScope(closure);

    /// <summary>
    /// Creates a new Lox environment
    /// </summary>
    /// <param name="stackCapacity">Stack depth initial capacity</param>
    /// <param name="identifierComparer">Variable name comparer</param>
    /// <param name="globalScope">Environment global scope</param>
    private LoxEnvironment(int stackCapacity, IEqualityComparer<string> identifierComparer, Scope globalScope)
    {
        this.identifierComparer = identifierComparer;
        this.stack              = new Stack<Scope>(stackCapacity);
        this.globalScope        = globalScope;
        this.stack.Push(this.globalScope);
    }
    #endregion

    #region Methods
    /// <summary>
    /// Pushes a new scope onto the stack
    /// </summary>
    /// <exception cref="StackOverflowException">The stack limit has been reached</exception>
    public void PushScope() => PushScope(GetFromPool());

    /// <summary>
    /// Pushes a new scope onto the stack
    /// </summary>
    /// <exception cref="StackOverflowException">The stack limit has been reached</exception>
    private void PushScope(Scope scope)
    {
        // Stack overflow
        if (this.stack.Count == STACK_LIMIT) throw new StackOverflowException("Lox stack limit reached");

        // Push onto stack
        this.stack.Push(scope);
    }

    /// <summary>
    /// Pops the top scope from the stack
    /// </summary>
    /// <exception cref="InvalidOperationException">The stack has no more scopes to push out</exception>
    public void PopScope()
    {
        // Stack underflow
        if (this.stack.Count is 1) throw new InvalidOperationException("Stack bottomed out, cannot pop another stack frame");

        // Store in pool for later reuse
        Scope scope = this.stack.Pop();
        scope.Clear();
        ScopePool.Push(scope);
    }

    /// <summary>
    /// Makes a closure from the current state of the stack
    /// </summary>
    /// <returns>The created closure</returns>
    public Scope MakeClosure()
    {
        Scope closure = GetFromPool();
        foreach (Scope scope in this.stack)
        {
            if (scope != this.globalScope)
            {
                scope.CopyVariables(closure);
            }
        }
        return closure;
    }

    /// <summary>
    /// Get a scope from the pool
    /// </summary>
    /// <returns>An empty <see cref="Scope"/> object</returns>
    private Scope GetFromPool()
    {
        // If possible, use a pooled scope object
        if (!ScopePool.TryPop(out Scope? scope))
        {
            scope = new Scope(this.identifierComparer);
        }
        return scope;
    }

    /// <summary>
    /// Defines a nil set variable at the top of the stack
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    public void DefineVariable(in Token identifier) => this.stack.Peek().DefineVariable(identifier, LoxValue.Nil);

    /// <summary>
    /// Defines a nil set global variable
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    public void DefineGlobalVariable(in Token identifier) => this.globalScope.DefineVariable(identifier, LoxValue.Nil);

    /// <summary>
    /// Defines a variable with the specified value at the top of the stack
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <param name="value">Variable value</param>
    public void DefineVariable(in Token identifier, in LoxValue value) => this.stack.Peek().DefineVariable(identifier, value);

    /// <summary>
    /// Defines a global variable with the specified value
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <param name="value">Variable value</param>
    public void DefineGlobalVariable(in Token identifier, in LoxValue value) => this.globalScope.DefineVariable(identifier, value);

    /// <summary>
    /// Sets the value of the topmost specified variable of the given name
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <param name="value">Variable value</param>
    /// <exception cref="LoxRuntimeException">If the variable is undefined</exception>
    public void SetVariable(in Token identifier, in LoxValue value)
    {
        foreach (Scope scope in this.stack)
        {
            if (scope.TrySetVariable(identifier, value))
            {
                return;
            }
        }

        throw new LoxRuntimeException($"Undefined variable '{identifier.Lexeme}'.");
    }

    /// <summary>
    /// Sets the value of the specified global variable
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <param name="value">Variable value</param>
    /// <exception cref="LoxRuntimeException">If the variable is undefined</exception>
    public void SetGlobalVariable(in Token identifier, in LoxValue value)
    {
        if (!this.globalScope.TrySetVariable(identifier, value))
        {
            throw new LoxRuntimeException($"Undefined variable '{identifier.Lexeme}'.");
        }
    }

    /// <summary>
    /// Gets the value of the specified variable in the stack
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <returns>The value of the variable</returns>
    /// <exception cref="LoxRuntimeException">If the variable is undefined</exception>
    public LoxValue GetVariable(in Token identifier)
    {
        foreach (Scope scope in this.stack)
        {
            if (scope.TryGetVariable(identifier, out LoxValue value))
            {
                return value;
            }
        }

        throw new LoxRuntimeException($"Undefined variable '{identifier.Lexeme}'.", identifier);
    }

    /// <summary>
    /// Gets the value of the specified global variable
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <returns>The value of the variable</returns>
    /// <exception cref="LoxRuntimeException">If the variable is undefined</exception>
    public LoxValue GetGlobalVariable(in Token identifier) => this.globalScope.TryGetVariable(identifier, out LoxValue value)
                                                                  ? value
                                                                  : throw new LoxRuntimeException($"Undefined variable '{identifier.Lexeme}'.", identifier);

    /// <summary>
    /// Checks if the specified variable exists somewhere within the stack
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <returns><see langword="true"/> if the variable exists, otherwise <see langword="false"/></returns>
    public bool IsVariableDefined(in Token identifier)
    {
        foreach (Scope scope in this.stack)
        {
            if (scope.IsVariableDefined(identifier))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the specified global variable exists
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <returns><see langword="true"/> if the variable exists, otherwise <see langword="false"/></returns>
    public bool IsGlobalVariableDefined(in Token identifier) => this.globalScope.IsVariableDefined(identifier);

    /// <summary>
    /// Deletes the specified variable from the stack
    /// </summary>
    /// <param name="identifier">Variable identifier to delete</param>
    /// <returns><see langword="true"/> if the variable was found and deleted, otherwise <see langword="false"/></returns>
    public bool DeleteVariable(in Token identifier)
    {
        foreach (Scope scope in this.stack)
        {
            if (scope.DeleteVariable(identifier))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Deletes the specified global variable
    /// </summary>
    /// <param name="identifier">Variable identifier to delete</param>
    /// <returns><see langword="true"/> if the variable was found and deleted, otherwise <see langword="false"/></returns>
    public bool DeleteGlobalVariable(in Token identifier) => this.globalScope.DeleteVariable(identifier);
    #endregion
}
