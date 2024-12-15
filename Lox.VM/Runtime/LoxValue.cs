using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lox.Common.Exceptions;
using Lox.Common.Utils;

namespace Lox.VM.Runtime;

/// <summary>
/// Literal value type
/// </summary>
public enum ValueType : byte
{
    INVALID,
    NIL,
    BOOLEAN,
    STRING,
    NUMBER,
    OBJECT
}

/// <summary>
/// Literal value wrapper<br/>
/// This is a union struct, so only one of the data fields may be populated at a time
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct LoxValue : IEquatable<LoxValue>
{
    #region Constants
    /// <summary>
    /// Invalid literal
    /// </summary>
    public static LoxValue Invalid { get; } = new(ValueType.INVALID);
    /// <summary>
    /// Nil literal
    /// </summary>
    public static LoxValue Nil { get; } = new(ValueType.NIL);
    /// <summary>
    /// True literal
    /// </summary>
    public static LoxValue True { get; } = new(true);
    /// <summary>
    /// False literal
    /// </summary>
    public static LoxValue False { get; } = new(false);
    #endregion

    #region Properties
    [FieldOffset(0)]
    private readonly bool boolValue;
    /// <summary>
    /// The boolean literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="ValueType.BOOLEAN"/></exception>
    public bool BoolValue
    {
        get
        {
            if (this.Type is not ValueType.BOOLEAN) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {ValueType.BOOLEAN}");
            return this.boolValue;
        }
    }

    [FieldOffset(0)]
    private readonly double numberValue;
    /// <summary>
    /// The double literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="ValueType.NUMBER"/></exception>
    public double NumberValue
    {
        get
        {
            if (this.Type is not ValueType.NUMBER) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {ValueType.NUMBER}");
            return this.numberValue;
        }
    }

    /// <summary>
    /// The boxed literal value
    /// </summary>
    /// <exception cref="InvalidOperationException">If an invalid literal value type is contained</exception>
    /// <exception cref="InvalidEnumArgumentException">Unknown literal type</exception>
    public object? BoxedValue => this.Type switch
    {
        ValueType.NIL     => null,
        ValueType.BOOLEAN => this.boolValue,
        ValueType.STRING  => null,
        ValueType.NUMBER  => this.numberValue,
        ValueType.OBJECT  => null,
        ValueType.INVALID => throw new InvalidOperationException("Invalid value type cannot be used"),
        _                 => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(ValueType))
    };

    /// <summary>
    /// Type of literal value this wrapper contains
    /// </summary>
    [field: FieldOffset(8)]
    public ValueType Type { get; }

    /// <summary>
    /// If this is an invalid literal
    /// </summary>
    public bool IsInvalid => this.Type is ValueType.INVALID;

    /// <summary>
    /// Checks if this value evaluates to something equivalent to true
    /// </summary>
    /// <returns><see langword="true"/> if the object is truthy, otherwise <see langword="false"/></returns>
    public bool IsTruthy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Type switch
        {
            ValueType.BOOLEAN => this.BoolValue,
            ValueType.NIL     => false,
            ValueType.INVALID => throw new InvalidOperationException("Invalid value type cannot be used"),
            _                 => true
        };
    }

    /// <summary>
    /// Checks if this value evaluates to something equivalent to false
    /// </summary>
    /// <returns><see langword="false"/> if the object is truthy, otherwise <see langword="true"/></returns>
    public bool IsFalsey
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Type switch
        {
            ValueType.BOOLEAN => !this.BoolValue,
            ValueType.NIL     => true,
            ValueType.INVALID => throw new InvalidOperationException("Invalid value type cannot be used"),
            _                 => false
        };
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new <see cref="bool"/> value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    public LoxValue(bool value) : this(ValueType.BOOLEAN) => this.boolValue   = value;

    /// <summary>
    /// Creates a new <see cref="double"/> value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    public LoxValue(double value) : this(ValueType.NUMBER) => this.numberValue = value;

    /// <summary>
    /// Creates a new uninitialized with the given type
    /// </summary>
    /// <param name="type">Literal type</param>
    private LoxValue(ValueType type) => this.Type = type;
    #endregion

    #region Methods
    /// <summary>
    /// Tries to get this value as a boolean
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <returns><see langword="true"/> if this value is a <see cref="bool"/>, otherwise <see langword="false"/></returns>
    public bool TryGetBool(out bool output)
    {
        if (this.Type is ValueType.BOOLEAN)
        {
            output = this.boolValue;
            return true;
        }

        output = false;
        return false;
    }

    /// <summary>
    /// Tries to get this value as a number
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <returns><see langword="true"/> if this value is a <see cref="double"/>, otherwise <see langword="false"/></returns>
    public bool TryGetNumber(out double output)
    {
        if (this.Type is ValueType.NUMBER)
        {
            output = this.numberValue;
            return true;
        }

        output = 0;
        return false;
    }

    /// <summary>
    /// Returns a string representation of whatever literal value is contained within this wrapper
    /// </summary>
    /// <returns>The string representation of the literal value</returns>
    /// <exception cref="InvalidOperationException">If an invalid literal value type is contained</exception>
    /// <exception cref="InvalidEnumArgumentException">Unknown literal type</exception>
    public override string ToString() => this.Type switch
    {
        ValueType.NIL     => LoxUtils.NilString,
        ValueType.BOOLEAN => this.boolValue ? LoxUtils.TrueString : LoxUtils.FalseString,
        ValueType.STRING  => null!,
        ValueType.NUMBER  => this.numberValue.ToString(CultureInfo.InvariantCulture),
        ValueType.OBJECT  => null!,
        ValueType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                 => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(ValueType))
    };

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(in LoxValue other) => this.Type == other.Type && this.Type switch
    {
        ValueType.NIL     => true,
        ValueType.BOOLEAN => this.boolValue == other.boolValue,
        ValueType.STRING  => default,
        ValueType.NUMBER  => this.numberValue.Equals(other.numberValue),
        ValueType.OBJECT  => default,
        ValueType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                 => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(ValueType)),
    };

    /// <inheritdoc />
    bool IEquatable<LoxValue>.Equals(LoxValue other) => Equals(in other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LoxValue other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => this.Type switch
    {
        ValueType.NIL     => throw new NullReferenceException("Cannot get the hashcode of a nil"),
        ValueType.BOOLEAN => this.boolValue.GetHashCode(),
        ValueType.STRING  => default,
        ValueType.NUMBER  => this.numberValue.GetHashCode(),
        ValueType.OBJECT  => default,
        ValueType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                 => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(ValueType)),
    };
    #endregion

    #region Operators
    /// <summary>
    /// Casts the given boolean to a LoxValue
    /// </summary>
    /// <param name="value">Boolean to cast</param>
    /// <returns>A <see cref="LoxValue"/> representing the given <see cref="bool"/></returns>
    public static implicit operator LoxValue(in bool value) => new(value);

    /// <summary>
    /// Casts the given LoxValue to a boolean
    /// </summary>
    /// <param name="value">LoxValue to cast</param>
    /// <returns>The casted <see cref="bool"/> value</returns>
    public static explicit operator bool(in LoxValue value) => value.BoolValue;

    /// <summary>
    /// Casts the given double to a LoxValue
    /// </summary>
    /// <param name="value">Double to cast</param>
    /// <returns>A <see cref="LoxValue"/> representing the given <see cref="double"/></returns>
    public static implicit operator LoxValue(in double value) => new(value);

    /// <summary>
    /// Casts the given LoxValue to a double
    /// </summary>
    /// <param name="value">LoxValue to cast</param>
    /// <returns>The casted <see cref="double"/> value</returns>
    public static explicit operator double(in LoxValue value) => value.NumberValue;

    /// <summary>
    /// Equality operator on two LoxValues
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns><see landword="true"/> if both values are equal, otherwise <see landword="false"/></returns>
    public static bool operator ==(in LoxValue left, in LoxValue right) => left.Equals(right);

    /// <summary>
    /// Inequality operator on two LoxValues
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns><see landword="true"/> if both values are unequal, otherwise <see landword="false"/></returns>
    public static bool operator !=(in LoxValue left, in LoxValue right) => !left.Equals(right);
    #endregion
}
