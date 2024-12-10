using Lox.Exceptions.Runtime;
using Lox.Runtime.Types.Functions;
using Lox.Scanning;

namespace Lox.Runtime.Types.Types;

/// <summary>
/// Lox object instance
/// </summary>
/// <param name="type">Object type definition</param>
public sealed class LoxInstance(LoxType type) : LoxObject
{
    #region Fields
    /// <summary>
    /// Properties dictionary
    /// </summary>
    private readonly Dictionary<string, LoxValue> fields = new(StringComparer.Ordinal);
    #endregion

    #region Properties
    /// <summary>
    /// Type of this Lox instance
    /// </summary>
    public LoxType Type { get; } = type;
    #endregion

    #region Methods
    /// <summary>
    /// Gets the property of the given identifier on this instance
    /// </summary>
    /// <param name="identifier">Property identifier</param>
    /// <returns>The value of the property</returns>
    /// <exception cref="LoxRuntimeException">If the property was not found</exception>
    public LoxValue GetProperty(in Token identifier)
    {
        if (this.fields.TryGetValue(identifier.Lexeme, out LoxValue value)) return value;

        if (this.Type.TryGetMethod(identifier, out FunctionDefinition? method))
        {
            return method.Bind(this);
        }

        throw new LoxRuntimeException($"Undefined property '{identifier.Lexeme}'.");
    }

    /// <summary>
    /// Sets the property of the given identifier on this instance
    /// </summary>
    /// <param name="identifier">Property identifier</param>
    /// <param name="value">Value to set</param>
    public void SetProperty(in Token identifier, in LoxValue value) => this.fields[identifier.Lexeme] = value;

    /// <inheritdoc />
    public override string ToString() => $"[instance {this.Type.Identifier.Lexeme}]";
    #endregion
}
