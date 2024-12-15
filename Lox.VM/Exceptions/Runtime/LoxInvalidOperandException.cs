using JetBrains.Annotations;
using Lox.VM.Scanner;

namespace Lox.VM.Exceptions.Runtime;

/// <summary>
/// Lox invalid operand exception
/// </summary>
[PublicAPI]
public sealed class LoxInvalidOperandException : LoxRuntimeException
{
    /// <inheritdoc />
    public LoxInvalidOperandException() { }

    /// <inheritdoc />
    public LoxInvalidOperandException(int line) : base(line) { }

    /// <inheritdoc />
    public LoxInvalidOperandException(string? message) : base(message) { }

    /// <inheritdoc />
    public LoxInvalidOperandException(string? message, int line) : base(message, line) { }

    /// <inheritdoc />
    public LoxInvalidOperandException(string? message, Exception? innerException) : base(message, innerException) { }

    /// <inheritdoc />
    public LoxInvalidOperandException(string? message, int line, Exception? innerException) : base(message, line, innerException) { }
}
