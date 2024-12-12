using System.Globalization;

namespace Lox.VM;

public readonly struct LoxValue(in double value)
{
    public readonly double value = value;

    public override string ToString() => this.value.ToString(CultureInfo.InvariantCulture);
}
