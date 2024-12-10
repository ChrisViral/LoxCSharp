using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Lox.Exceptions.Runtime;
using Lox.Runtime.Types.Functions;
using Lox.Scanning;

namespace Lox.Runtime.Types.Types;

/// <summary>
/// Lox type kind
/// </summary>
public enum TypeKind
{
    NONE,
    CLASS,
}

/// <summary>
/// Lox type object
/// </summary>
public abstract class LoxType : LoxObject, IInvokable
{
    #region Constants
    /// <summary>
    /// Constructor name
    /// </summary>
    public const string CONSTRUCTOR = "init";
    #endregion

    #region Fields
    /// <summary>
    /// Methods defined on this type
    /// </summary>
    protected readonly Dictionary<string, FunctionDefinition> methods;
    #endregion

    #region Constructors
    /// <summary>
    /// Lox type object
    /// </summary>
    /// <param name="identifier">Type identifier</param>
    /// <param name="methods">Type methods</param>
    /// <param name="kind">Type kind</param>
    protected LoxType(in Token identifier, Dictionary<string, FunctionDefinition> methods, TypeKind kind)
    {
        this.methods    = methods;
        this.Kind       = kind;
        this.Identifier = identifier;
        if (this.methods.TryGetValue(CONSTRUCTOR, out FunctionDefinition? constructor))
        {
            this.Arity = constructor.Arity;
        }
    }
    #endregion

    #region Properties
    /// <summary>
    /// Type kind
    /// </summary>
    public TypeKind Kind { get; protected init; }

    /// <inheritdoc />
    public virtual int Arity { get; }

    /// <summary>
    /// Type identifier
    /// </summary>
    public Token Identifier { get; }
    #endregion

    #region Methods
    /// <summary>
    /// Tries to get a method definition on the type
    /// </summary>
    /// <param name="identifier">Method identifier</param>
    /// <param name="method">Method definition, if found</param>
    /// <returns><see langword="true"/> if the method was found, otherwise <see langword="false"/></returns>
    public virtual bool TryGetMethod(in Token identifier, [MaybeNullWhen(false)] out FunctionDefinition method) => this.methods.TryGetValue(identifier.Lexeme, out method);

    /// <inheritdoc />
    public virtual LoxValue Invoke(LoxInterpreter interpreter, in ReadOnlySpan<LoxValue> arguments)
    {
        LoxInstance instance = new(this);
        if (this.methods.TryGetValue(CONSTRUCTOR, out FunctionDefinition? constructor))
        {
            constructor.Bind(instance).Invoke(interpreter, arguments);
        }
        return instance;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string kind = this.Kind switch
        {
            TypeKind.CLASS => "class",
            TypeKind.NONE  => throw new LoxRuntimeException("Invalid type kind"),
            _              => throw new InvalidEnumArgumentException(nameof(this.Kind), (int)this.Kind, typeof(TypeKind))
        };
        return $"[{kind} {this.Identifier.Lexeme}]";
    }
    #endregion
}
