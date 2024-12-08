﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CodeCrafters.Interpreter.Exceptions.Runtime;
using CodeCrafters.Interpreter.Interrupts;
using CodeCrafters.Interpreter.Runtime.Functions;
using CodeCrafters.Interpreter.Runtime.Functions.Native;
using CodeCrafters.Interpreter.Scanning;
using CodeCrafters.Interpreter.Syntax.Expressions;
using CodeCrafters.Interpreter.Syntax.Statements;
using CodeCrafters.Interpreter.Syntax.Statements.Declarations;
using CodeCrafters.Interpreter.Utils;

namespace CodeCrafters.Interpreter.Runtime;

/// <summary>
/// Lox program interpreter
/// </summary>
public sealed class LoxInterpreter : IExpressionVisitor<LoxValue>, IStatementVisitor<object?>
{
    #region Properties
    /// <summary>
    /// Runtime environment
    /// </summary>
    public LoxEnvironment CurrentEnvironment { get; private set; } = new();
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new Lox interpreter
    /// </summary>
    public LoxInterpreter()
    {
        // Load all native functions
        Type nativeType = typeof(LoxNativeFunction);
        foreach (Type type in Assembly.GetExecutingAssembly()
                                      .DefinedTypes
                                      .Where(ti => ti is { IsAbstract: false, IsClass: true, IsGenericType: false }
                                                && ti.IsSubclassOf(nativeType))
                                      .Select(ti => ti.AsType()))
        {
            // Instantiate function instances and define global
            LoxNativeFunction nativeFunction = (LoxNativeFunction)Activator.CreateInstance(type)!;
            this.CurrentEnvironment.DefineGlobalVariable(nativeFunction.Identifier, nativeFunction);
        }
    }
    #endregion


    #region Interpreter
    /// <summary>
    /// Interprets and prints a given Lox program
    /// </summary>
    /// <param name="program">Statement list to interpret</param>
    public async Task InterpretAsync(IReadOnlyCollection<LoxStatement> program) => await Task.Run(() => Interpret(program));

    /// <summary>
    /// Interprets and prints a given Lox program
    /// </summary>
    /// <param name="program">Statement list to interpret</param>
    public void Interpret(IEnumerable<LoxStatement> program)
    {
        try
        {
            foreach (LoxStatement statement in program)
            {
                Execute(statement);
            }
        }
        catch (LoxRuntimeException e)
        {
            LoxErrorUtils.ReportRuntimeException(e);
        }
    }

    /// <summary>
    /// Interprets and prints a given Lox expression
    /// </summary>
    /// <param name="expression">Expression to interpret</param>
    public async Task InterpretAsync(LoxExpression expression) => await Task.Run(() => Interpret(expression));

    /// <summary>
    /// Interprets and prints a given Lox expression
    /// </summary>
    /// <param name="expression">Expression to interpret</param>
    public void Interpret(LoxExpression expression)
    {
        try
        {
            LoxValue result = Evaluate(expression);
            Console.Out.WriteLineAsync(result.ToString());
        }
        catch (LoxRuntimeException e)
        {
            LoxErrorUtils.ReportRuntimeException(e);
        }
    }

    /// <summary>
    /// Executes a Lox statement
    /// </summary>
    /// <param name="statement">Statement to execute</param>
    public void Execute(LoxStatement statement) => statement.Accept(this);

