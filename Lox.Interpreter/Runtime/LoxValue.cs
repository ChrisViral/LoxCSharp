using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JetBrains.Annotations;
using Lox.Interpreter.Exceptions;

namespace Lox.Interpreter.Runtime;

/// <summary>
/// Literal value wrapper<br/>
/// This is a union struct, so only one of the data fields may be populated at a time
/// </summary>
[PublicAPI]
public readonly struct LoxValue : IEquatable<LoxValue>
{
    /// <summary>
    /// Literal value type
    /// </summary>
    public enum LiteralType : byte
    {
        INVALID,
        NIL,
        BOOLEAN,
        STRING,
        NUMBER,
        OBJECT
    }

    #region Constants
    /// <summary>
    /// Nil literal string value
    /// </summary>
    public const string NilString   = "nil";
    /// <summary>
    /// True literal string value
    /// </summary>
    public const string TrueString  = "true";
    /// <summary>
    /// False literal string value
    /// </summary>
    public const string FalseString = "false";


    /// <summary>
    /// Invalid literal
    /// </summary>
    public static LoxValue Invalid { get; } = new(LiteralType.INVALID);
    /// <summary>
    /// Nil literal
    /// </summary>
    public static LoxValue Nil { get; } = new(LiteralType.NIL);
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
    private readonly bool boolValue;
    /// <summary>
    /// The boolean literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="LiteralType.BOOLEAN"/></exception>
    public bool BoolValue
    {
        get
        {
            if (this.Type is not LiteralType.BOOLEAN) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {LiteralType.BOOLEAN}");
            return this.boolValue;
        }
    }

    private readonly string? stringValue;
    /// <summary>
    /// The string literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="LiteralType.STRING"/></exception>
    public string StringValue
    {
        get
        {
            if (this.Type is not LiteralType.STRING) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {LiteralType.STRING}");

            return this.stringValue!;
        }
    }

    private readonly double numberValue;
    /// <summary>
    /// The double literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="LiteralType.NUMBER"/></exception>
    public double NumberValue
    {
        get
        {
            if (this.Type is not LiteralType.NUMBER) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {LiteralType.NUMBER}");
            return this.numberValue;
        }
    }

    private readonly LoxObject? objectValue;
    /// <summary>
    /// The object literal value
    /// </summary>
    /// <exception cref="LoxInvalidLiteralTypeException">If the literal value of this wrapper is not <see cref="LiteralType.OBJECT"/></exception>
    public LoxObject ObjectValue
    {
        get
        {
            if (this.Type is not LiteralType.OBJECT) throw new LoxInvalidLiteralTypeException($"Wrapped literal type is {this.Type}, tried getting {LiteralType.OBJECT}");
            return this.objectValue!;
        }
    }

    /// <summary>
    /// The boxed literal value
    /// </summary>
    /// <exception cref="InvalidOperationException">If an invalid literal value type is contained</exception>
    /// <exception cref="InvalidEnumArgumentException">Unknown literal type</exception>
    public object? BoxedValue => this.Type switch
    {
        LiteralType.NIL     => null,
        LiteralType.BOOLEAN => this.boolValue,
        LiteralType.STRING  => this.stringValue,
        LiteralType.NUMBER  => this.numberValue,
        LiteralType.OBJECT  => this.ObjectValue,
        LiteralType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                   => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LiteralType))
    };

    /// <summary>
    /// Type of literal value this wrapper contains
    /// </summary>
    public LiteralType Type { get; }

    /// <summary>
    /// If this is an invalid literal
    /// </summary>
    public bool IsInvalid => this.Type is LiteralType.INVALID;

    /// <summary>
    /// Checks if this value evaluates to true or false
    /// </summary>
    /// <returns><see langword="true"/> if the object is truthy, otherwise <see langword="false"/></returns>
    public bool IsTruthy => this.Type switch
    {
        LiteralType.BOOLEAN => this.BoolValue,
        LiteralType.NIL     => false,
        LiteralType.INVALID => throw new InvalidOperationException("Invalid value type is invalid"),
        _                   => true
    };
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new <see cref="bool"/> literal value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    public LoxValue(bool value) : this(LiteralType.BOOLEAN) => this.boolValue   = value;

    /// <summary>
    /// Creates a new <see cref="string"/> literal value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    public LoxValue(string value) : this(LiteralType.STRING) => this.stringValue = value;

    /// <summary>
    /// Creates a new <see cref="double"/> literal value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    public LoxValue(double value) : this(LiteralType.NUMBER) => this.numberValue = value;

    /// <summary>
    /// Creates a new <see cref="object"/> literal value
    /// </summary>
    /// <param name="value">Value to wrap</param>
    public LoxValue(LoxObject value) : this(LiteralType.OBJECT) => this.objectValue = value;

    /// <summary>
    /// Creates a new uninitialized literal with the given type
    /// </summary>
    /// <param name="type">Literal type</param>
    private LoxValue(LiteralType type) => this.Type = type;
    #endregion

    #region Methods
    /// <summary>
    /// Tries to get this value as a boolean
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <returns><see langword="true"/> if this value is a <see cref="bool"/>, otherwise <see langword="false"/></returns>
    public bool TryGetBool(out bool output)
    {
        if (this.Type is LiteralType.BOOLEAN)
        {
            output = this.boolValue;
            return true;
        }

        output = default;
        return false;
    }

    /// <summary>
    /// Tries to get this value as a string
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <returns><see langword="true"/> if this value is a <see cref="string"/>, otherwise <see langword="false"/></returns>
    public bool TryGetString([MaybeNullWhen(false)] out string output)
    {
        if (this.Type is LiteralType.STRING)
        {
            output = this.stringValue!;
            return true;
        }

        output = null;
        return false;
    }

    /// <summary>
    /// Tries to get this value as a number
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <returns><see langword="true"/> if this value is a <see cref="double"/>, otherwise <see langword="false"/></returns>
    public bool TryGetNumber(out double output)
    {
        if (this.Type is LiteralType.NUMBER)
        {
            output = this.numberValue;
            return true;
        }

        output = default;
        return false;
    }

    /// <summary>
    /// Tries to get this value as a LoxObject
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <returns><see langword="true"/> if this value is a <see cref="LoxObject"/>, otherwise <see langword="false"/></returns>
    public bool TryGetObject([MaybeNullWhen(false)] out LoxObject output)
    {
        if (this.Type is LiteralType.OBJECT)
        {
            output = this.objectValue!;
            return true;
        }

        output = null;
        return false;
    }

    /// <summary>
    /// Tries to get this value as a typed LoxObject
    /// </summary>
    /// <param name="output">Output value parameter</param>
    /// <typeparam name="T">Lox object type</typeparam>
    /// <returns><see langword="true"/> if this value is a <see cref="LoxObject"/>, otherwise <see langword="false"/></returns>
    public bool TryGetObject<T>([MaybeNullWhen(false)] out T output)
    {
        if (this.Type is LiteralType.OBJECT && this.objectValue is T value)
        {
            output = value;
            return true;
        }

        output = default;
        return false;
    }

    /// <summary>
    /// Returns the token string of the given literal value
    /// </summary>
    /// <returns>The expression string of this literal</returns>
    /// <exception cref="InvalidOperationException">If an invalid literal value type is contained</exception>
    /// <exception cref="InvalidEnumArgumentException">Unknown literal type</exception>
    public string TokenString() => this.Type switch
    {
        LiteralType.NIL     => "null",
        LiteralType.BOOLEAN => "null",
        LiteralType.STRING  => this.stringValue!,
        LiteralType.NUMBER  => this.numberValue.ToString("0.0###############", CultureInfo.InvariantCulture),
        LiteralType.OBJECT  => throw new InvalidOperationException("Object value does not have a token value"),
        LiteralType.INVALID => "null",
        _                   => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LiteralType))
    };

    /// <summary>
    /// Returns the AST string representation of the given literal value
    /// </summary>
    /// <returns>The string representation of the literal value</returns>
    /// <exception cref="InvalidOperationException">If an invalid literal value type is contained</exception>
    /// <exception cref="InvalidEnumArgumentException">Unknown literal type</exception>
    public string ASTString() => this.Type switch
    {
        LiteralType.NIL     => NilString,
        LiteralType.BOOLEAN => this.boolValue ? TrueString : FalseString,
        LiteralType.STRING  => this.stringValue!,
        LiteralType.NUMBER  => this.numberValue.ToString("0.0###############", CultureInfo.InvariantCulture),
        LiteralType.OBJECT  => throw new InvalidOperationException("Object value does not have an AST value"),
        LiteralType.INVALID => throw new InvalidOperationException("Invalid value type is invalid"),
        _                   => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LiteralType))
    };

    /// <summary>
    /// Returns a string representation of whatever literal value is contained within this wrapper
    /// </summary>
    /// <returns>The string representation of the literal value</returns>
    /// <exception cref="InvalidOperationException">If an invalid literal value type is contained</exception>
    /// <exception cref="InvalidEnumArgumentException">Unknown literal type</exception>
    public override string ToString() => this.Type switch
    {
        LiteralType.NIL     => NilString,
        LiteralType.BOOLEAN => this.boolValue ? TrueString : FalseString,
        LiteralType.STRING  => this.stringValue!,
        LiteralType.NUMBER  => this.numberValue.ToString(CultureInfo.InvariantCulture),
        LiteralType.OBJECT  => this.objectValue!.ToString(),
        LiteralType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                   => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LiteralType))
    };

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(in LoxValue other) => this.Type == other.Type && this.Type switch
    {
        LiteralType.NIL     => true,
        LiteralType.BOOLEAN => this.boolValue == other.boolValue,
        LiteralType.STRING  => this.stringValue == other.stringValue,
        LiteralType.NUMBER  => this.numberValue.Equals(other.numberValue),
        LiteralType.OBJECT  => ReferenceEquals(this.objectValue, other.objectValue),
        LiteralType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                   => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LiteralType)),
    };

    /// <inheritdoc />
    bool IEquatable<LoxValue>.Equals(LoxValue other) => Equals(in other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LoxValue other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => this.Type switch
    {
        LiteralType.NIL     => throw new NullReferenceException("Cannot get the hashcode of a nil"),
        LiteralType.BOOLEAN => this.boolValue.GetHashCode(),
        LiteralType.STRING  => this.stringValue!.GetHashCode(),
        LiteralType.NUMBER  => this.numberValue.GetHashCode(),
        LiteralType.OBJECT  => this.objectValue!.GetHashCode(),
        LiteralType.INVALID => throw new InvalidOperationException("None literal type is invalid"),
        _                   => throw new InvalidEnumArgumentException(nameof(this.Type), (int)this.Type, typeof(LiteralType)),
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
    /// Casts the given string to a LoxValue
    /// </summary>
    /// <param name="value">String to cast</param>
    /// <returns>A <see cref="LoxValue"/> representing the given <see cref="string"/></returns>
    public static implicit operator LoxValue(string value) => new(value);

    /// <summary>
    /// Casts the given LoxValue to a string
    /// </summary>
    /// <param name="value">LoxValue to cast</param>
    /// <returns>The casted <see cref="string"/> value</returns>
    public static explicit operator string(in LoxValue value) => value.StringValue;

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
    /// Casts the given LoxObject to a LoxValue
    /// </summary>
    /// <param name="value">LoxObject to cast</param>
    /// <returns>A <see cref="LoxValue"/> representing the given <see cref="LoxObject"/></returns>
    public static implicit operator LoxValue(LoxObject value) => new(value);

    /// <summary>
    /// Casts the given LoxValue to a LoxObject
    /// </summary>
    /// <param name="value">LoxValue to cast</param>
    /// <returns>The casted <see cref="LoxObject"/> value</returns>
    public static explicit operator LoxObject(in LoxValue value) => value.ObjectValue;

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
