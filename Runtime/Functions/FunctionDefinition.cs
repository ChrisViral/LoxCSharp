using CodeCrafters.Interpreter.Interrupts;
using CodeCrafters.Interpreter.Scanning;
using CodeCrafters.Interpreter.Syntax.Statements.Declarations;

namespace CodeCrafters.Interpreter.Runtime.Functions;

/// <summary>
/// Runtime defined function object
/// </summary>
public sealed class FunctionDefinition : LoxFunction
{
    /// <summary>
    /// Function closure
    /// </summary>
    private readonly LoxEnvironment.Scope closure;
    /// <summary>
    /// Associated function declaration
    /// </summary>
    private readonly FunctionDeclaration declaration;

    /// <summary>
    /// Creates a new function from the specified declaration
    /// </summary>
    /// <param name="declaration">Function declaration to define</param>
    /// <param name="closure">Function closure</param>
    public FunctionDefinition(FunctionDeclaration declaration, LoxEnvironment.Scope closure) : base(declaration.Identifier, FunctionKind.FUNCTION)
    {
        this.closure     = closure;
        this.declaration = declaration;
        this.Arity       = this.declaration.Parameters.Count;
    }

    /// <inheritdoc />
    public override LoxValue Invoke(LoxInterpreter interpreter, in ReadOnlySpan<LoxValue> arguments)
    {
        LoxEnvironment functionEnvironment = new(interpreter.CurrentEnvironment, this.closure);
        functionEnvironment.PushScope();
        for (int i = 0; i < this.Arity; i++)
        {
            Token parameter = this.declaration.Parameters[i];
            LoxValue argument = arguments[i];
            functionEnvironment.DefineVariable(parameter, argument);
        }

        try
        {
            interpreter.ExecuteWithEnv(this.declaration.Body.Statements, functionEnvironment);
        }
        catch (ReturnInterrupt returnValue)
        {
            return returnValue.Value;
        }
        finally
        {
            functionEnvironment.PopScope();
        }

        return LoxValue.Nil;
    }
}
