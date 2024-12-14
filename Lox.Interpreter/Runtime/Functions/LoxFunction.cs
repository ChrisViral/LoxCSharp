using System.ComponentModel;
using FastEnumUtility;
using Lox.Interpreter.Exceptions.Runtime;
using Lox.Interpreter.Scanner;

namespace Lox.Interpreter.Runtime.Functions;

/// <summary>
/// Lox function type
/// </summary>
public enum FunctionKind
{
    NONE,
    FUNCTION,
    METHOD,
    CONSTRUCTOR,
    NATIVE
}

[FastEnum<FunctionKind>]
internal sealed partial class FunctionKindBooster;

/// <summary>
/// Lox function object
/// </summary>
public abstract class LoxFunction : LoxObject, IInvokable
{
    /// <summary>
    /// Function kind
    /// </summary>
    public FunctionKind Kind { get; protected init; }

    /// <summary>
    /// Function identifier
    /// </summary>
    public Token Identifier { get; protected init; }

    /// <inheritdoc />
    public virtual int Arity { get; protected init; }

    /// <summary>
    /// Creates a new Lox function with the specified name
    /// </summary>
    /// <param name="identifier">Function identifier</param>
    /// <param name="kind">Function kind</param>
    protected LoxFunction(in Token identifier, FunctionKind kind)
    {
        this.Kind       = kind;
        this.Identifier = identifier;
    }

    /// <inheritdoc />
    public abstract LoxValue Invoke(LoxInterpreter interpreter, in ReadOnlySpan<LoxValue> arguments);

    /// <summary>
    /// String representation of the function
    /// </summary>
    /// <returns>String representation of the function</returns>
    /// <exception cref="LoxRuntimeException">Invalid function type</exception>
    /// <exception cref="InvalidEnumArgumentException">Unknown function type</exception>
    public override string ToString()
    {
        string kind = this.Kind switch
        {
            FunctionKind.FUNCTION    => "fn",
            FunctionKind.METHOD      => "mthd",
            FunctionKind.CONSTRUCTOR => "ctor",
            FunctionKind.NATIVE      => "fn-native",
            FunctionKind.NONE        => throw new LoxRuntimeException("Invalid function type"),
            _                        => throw new InvalidEnumArgumentException(nameof(this.Kind), (int)this.Kind, typeof(FunctionKind))
        };
        return $"<{kind} {this.Identifier.Lexeme}>";
    }
}
