using Lox.Interrupts;
using Lox.Scanning;
using Lox.Syntax.Statements.Declarations;

namespace Lox.Runtime.Types.Functions;

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
    /// Creates a new function from the specified declaration
    /// </summary>
    /// <param name="declaration">Function declaration to define</param>
    /// <param name="closure">Function closure</param>
    public FunctionDefinition(FunctionDeclaration declaration, LoxEnvironment closure) : base(declaration.Identifier, FunctionKind.FUNCTION)
    {
        this.closure     = closure;
        this.declaration = declaration;
        this.Arity       = this.declaration.Parameters.Count;
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
            return returnValue.Value;
        }
        finally
        {
            environment.PopScope();
        }

        return LoxValue.Nil;
    }
}
