namespace CodeCrafters.Interpreter.Interrupts;

/// <summary>
/// Lox interrupt base class
/// </summary>
public abstract class LoxInterrupt : Exception
{
    /// <inheritdoc />
    protected LoxInterrupt() { }
}
