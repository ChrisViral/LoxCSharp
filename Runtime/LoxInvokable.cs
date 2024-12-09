namespace Lox.Runtime;

/// <summary>
/// Lox invokable object
/// </summary>
public abstract class LoxInvokable : LoxObject
{
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
}
