using Lox.Scanning;

namespace Lox.Runtime.Types;

/// <summary>
/// Lox invokable object
/// </summary>
/// <param name="identifier">Object identifier</param>
public abstract class LoxInvokable(in Token identifier) : LoxObject
{
    /// <summary>
    /// Object identifier
    /// </summary>
    public Token Identifier { get; protected init; } = identifier;

    /// <summary>
    /// Invocation arity
    /// </summary>
    public virtual int Arity { get; protected init; }

    /// <summary>
    /// Invokes this LoxObject with the specified parameters
    /// </summary>
    /// <param name="interpreter">Executing Lox interpreter</param>
    /// <param name="arguments">Invocation arguments</param>
    /// <returns>The resulting invocation value</returns>
    /// <returns>The resulting invocation value</returns>
    public abstract LoxValue Invoke(LoxInterpreter interpreter, in ReadOnlySpan<LoxValue> arguments);

    /// <inheritdoc/>
    public override string ToString() => $"<{this.Identifier.Lexeme}>";
}
