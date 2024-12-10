using Lox.Exceptions.Runtime;
using Lox.Runtime.Types;
using Lox.Runtime.Types.Types;
using Lox.Scanning;
using Lox.Syntax.Expressions;

namespace Lox.Runtime;

public sealed partial class LoxInterpreter
{
    #region Expression visitor
    /// <inheritdoc />
    public LoxValue VisitLiteralExpression(LiteralExpression expression) => expression.Value;

    /// <inheritdoc />
    public LoxValue VisitVariableExpression(VariableExpression expression) => ResolveVariable(expression.Identifier, expression);

    /// <inheritdoc />
    public LoxValue VisitGroupingExpression(GroupingExpression expression) => Evaluate(expression.InnerExpression);

    /// <inheritdoc />
    /// <exception cref="LoxInvalidOperandException">Invalid operand</exception>
    /// <exception cref="LoxInvalidOperationException">Invalid operator</exception>
    public LoxValue VisitUnaryExpression(UnaryExpression expression)
    {
        LoxValue inner = Evaluate(expression.InnerExpression);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (expression.Operator.Type)
        {
            case TokenType.MINUS:
                double number = ValidateNumber(expression.Operator, inner, "Operand must be a number.");
                return -number;

            case TokenType.BANG:
                return !inner.IsTruthy;

            default:
                throw new LoxInvalidOperationException("Invalid unary operation", expression.Operator);
        }
    }

    /// <inheritdoc />
    /// <exception cref="LoxInvalidOperandException">Invalid operand</exception>
    /// <exception cref="LoxInvalidOperationException">Invalid operator</exception>
    public LoxValue VisitBinaryExpression(BinaryExpression expression)
    {
        LoxValue left = Evaluate(expression.LeftExpression);
        LoxValue right = Evaluate(expression.RightExpression);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (expression.Operator.Type)
        {
            case TokenType.MINUS:
            {
                double numberLeft = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft - numberRight;
            }

            case TokenType.SLASH:
            {
                double numberLeft = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft / numberRight;
            }

            case TokenType.STAR:
            {
                double numberLeft = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft * numberRight;
            }

            case TokenType.GREATER:
            {
                double numberLeft = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft > numberRight;
            }

            case TokenType.GREATER_EQUAL:
            {
                double numberLeft = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft >= numberRight;
            }

            case TokenType.LESS:
            {
                double numberLeft = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
                double numberRight = ValidateNumber(expression.Operator, right, "Operands must be numbers.");
                return numberLeft < numberRight;
            }

            case TokenType.LESS_EQUAL:
            {
                double numberLeft = ValidateNumber(expression.Operator, left, "Operands must be numbers.");
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
        LoxValue leftValue = Evaluate(expression.LeftExpression);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (expression.Operator.Type)
        {
            case TokenType.OR:
                if (leftValue.IsTruthy) return leftValue;

                break;

            case TokenType.AND:
                if (!leftValue.IsTruthy) return leftValue;

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
        if (this.locals.TryGetValue(expression, out Index depth))
        {
            this.CurrentEnvironment.SetVariableAt(expression.Identifier, value, depth);
        }
        else
        {
            LoxEnvironment.SetGlobalVariable(expression.Identifier, value);
        }

        return value;
    }

    /// <inheritdoc />
    public LoxValue VisitAccessExpression(AccessExpression expression)
    {
        LoxValue target = Evaluate(expression.Target);
        if (!target.TryGetObject(out LoxInstance? instance)) throw new LoxRuntimeException("Only instances have properties.", expression.Identifier);

        return instance!.GetProperty(expression.Identifier);
    }

    /// <inheritdoc />
    public LoxValue VisitSetExpression(SetExpression expression)
    {
        LoxValue target = Evaluate(expression.Target);
        if (!target.TryGetObject(out LoxInstance? instance)) throw new LoxRuntimeException("Only instances have properties.", expression.Identifier);

        LoxValue value = Evaluate(expression.Value);
        instance!.SetProperty(expression.Identifier, value);
        return value;
    }

    /// <inheritdoc />
    /// <exception cref="LoxInvalidOperationException">If a non-invokable object is trying to be invoked, or the function arity is mismatched</exception>
    public LoxValue VisitInvokeExpression(InvokeExpression expression)
    {
        // Check that the target is callable
        LoxValue value = Evaluate(expression.Target);
        if (!value.TryGetObject(out IInvokable? target)) throw new LoxInvalidOperationException("Can only call functions and classes", expression.Terminator);

        // Check that the arity matches
        if (target!.Arity != expression.Arguments.Count) throw new LoxInvalidOperationException($"Expected {target.Arity} arguments but got {expression.Arguments.Count}.");

        LoxValue[] parameters;
        if (expression.Arguments.Count is 0)
        {
            parameters = [];
        }
        else
        {
            parameters = new LoxValue[expression.Arguments.Count];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = Evaluate(expression.Arguments[i]);
            }
        }

        return target.Invoke(this, parameters);
    }
    #endregion
}
