using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lox.Common.Exceptions;
using Lox.Common.Utils;

namespace Lox.VM.Runtime;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

/// <summary>
/// Literal value type
/// </summary>
public enum LoxValueType : byte
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
public readonly unsafe struct LoxValue : IEquatable<LoxValue>
{
    #region Constants
    /// <summary>
    /// Invalid literal
    /// </summary>
    public static LoxValue Invalid { get; } = new(LoxValueType.INVALID);
    /// <summary>
    /// Nil literal
    /// </summary>
    public static LoxValue Nil { get; } = new(LoxValueType.NIL);
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
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="LoxValueType.BOOLEAN"/></exception>
    public bool BoolValue
    {
        get
        {
            if (this.Type is not LoxValueType.BOOLEAN) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {LoxValueType.BOOLEAN}");
            return this.boolValue;
        }
    }

    [FieldOffset(0)]
    private readonly double numberValue;
    /// <summary>
    /// The double literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="LoxValueType.NUMBER"/></exception>
    public double NumberValue
    {
        get
        {
            if (this.Type is not LoxValueType.NUMBER) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {LoxValueType.NUMBER}");
            return this.numberValue;
        }
    }

    [FieldOffset(0)]
    private readonly char* stringPointer;
    /// <summary>
    /// The string literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="LoxValueType.STRING"/></exception>
    public ReadOnlySpan<char> StringValue
    {
        get
        {
            if (this.Type is not LoxValueType.STRING) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {LoxValueType.STRING}");
            return new ReadOnlySpan<char>(this.stringPointer, this.StringLength);
        }
    }
    /// <summary>
    /// The string literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="LoxValueType.STRING"/></exception>
    public char* StringPointer
    {
        get
        {
            if (this.Type is not LoxValueType.STRING) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {LoxValueType.STRING}");
            return this.stringPointer;
        }
    }

    [FieldOffset(0)]
    private readonly object* objectPointer;
    /// <summary>
    /// The object literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="LoxValueType.OBJECT"/></exception>
    public object ObjectValue
    {
        get
        {
            if (this.Type is not LoxValueType.OBJECT) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {LoxValueType.OBJECT}");
            return *this.objectPointer;
        }
    }

    /// <summary>
    /// The boxed literal value
    /// </summary>
    /// <exception cref="InvalidOperationException">If an invalid literal value type is contained</exception>
    /// <exception cref="InvalidEnumArgumentException">Unknown literal type</exception>
    public object? BoxedValue => this.Type switch
    {
        LoxValueType.NIL     => null,
        LoxValueType.BOOLEAN => this.boolValue,
        LoxValueType.STRING  => *this.stringPointer,
        LoxValueType.NUMBER  => this.numberValue,
        LoxValueType.OBJECT  => *this.objectPointer,
        LoxValueType.INVALID => throw new InvalidOperationException("Invalid value type cannot be used"),
        _                 => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LoxValueType))
    };

    [field: FieldOffset(8)]
    public int StringLength { get; } = 1;

    /// <summary>
    /// Type of literal value this wrapper contains
    /// </summary>
    [field: FieldOffset(12)]
    public LoxValueType Type { get; }

    /// <summary>
    /// If this is an invalid literal
    /// </summary>
    public bool IsInvalid => this.Type is LoxValueType.INVALID;

    /// <summary>
    /// Checks if this value evaluates to something equivalent to true
    /// </summary>
    /// <returns><see langword="true"/> if the object is truthy, otherwise <see langword="false"/></returns>
    public bool IsTruthy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Type switch
        {
            LoxValueType.BOOLEAN => this.BoolValue,
            LoxValueType.NIL     => false,
            LoxValueType.INVALID => throw new InvalidOperationException("Invalid value type cannot be used"),
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
            LoxValueType.BOOLEAN => !this.BoolValue,
            LoxValueType.NIL     => true,
            LoxValueType.INVALID => throw new InvalidOperationException("Invalid value type cannot be used"),
            _                 => false
        };
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new <see cref="bool"/> value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    public LoxValue(bool value) : this(LoxValueType.BOOLEAN) => this.boolValue   = value;

    /// <summary>
    /// Creates a new <see cref="double"/> value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    public LoxValue(double value) : this(LoxValueType.NUMBER) => this.numberValue = value;

    /// <summary>
    /// Creates a new <see cref="string"/> value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    /// <param name="length">String length</param>
    public LoxValue(char* value, int length) : this(LoxValueType.STRING)
    {
        this.stringPointer = value;
        this.StringLength  = length;
    }

    /// <summary>
    /// Creates a new <see cref="object"/> value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    public LoxValue(object* value) : this(LoxValueType.OBJECT) => this.objectPointer = value;

    /// <summary>
    /// Creates a new uninitialized with the given type
    /// </summary>
    /// <param name="type">Literal type</param>
    private LoxValue(LoxValueType type) => this.Type = type;
    #endregion

    #region Methods
    /// <summary>
    /// Tries to get this value as a boolean
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <returns><see langword="true"/> if this value is a <see cref="bool"/>, otherwise <see langword="false"/></returns>
    public bool TryGetBool(out bool output)
    {
        if (this.Type is LoxValueType.BOOLEAN)
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
        if (this.Type is LoxValueType.NUMBER)
        {
            output = this.numberValue;
            return true;
        }

        output = 0;
        return false;
    }

    /// <summary>
    /// Tries to get this value as a string
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <returns><see langword="true"/> if this value is a <see cref="string"/>, otherwise <see langword="false"/></returns>
    public bool TryGetString(out ReadOnlySpan<char> output)
    {
        if (this.Type is LoxValueType.STRING)
        {
            output = new ReadOnlySpan<char>(this.stringPointer, this.StringLength);
            return true;
        }

        output = ReadOnlySpan<char>.Empty;
        return false;
    }

    /// <summary>
    /// Tries to get this value as a string
    /// </summary>
    /// <param name="pointer">Output value parameter</param>
    /// <param name="length">Length of the raw string</param>
    /// <returns><see langword="true"/> if this value is a <see cref="string"/>, otherwise <see langword="false"/></returns>
    public bool TryGetStringRaw(out char* pointer, out int length)
    {
        if (this.Type is LoxValueType.STRING)
        {
            pointer = this.stringPointer;
            length  = this.StringLength;
            return true;
        }

        pointer = null;
        length  = 0;
        return false;
    }

    /// <summary>
    /// Tries to get this value as an object
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <returns><see langword="true"/> if this value is a <see cref="object"/>, otherwise <see langword="false"/></returns>
    public bool TryGetObject([MaybeNullWhen(false)] out object output)
    {
        if (this.Type is LoxValueType.OBJECT)
        {
            output = *this.objectPointer;
            return true;
        }

        output = null;
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
        LoxValueType.NIL     => LoxUtils.NilString,
        LoxValueType.BOOLEAN => this.boolValue ? LoxUtils.TrueString : LoxUtils.FalseString,
        LoxValueType.STRING  => new string(this.stringPointer, 0, this.StringLength),
        LoxValueType.NUMBER  => this.numberValue.ToString(CultureInfo.InvariantCulture),
        LoxValueType.OBJECT  => this.objectPointer->ToString()!,
        LoxValueType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                    => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LoxValueType))
    };

    /// <summary>
    /// Checks if the other value is equal to this
    /// </summary>
    /// <param name="other">Other value to check</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> is equals to this value, otherwise <see langword="false"/></returns>
    /// <exception cref="InvalidOperationException">If this value's type is <see cref="LoxValueType.INVALID"/></exception>
    /// <exception cref="InvalidEnumArgumentException">For unknown values of <see cref="Type"/></exception>
    public bool Equals(in LoxValue other) => this.Type == other.Type && this.Type switch
    {
        LoxValueType.NIL     => true,
        LoxValueType.BOOLEAN => this.boolValue == other.boolValue,
        LoxValueType.STRING  => this.stringPointer == other.stringPointer || this.stringPointer->Equals(*other.stringPointer),
        LoxValueType.NUMBER  => this.numberValue.Equals(other.numberValue),
        LoxValueType.OBJECT  => this.objectPointer == other.objectPointer || this.objectPointer->Equals(*other.objectPointer),
        LoxValueType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                 => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LoxValueType)),
    };

    /// <summary>
    /// Checks if the other value isn't equal to this
    /// </summary>
    /// <param name="other">Other value to check</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> isn't equals to this value, otherwise <see langword="false"/></returns>
    /// <exception cref="InvalidOperationException">If this value's type is <see cref="LoxValueType.INVALID"/></exception>
    /// <exception cref="InvalidEnumArgumentException">For unknown values of <see cref="Type"/></exception>
    public bool NotEquals(in LoxValue other) => this.Type == other.Type && this.Type switch
    {
        LoxValueType.NIL     => false,
        LoxValueType.BOOLEAN => this.boolValue != other.boolValue,
        LoxValueType.STRING  => this.stringPointer != other.stringPointer || !this.stringPointer->Equals(*other.stringPointer),
        LoxValueType.NUMBER  => !this.numberValue.Equals(other.numberValue),
        LoxValueType.OBJECT  => this.objectPointer != other.objectPointer || !this.objectPointer->Equals(*other.objectPointer),
        LoxValueType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                 => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LoxValueType)),
    };

    /// <inheritdoc />
    bool IEquatable<LoxValue>.Equals(LoxValue other) => Equals(in other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LoxValue other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => this.Type switch
    {
        LoxValueType.NIL     => throw new NullReferenceException("Cannot get the hashcode of a nil"),
        LoxValueType.BOOLEAN => this.boolValue.GetHashCode(),
        LoxValueType.STRING  => string.GetHashCode(new ReadOnlySpan<char>(this.stringPointer, this.StringLength)),
        LoxValueType.NUMBER  => this.numberValue.GetHashCode(),
        LoxValueType.OBJECT  => this.objectPointer->GetHashCode(),
        LoxValueType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                    => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LoxValueType)),
    };

    /// <summary>
    /// Frees the resources associated to this value
    /// </summary>
    public void FreeResources()
    {
        switch (this.Type)
        {
            case LoxValueType.STRING:
                Marshal.FreeHGlobal((IntPtr)this.stringPointer);
                return;
        }
    }
    #endregion

    #region Operators
    /// <summary>
    /// Casts the given boolean to a LoxValue
    /// </summary>
    /// <param name="value">Boolean to cast</param>
    /// <returns>A <see cref="LoxValue"/> representing the given <see cref="bool"/></returns>
    public static implicit operator LoxValue(bool value) => new(value);

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
    public static implicit operator LoxValue(double value) => new(value);

    /// <summary>
    /// Casts the given LoxValue to a double
    /// </summary>
    /// <param name="value">LoxValue to cast</param>
    /// <returns>The casted <see cref="double"/> value</returns>
    public static explicit operator double(in LoxValue value) => value.NumberValue;

    /// <summary>
    /// Casts the given LoxValue to a string
    /// </summary>
    /// <param name="value">LoxValue to cast</param>
    /// <returns>The casted <see cref="string"/> value</returns>
    public static explicit operator ReadOnlySpan<char>(in LoxValue value) => value.StringValue;

    /// <summary>
    /// Casts the given object pointer to a LoxValue
    /// </summary>
    /// <param name="value">Object to cast</param>
    /// <returns>A <see cref="LoxValue"/> representing the given <see cref="object"/></returns>
    public static implicit operator LoxValue(object* value) => new(value);

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
    public static bool operator !=(in LoxValue left, in LoxValue right) => left.NotEquals(right);
    #endregion
}
