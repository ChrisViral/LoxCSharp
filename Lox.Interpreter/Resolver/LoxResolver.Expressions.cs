﻿using Lox.Interpreter.Runtime.Types;
using Lox.Interpreter.Syntax.Expressions;
using Lox.Interpreter.Utils;

namespace Lox.Interpreter;

public sealed partial class LoxResolver
{
    #region Expressions
    /// <inheritdoc />
    public void VisitLiteralExpression(LiteralExpression expression) { }

    /// <inheritdoc />
    public void VisitThisExpression(ThisExpression expression)
    {
        if (this.currentTypeKind is TypeKind.NONE)
        {
            LoxErrorUtils.ReportParseError(expression.Keyword, "Can't use 'this' outside of a class.");
            return;
        }

        ResolveLocal(expression, expression.Keyword);
    }

    /// <inheritdoc />
    public void VisitSuperExpression(SuperExpression expression)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (this.currentTypeKind)
        {
            case TypeKind.NONE:
                LoxErrorUtils.ReportParseError(expression.Keyword, "Can't use 'super' outside of a class.");
                break;

            case TypeKind.CLASS:
                LoxErrorUtils.ReportParseError(expression.Keyword, "Can't use 'super' in a class with no superclass.");
                break;
        }

        ResolveLocal(expression, expression.Keyword);
    }

    /// <inheritdoc />
    public void VisitVariableExpression(VariableExpression expression)
    {
        if (this.scopes.TryPeek(out Scope? scope)
         && scope.TryGetValue(expression.Identifier.Lexeme, out VariableData data)
         && data.State is State.UNDEFINED)
        {
            LoxErrorUtils.ReportParseError(expression.Identifier, "Can't read local variable in its own initializer.");
        }

        ResolveLocal(expression, expression.Identifier);
    }

    /// <inheritdoc />
    public void VisitGroupingExpression(GroupingExpression expression) => Resolve(expression.InnerExpression);

    /// <inheritdoc />
    public void VisitUnaryExpression(UnaryExpression expression) => Resolve(expression.InnerExpression);

    /// <inheritdoc />
    public void VisitBinaryExpression(BinaryExpression expression)
    {
        Resolve(expression.LeftExpression);
        Resolve(expression.RightExpression);
    }

    /// <inheritdoc />
    public void VisitLogicalExpression(LogicalExpression expression)
    {
        Resolve(expression.LeftExpression);
        Resolve(expression.RightExpression);
    }

    /// <inheritdoc />
    public void VisitAssignmentExpression(AssignmentExpression expression)
    {
        Resolve(expression.Value);
        ResolveLocal(expression, expression.Identifier);
    }

    /// <inheritdoc />
    public void VisitAccessExpression(AccessExpression expression) => Resolve(expression.Target);

    /// <inheritdoc />
    public void VisitSetExpression(SetExpression expression)
    {
        Resolve(expression.Value);
        Resolve(expression.Target);
    }

    /// <inheritdoc />
    public void VisitInvokeExpression(InvokeExpression expression)
    {
        Resolve(expression.Target);
        foreach (LoxExpression argument in expression.Arguments)
        {
            Resolve(argument);
        }
    }
    #endregion
}
