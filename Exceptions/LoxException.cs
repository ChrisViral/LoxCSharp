using JetBrains.Annotations;

namespace CodeCrafters.Interpreter.Exceptions;

/// <summary>
/// Lox exceptions base class
/// </summary>
[PublicAPI]
public abstract class LoxException : Exception
{
    /// <inheritdoc />
    protected LoxException() { }

    /// <inheritdoc />
    protected LoxException(string? message) : base(message) { }

    /// <inheritdoc />
    protected LoxException(string? message, Exception? innerException) : base(message, innerException) { }
}
