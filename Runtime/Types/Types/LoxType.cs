using System.Diagnostics.CodeAnalysis;
using Lox.Runtime.Types.Functions;
using Lox.Scanning;

namespace Lox.Runtime.Types.Types;

/// <summary>
/// Lox type object
/// </summary>
/// <param name="identifier">Object identifier</param>
public abstract class LoxType(in Token identifier, Dictionary<string, FunctionDefinition> methods) : LoxInvokable(identifier)
{
    /// <summary>
    /// Methods defined on this type
    /// </summary>
    private readonly Dictionary<string, FunctionDefinition> methods = methods;

    /// <inheritdoc />
    public override int Arity => 0;

    /// <summary>
    /// Tries to get a method definition on the type
    /// </summary>
    /// <param name="identifier">Method identifier</param>
    /// <param name="method">Method definition, if found</param>
    /// <returns><see langword="true"/> if the method was found, otherwise <see langword="false"/></returns>
    public virtual bool TryGetMethod(in Token identifier, [MaybeNullWhen(false)] out FunctionDefinition method) => this.methods.TryGetValue(identifier.Lexeme, out method);

    /// <inheritdoc />
    public override LoxValue Invoke(LoxInterpreter interpreter, in ReadOnlySpan<LoxValue> arguments) => new LoxInstance(this);

    /// <inheritdoc />
    public override string ToString() => $"[class {this.Identifier.Lexeme}]";
}
