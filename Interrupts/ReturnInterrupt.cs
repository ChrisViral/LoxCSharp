using CodeCrafters.Interpreter.Runtime;

namespace CodeCrafters.Interpreter.Interrupts;

/// <summary>
/// Lox return interrupt
/// </summary>
public class ReturnInterrupt : LoxInterrupt
{
    /// <summary>
    /// Return value
    /// </summary>
    public LoxValue Value { get; }

    /// <summary>
    /// Returns without a value
    /// </summary>
    public ReturnInterrupt() : this(LoxValue.Nil) { }

    /// <summary>
    /// Returns with the specified value
    /// </summary>
    /// <param name="value">Value to return</param>
    public ReturnInterrupt(in LoxValue value) => this.Value = value;
}