    /// <summary>
    /// Executes the specified statements in the provided environment
    /// </summary>
    /// <param name="statements">Statements to execute</param>
    /// <param name="environment">Environment to execute in</param>
    public void ExecuteWithEnv(IEnumerable<LoxStatement> statements, LoxEnvironment environment)
    {
        // Save previous env and restore after execution
        LoxEnvironment previousEnv = this.CurrentEnvironment;
        try
        {
            this.CurrentEnvironment = environment;
            foreach (LoxStatement statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            this.CurrentEnvironment = previousEnv;
        }
    }

    /// <summary>
    /// Evaluates a given Lox expression
    /// </summary>
    /// <param name="loxExpression">Expression to evaluate</param>
    /// <returns>The result of the expression</returns>
    public LoxValue Evaluate(LoxExpression loxExpression) => loxExpression.Accept(this);
    #endregion

    #region Statement visitor
    /// <inheritdoc />
    public object? VisitPrintStatement(PrintStatement statement)
    {
        LoxValue value = Evaluate(statement.Expression);
        Console.WriteLine(value.ToString());
        return null;
    }

    /// <inheritdoc />
    /// <exception cref="ReturnInterrupt">The return value</exception>
    [DoesNotReturn]
    public object VisitReturnStatement(ReturnStatement statement)
    {
        if (statement.Value is null) throw new ReturnInterrupt();

        LoxValue value = Evaluate(statement.Value);
        throw new ReturnInterrupt(value);
    }

    /// <inheritdoc />
    public object? VisitIfStatement(IfStatement statement)
    {
        LoxValue condition = Evaluate(statement.Condition);
        if (IsTruthy(condition))
        {
            Execute(statement.IfBranch);
        }
        else if (statement.ElseBranch is not null)
        {
            Execute(statement.ElseBranch);
        }

        return null;
    }

    /// <inheritdoc />
    public object? VisitWhileStatement(WhileStatement statement)
    {
        while (IsTruthy(Evaluate(statement.Condition)))
        {
            Execute(statement.BodyStatement);
        }
        return null;
    }

    /// <inheritdoc />
    public object? VisitForStatement(ForStatement statement)
    {
        bool pushedScope = false;
        try
        {
            // Run the initializer if needed
            if (statement.Initializer is not null)
            {
                // If we're declaring a variable, push the scope
                if (statement.Initializer is VariableDeclaration)
                {
                    this.CurrentEnvironment.PushScope();
                    pushedScope = true;
                }
                Execute(statement.Initializer);
            }

            // With condition
            if (statement.Condition is not null)
            {
                // With increment
                if (statement.Increment is not null)
                {
                    while (IsTruthy(Evaluate(statement.Condition)))
                    {
                        Execute(statement.BodyStatement);
                        Execute(statement.Increment);
                    }
                }
                // Without increment
                else
                {
                    while (IsTruthy(Evaluate(statement.Condition)))
                    {
                        Execute(statement.BodyStatement);
                    }
                }
            }
            // Without condition
            else
            {
                // With increment
                if (statement.Increment is not null)
                {
                    while (true)
                    {
                        Execute(statement.BodyStatement);
                        Execute(statement.Increment);
                    }
                }

                // Without increment
                while (true)
                {
                    Execute(statement.BodyStatement);
                }
            }
        }
        finally
        {
            // If we pushed a scope, pop it now
            if (pushedScope)
            {
                this.CurrentEnvironment.PopScope();
            }
        }
        return null;
    }

    /// <inheritdoc />
    public object? VisitBlockStatement(BlockStatement block)
    {
        this.CurrentEnvironment.PushScope();
        try
        {
            foreach (LoxStatement statement in block.Statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            this.CurrentEnvironment.PopScope();
        }

        return null;
    }

    /// <inheritdoc />
    public object? VisitExpressionStatement(ExpressionStatement statement)
    {
        Evaluate(statement.Expression);
        return null;
    }

    /// <inheritdoc />
    public object? VisitVariableDeclaration(VariableDeclaration declaration)
    {
        if (declaration.Initializer is not null)
        {
            LoxValue value = Evaluate(declaration.Initializer);
            this.CurrentEnvironment.DefineVariable(declaration.Identifier, value);
        }
        else
        {
            this.CurrentEnvironment.DefineVariable(declaration.Identifier);
        }
        return null;
    }

    /// <inheritdoc />
    public object? VisitFunctionDeclaration(FunctionDeclaration declaration)
    {
        FunctionDefinition function = new(declaration, this.CurrentEnvironment.MakeClosure());
        this.CurrentEnvironment.DefineVariable(declaration.Identifier, function);
        return null;
    }
    #endregion

    #region Expression visitor
    /// <inheritdoc />
    public LoxValue VisitLiteralExpression(LiteralExpression expression) => expression.Value;

    /// <inheritdoc />
    public LoxValue VisitVariableExpression(VariableExpression expression) => this.CurrentEnvironment.GetVariable(expression.Identifier);

    /// <inheritdoc />
    public LoxValue VisitGroupingExpression(GroupingExpression expression) => Evaluate(expression.InnerExpression);

    /// <inheritdoc />
    /// <exception cref="LoxInvalidOperandException">Invalid operand</exception>
    /// <exception cref="LoxInvalidOperationException">Invalid operator</exception>
    public LoxValue VisitUnaryExpression(UnaryExpression expression)
    {
        LoxValue inner = Evaluate(expression.InnerExpression);

        switch (expression.Operator.Type)
        {
            case TokenType.MINUS:
                double number = ValidateNumber(expression.Operator, inner, "Operand must be a number.");
                return -number;

            case TokenType.BANG:
                return !IsTruthy(inner);

            default:
                throw new LoxInvalidOperationException("Invalid unary operation", expression.Operator);
        }
    }

    /// <inheritdoc />
    /// <exception cref="LoxInvalidOperandException">Invalid operand</exception>
    /// <exception cref="LoxInvalidOperationException">Invalid operator</exception>
    public LoxValue VisitBinaryExpression(BinaryExpression expression)
    {
        LoxValue left  = Evaluate(expression.LeftExpression);
        LoxValue right = Evaluate(expression.RightExpression);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (expression.Operator.Type)
        {
            case TokenType.MINUS:
            {
                double numberLeft  = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft - numberRight;
            }

            case TokenType.SLASH:
            {
                double numberLeft  = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft / numberRight;
            }

            case TokenType.STAR:
            {
                double numberLeft  = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft * numberRight;
            }

            case TokenType.GREATER:
            {
                double numberLeft  = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft > numberRight;
            }

            case TokenType.GREATER_EQUAL:
            {
                double numberLeft  = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft >= numberRight;
            }

            case TokenType.LESS:
            {
                double numberLeft  = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft < numberRight;
            }

            case TokenType.LESS_EQUAL:
            {
                double numberLeft  = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft <= numberRight;
            }

            case TokenType.PLUS when left.TryGetNumber(out double numberLeft)
                                  && right.TryGetNumber(out double numberRight):
                return numberLeft + numberRight;

            case TokenType.PLUS when left.TryGetString(out string? stringLeft)
                                  && right.TryGetString(out string? stringRight):
                return stringLeft + stringRight;

            case TokenType.PLUS:
                throw new LoxInvalidOperandException("Operands must be two numbers or two strings.", expression.Operator);

            case TokenType.EQUAL_EQUAL:
                return left.Equals(right);

            case TokenType.BANG_EQUAL:
                return !left.Equals(right);

            default:
                throw new LoxInvalidOperationException("Invalid binary operation", expression.Operator);
        }
    }

    /// <inheritdoc />
    /// <exception cref="LoxInvalidOperationException">Invalid operator</exception>
    public LoxValue VisitLogicalExpression(LogicalExpression expression)
    {
        LoxValue leftValue  = Evaluate(expression.LeftExpression);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (expression.Operator.Type)
        {
            case TokenType.OR:
                if (IsTruthy(leftValue)) return leftValue;
                break;

            case TokenType.AND:
                if (!IsTruthy(leftValue)) return leftValue;
                break;

            default:
                throw new LoxInvalidOperationException("Invalid logical operation", expression.Operator);
        }

        return Evaluate(expression.RightExpression);
    }

    /// <inheritdoc />
    public LoxValue VisitAssignmentExpression(AssignmentExpression expression)
    {
        LoxValue value = Evaluate(expression.Value);
        this.CurrentEnvironment.SetVariable(expression.Identifier, value);
        return value;
    }

    /// <inheritdoc />
    /// <exception cref="LoxInvalidOperationException">If a non-invokable object is trying to be invoked, or the function arity is mismatched</exception>
    public LoxValue VisitInvokeExpression(InvokeExpression expression)
    {
        // Check that the target is callable
        LoxValue value = Evaluate(expression.Target);
        if (value.Type is not LoxValue.LiteralType.OBJECT) throw new LoxInvalidOperationException("Can only call functions and classes", expression.Terminator);

        // Check that the arity matches
        LoxObject target = value.ObjectValue;
        if (target.Arity != expression.Parameters.Count) throw new LoxInvalidOperationException($"Expected {target.Arity} arguments but got {expression.Parameters.Count}.");

        LoxValue[] parameters;
        if (expression.Parameters.Count is 0)
        {
            parameters = [];
        }
        else
        {
            parameters = new LoxValue[expression.Parameters.Count];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = Evaluate(expression.Parameters[i]);
            }
        }

        return target.Invoke(this, parameters);
    }
    #endregion

    #region Static methods
    /// <summary>
    /// Validates that the given operand is a number
    /// </summary>
    /// <param name="operatorToken">Operator token</param>
    /// <param name="operand">Operand value</param>
    /// <param name="message">Error message</param>
    /// <returns>The operand as a double</returns>
    /// <exception cref="LoxInvalidOperandException">If <paramref name="operand"/> is not of type a <see cref="double"/></exception>
    private static double ValidateNumber(in Token operatorToken, in LoxValue operand, string message)
    {
        return operand.TryGetNumber(out double result) ? result : throw new LoxInvalidOperandException(message, operatorToken);
    }

    /// <summary>
    /// Checks if an object evaluates to true or false
    /// </summary>
    /// <param name="value">Value to evaluate</param>
    /// <returns><see langword="true"/> if the object is truthy, otherwise <see langword="false"/></returns>
    private static bool IsTruthy(in LoxValue value) => value.Type switch
    {
        LoxValue.LiteralType.BOOLEAN => value.BoolValue,
        LoxValue.LiteralType.NIL     => false,
        _                            => true
    };
    #endregion
}