using System.Globalization;

namespace Lox.VM.Runtime;

public readonly struct LoxValue(in double value)
{
    public readonly double value = value;

    public override string ToString() => this.value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator LoxValue(in double value) => new(value);
}
