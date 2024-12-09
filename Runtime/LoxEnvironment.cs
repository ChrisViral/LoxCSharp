using JetBrains.Annotations;
using Lox.Exceptions.Runtime;
using Lox.Scanning;

namespace Lox.Runtime;

/// <summary>
/// Lox runtime environment
/// </summary>
public sealed partial class LoxEnvironment
{
    #region Constants
    /// <summary>
    /// Default stack capacity
    /// </summary>
    private const int DEFAULT_CAPACITY = 4;
    /// <summary>
    /// Stack depth limit
    /// </summary>
    private const int STACK_LIMIT = byte.MaxValue;
    #endregion

    #region Fields
    private readonly Scope globalScope;
    private readonly List<Scope> stack;
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
    /// <param name="identifierComparer">Variable name comparer</param>
    public LoxEnvironment(int stackCapacity, IEqualityComparer<string> identifierComparer)
    {
        this.identifierComparer = identifierComparer;
        this.stack              = new List<Scope>(stackCapacity);
        this.globalScope        = new Scope(identifierComparer);
        this.stack.Add(this.globalScope);
    }

    /// <summary>
    /// Lox environment clone constructor
    /// </summary>
    /// <param name="other">Other environment to clone</param>
    private LoxEnvironment(LoxEnvironment other)
    {
        this.identifierComparer = other.identifierComparer;
        this.stack              = new List<Scope>(other.stack.Capacity);
        this.stack.AddRange(other.stack);
        this.globalScope = other.globalScope;
    }
    #endregion

    #region Methods
    /// <summary>
    /// Pushes a new scope onto the stack
    /// </summary>
    /// <exception cref="StackOverflowException">The stack limit has been reached</exception>
    public void PushScope()
    {
        // Stack overflow
        if (this.stack.Count == STACK_LIMIT) throw new StackOverflowException("Lox stack limit reached");

        // Push onto stack
        this.stack.Add(new Scope(this.identifierComparer));
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
        this.stack.RemoveAt(this.stack.Count - 1);
    }

    /// <summary>
    /// Captures the current environment as a closure
    /// </summary>
    /// <returns>The captured environment</returns>
    public LoxEnvironment Capture() => new(this);

    /// <summary>
    /// Defines a nil set variable at the top of the stack
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    public void DefineVariable(in Token identifier) => this.stack[^1].DefineVariable(identifier, LoxValue.Nil);

    /// <summary>
    /// Defines a nil set variable at the top of the stack
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <param name="index">Index to define the variable at</param>
    public void DefineVariableAt(in Token identifier, in Index index) => this.stack[index].DefineVariable(identifier, LoxValue.Nil);

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
    public void DefineVariable(in Token identifier, in LoxValue value) => this.stack[^1].DefineVariable(identifier, value);

    /// <summary>
    /// Defines a variable with the specified value at the top of the stack
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <param name="value">Variable value</param>
    /// <param name="index">Index to define the variable at</param>
    public void DefineVariableAt(in Token identifier, in LoxValue value, in Index index) => this.stack[index].DefineVariable(identifier, value);

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
    /// Sets the value of the topmost specified variable of the given name
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <param name="value">Variable value</param>
    /// <param name="index">Index to set the variable at</param>
    /// <exception cref="LoxRuntimeException">If the variable is undefined</exception>
    public void SetVariableAt(in Token identifier, in LoxValue value, in Index index)
    {
        if (!this.stack[index].TrySetVariable(identifier, value))
        {
            throw new LoxRuntimeException($"Undefined variable '{identifier.Lexeme}'.");
        }
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
    /// Gets the value of the specified variable in the stack
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <param name="index">Index to get the variable at</param>
    /// <returns>The value of the variable</returns>
    /// <exception cref="LoxRuntimeException">If the variable is undefined</exception>
    public LoxValue GetVariableAt(in Token identifier, in Index index) => this.stack[index].TryGetVariable(identifier, out LoxValue value)
                                                                              ? value
                                                                              : throw new LoxRuntimeException($"Undefined variable '{identifier.Lexeme}'.", identifier);

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
    /// Checks if the specified variable exists somewhere within the stack
    /// </summary>
    /// <param name="identifier">Variable identifier token</param>
    /// <param name="index">Index to check the variable at</param>
    /// <returns><see langword="true"/> if the variable exists, otherwise <see langword="false"/></returns>
    public bool IsVariableDefinedAt(in Token identifier, in Index index) => this.stack[index].IsVariableDefined(identifier);

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
    /// Deletes the specified variable from the stack
    /// </summary>
    /// <param name="identifier">Variable identifier to delete</param>
    /// <param name="index">Index to delete the variable at</param>
    /// <returns><see langword="true"/> if the variable was found and deleted, otherwise <see langword="false"/></returns>
    public bool DeleteVariableAt(in Token identifier, in Index index) => this.stack[index].DeleteVariable(identifier);

    /// <summary>
    /// Deletes the specified global variable
    /// </summary>
    /// <param name="identifier">Variable identifier to delete</param>
    /// <returns><see langword="true"/> if the variable was found and deleted, otherwise <see langword="false"/></returns>
    public bool DeleteGlobalVariable(in Token identifier) => this.globalScope.DeleteVariable(identifier);
    #endregion
}
