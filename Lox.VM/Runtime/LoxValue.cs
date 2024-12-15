using System.Globalization;

namespace Lox.VM.Runtime;

public readonly struct LoxValue(double value)
{
    public readonly double value = value;

    public override string ToString() => this.value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator LoxValue(double value) => new(value);
}
