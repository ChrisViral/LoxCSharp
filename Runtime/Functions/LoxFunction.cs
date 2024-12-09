using System.ComponentModel;
using Lox.Scanning;

namespace Lox.Runtime.Functions;

/// <summary>
/// Lox function type
/// </summary>
public enum FunctionKind
{
    FUNCTION,
    METHOD,
    NATIVE
}

/// <summary>
/// Lox function object
/// </summary>
public abstract class LoxFunction : LoxInvokable
{
    /// <summary>
    /// Function identifier
    /// </summary>
    public Token Identifier { get; protected init; }

    /// <summary>
    /// Function kind
    /// </summary>
    public FunctionKind Kind { get; protected init; }

    /// <summary>
    /// Creates a new Lox function with the specified name
    /// </summary>
    /// <param name="identifier">Function identifier</param>
    /// <param name="kind">Function kind</param>
    protected LoxFunction(Token identifier, FunctionKind kind)
    {
        this.Identifier = identifier;
        this.Kind       = kind;
    }

    /// <summary>
    /// String representation of the function
    /// </summary>
    /// <returns>String representation of the function</returns>
    public override string ToString()
    {
        string kind = this.Kind switch
        {
            FunctionKind.FUNCTION => "fn",
            FunctionKind.METHOD   => "mthd",
            FunctionKind.NATIVE   => "fn-native",
            _                     => throw new InvalidEnumArgumentException(nameof(this.Kind), (int)this.Kind, typeof(FunctionKind))
        };
        return $"<{kind} {this.Identifier.Lexeme}>";
    }
}
