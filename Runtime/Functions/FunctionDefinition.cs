using Lox.Interrupts;
using Lox.Runtime.Types;
using Lox.Scanner;
using Lox.Syntax.Statements.Declarations;

namespace Lox.Runtime.Functions;

/// <summary>
/// Runtime defined function object
/// </summary>
public sealed class FunctionDefinition : LoxFunction
{
    /// <summary>
    /// Function closure
    /// </summary>
    private readonly LoxEnvironment closure;
    /// <summary>
    /// Associated function declaration
    /// </summary>
    private readonly FunctionDeclaration declaration;

    /// <summary>
    /// If this is a constructor function
    /// </summary>
    public bool IsConstructor => this.Kind is FunctionKind.CONSTRUCTOR;

    /// <summary>
    /// Creates a new function from the specified declaration
    /// </summary>
    /// <param name="declaration">Function declaration to define</param>
    /// <param name="closure">Function closure</param>
    /// <param name="kind">The function kind</param>
    public FunctionDefinition(FunctionDeclaration declaration, LoxEnvironment closure, FunctionKind kind) : base(declaration.Identifier, kind)
    {
        this.closure       = closure;
        this.declaration   = declaration;
        this.Arity         = this.declaration.Parameters.Count;
    }

    /// <inheritdoc />
    public override LoxValue Invoke(LoxInterpreter interpreter, in ReadOnlySpan<LoxValue> arguments)
    {
        LoxEnvironment environment = this.closure.Capture();
        environment.PushScope();
        for (int i = 0; i < this.Arity; i++)
        {
            Token parameter = this.declaration.Parameters[i];
            LoxValue argument = arguments[i];
            environment.DefineVariable(parameter, argument);
        }

        try
        {
            interpreter.Execute(this.declaration.Body.Statements, environment);
        }
        catch (ReturnInterrupt returnValue)
        {
            return this.IsConstructor ? this.closure.GetVariableAt(Token.This, 0) : returnValue.Value;
        }
        finally
        {
            environment.PopScope();
        }

        return this.IsConstructor ? this.closure.GetVariableAt(Token.This, 0) : LoxValue.Nil;
    }

    /// <summary>
    /// Binds this function to a given object instance
    /// </summary>
    /// <param name="instance">Instance to bind to</param>
    /// <returns>The bound function</returns>
    public LoxFunction Bind(LoxInstance instance)
    {
        LoxEnvironment binding = this.closure.Capture();
        binding.DefineVariable(Token.This, instance);
        return new FunctionDefinition(this.declaration, binding, this.Kind);
    }
}
