using System.Reflection;
using CodeCrafters.Interpreter.Exceptions;
using CodeCrafters.Interpreter.Scanning;

namespace CodeCrafters.Interpreter.Runtime.Functions.Native;

/// <summary>
/// Native function base class
/// </summary>
public abstract class LoxNativeFunction : LoxFunction
{
    /// <summary>
    /// Creates a new NativeFunction with it's attached compile-time name
    /// </summary>
    protected LoxNativeFunction() : base(default, FunctionKind.NATIVE)
    {
        // Get name definition
        LoxNativeDefinitionAttribute? attribute = GetType().GetCustomAttribute<LoxNativeDefinitionAttribute>();
        if (attribute is null) throw new LoxInvalidNativeDefinitionException($"{GetType().FullName} does not have an attached LoxNativeDefinitionAttribute");
        if (string.IsNullOrEmpty(attribute.Name)) throw new LoxInvalidNativeDefinitionException($"{GetType().FullName} has null or empty name definition");

        // Create identifier pointing to this object
        this.Identifier = new Token(TokenType.IDENTIFIER, attribute.Name, LoxValue.Invalid, -1);
    }
}
