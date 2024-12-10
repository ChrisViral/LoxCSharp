using Lox.Scanning;

namespace Lox.Runtime.Types;

/// <summary>
/// Lox invokable interface
/// </summary>
public interface IInvokable
{
    /// <summary>
    /// Invocation arity
    /// </summary>
    int Arity { get; }

    /// <summary>
    /// Invokes this LoxObject with the specified parameters
    /// </summary>
    /// <param name="interpreter">Executing Lox interpreter</param>
    /// <param name="arguments">Invocation arguments</param>
    /// <returns>The resulting invocation value</returns>
    /// <returns>The resulting invocation value</returns>
    LoxValue Invoke(LoxInterpreter interpreter, in ReadOnlySpan<LoxValue> arguments);
}
