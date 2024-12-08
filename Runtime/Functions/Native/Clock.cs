namespace CodeCrafters.Interpreter.Runtime.Functions.Native;

[LoxNativeDefinition("clock")]
public sealed class Clock : LoxNativeFunction
{
    /// <inheritdoc />
    public override int Arity => 0;

    /// <summary>
    /// Returns a floating point value of the current unix time in seconds
    /// </summary>
    /// <inheritdoc/>
    public override LoxValue Invoke(LoxInterpreter interpreter, in ReadOnlySpan<LoxValue> arguments) => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d;
}
