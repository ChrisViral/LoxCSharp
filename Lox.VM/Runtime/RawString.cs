using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lox.VM.Runtime;

/// <summary>
/// Raw string struct
/// </summary>
/// <param name="pointer">String pointer</param>
/// <param name="length">string length</param>
[StructLayout(LayoutKind.Explicit)]
public readonly unsafe struct RawString(char* pointer, int length) : IEquatable<RawString>
{
    #region Fields
    /// <summary>
    /// String start pointer
    /// </summary>
    [FieldOffset(0)]
    public readonly char* pointer = pointer;
    /// <summary>
    /// String length
    /// </summary>
    [FieldOffset(8)]
    public readonly int length    = length;
    #endregion

    #region Methods
    /// <summary>
    /// Converts this RawString to a span
    /// </summary>
    /// <returns>Resulting span</returns>
    public ReadOnlySpan<char> AsSpan() => new(this.pointer, this.length);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals(in RawString other) => this.pointer == other.pointer && this.length == other.length
                                           || AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc />
    bool IEquatable<RawString>.Equals(RawString other) => Equals(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RawString other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => string.GetHashCode(AsSpan());

    /// <summary>
    /// Returns the string value contained within this raw string
    /// </summary>
    /// <returns>The string value contained within this raw string</returns>
    public override string ToString() => new(this.pointer, 0, this.length);
    #endregion

    #region Static methods
    /// <summary>
    /// Allocates a given span to the unmanaged heap.<br/>
    /// You <i>must</i> take ownership of the pointer returned by this function.
    /// </summary>
    /// <param name="valueSpan">Value to allocate to the heap</param>
    /// <param name="allocated">Allocated value output</param>
    /// <returns>The allocated unmanaged heat pointer for this string</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr Allocate(ReadOnlySpan<char> valueSpan, out RawString allocated)
    {
        int length = valueSpan.Length;
        IntPtr allocatedPtr = Marshal.AllocHGlobal(length * sizeof(char));
        char* allocatedValue = (char*)allocatedPtr;
        Span<char> allocatedSpan = new(allocatedValue, length);
        valueSpan.CopyTo(allocatedSpan);
        allocated = new RawString(allocatedValue, length);
        return allocatedPtr;
    }
    #endregion

    #region Operators
    /// <summary>
    /// Lox RawString equality
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns><see langword="true"/> if both raw strings are equal, otherwise <see langword="false"/></returns>
    public static bool operator ==(in RawString left, in RawString right) => left.Equals(right);

    /// <summary>
    /// Lox RawString inequality
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns><see langword="true"/> if both raw strings are unequal, otherwise <see langword="false"/></returns>
    public static bool operator !=(in RawString left, in RawString right) => !left.Equals(right);
    #endregion
}
