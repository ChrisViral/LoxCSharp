using JetBrains.Annotations;

namespace CodeCrafters.Interpreter.Runtime.Functions.Native;

/// <summary>
/// Lox native definition
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false), MeansImplicitUse(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
public class LoxNativeDefinitionAttribute : Attribute
{
    /// <summary>
    /// Native name
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Defines a new LoxNative attribute
    /// </summary>
    /// <param name="name">Native name</param>
    public LoxNativeDefinitionAttribute(string name) => this.Name = name;
}
