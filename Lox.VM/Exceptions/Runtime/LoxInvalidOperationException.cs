using JetBrains.Annotations;
using Lox.VM.Scanner;

namespace Lox.VM.Exceptions.Runtime;

/// <summary>
/// Lox invalid operation exception
/// </summary>
[PublicAPI]
public sealed class LoxInvalidOperationException : LoxRuntimeException
{
    /// <inheritdoc />
    public LoxInvalidOperationException() { }

    /// <inheritdoc />
    public LoxInvalidOperationException(int line) : base(line) { }

    /// <inheritdoc />
    public LoxInvalidOperationException(string? message) : base(message) { }

    /// <inheritdoc />
    public LoxInvalidOperationException(string? message, int line) : base(message, line) { }

    /// <inheritdoc />
    public LoxInvalidOperationException(string? message, Exception? innerException) : base(message, innerException) { }

    /// <inheritdoc />
    public LoxInvalidOperationException(string? message, int line, Exception? innerException) : base(message, line, innerException) { }
}