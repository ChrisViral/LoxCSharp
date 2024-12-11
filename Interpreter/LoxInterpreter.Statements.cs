using System.Diagnostics.CodeAnalysis;
using Lox.Exceptions.Runtime;
using Lox.Interrupts;
using Lox.Runtime;
using Lox.Runtime.Functions;
using Lox.Runtime.Types;
using Lox.Scanner;
using Lox.Syntax.Statements;
using Lox.Syntax.Statements.Declarations;

namespace Lox;

public sealed partial class LoxInterpreter
{
    #region Statement visitor
    /// <inheritdoc />
    public void VisitPrintStatement(PrintStatement statement)
    {
        LoxValue value = Evaluate(statement.Expression);
        Console.WriteLine(value.ToString());
    }

    /// <inheritdoc />
    /// <exception cref="ReturnInterrupt">The return value</exception>
    [DoesNotReturn]
    public void VisitReturnStatement(ReturnStatement statement)
    {
        if (statement.Value is null) throw new ReturnInterrupt();

        LoxValue value = Evaluate(statement.Value);
        throw new ReturnInterrupt(value);
    }

    /// <inheritdoc />
    public void VisitIfStatement(IfStatement statement)
    {
        LoxValue condition = Evaluate(statement.Condition);
        if (condition.IsTruthy)
        {
            Execute(statement.IfBranch);
        }
        else if (statement.ElseBranch is not null)
        {
            Execute(statement.ElseBranch);
        }
    }

    /// <inheritdoc />
    public void VisitWhileStatement(WhileStatement statement)
    {
        while (Evaluate(statement.Condition).IsTruthy)
        {
            Execute(statement.BodyStatement);
        }
    }

    /// <inheritdoc />
    public void VisitForStatement(ForStatement statement)
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
                    while (Evaluate(statement.Condition).IsTruthy)
                    {
                        Execute(statement.BodyStatement);
                        Execute(statement.Increment);
                    }
                }
                // Without increment
                else
                {
                    while (Evaluate(statement.Condition).IsTruthy)
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
    }

    /// <inheritdoc />
    public void VisitBlockStatement(BlockStatement block)
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
    }

    /// <inheritdoc />
    public void VisitExpressionStatement(ExpressionStatement statement) => Evaluate(statement.Expression);

    /// <inheritdoc />
    public void VisitVariableDeclaration(VariableDeclaration declaration)
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
    }

    /// <inheritdoc />
    public void VisitFunctionDeclaration(FunctionDeclaration declaration)
    {
        this.CurrentEnvironment.DefineVariable(declaration.Identifier);
        FunctionDefinition functionDefinition = new(declaration, this.CurrentEnvironment.Capture(), FunctionKind.FUNCTION);
        this.CurrentEnvironment.SetVariable(declaration.Identifier, functionDefinition);
    }

    /// <inheritdoc />
    public void VisitMethodDeclaration(MethodDeclaration declaration) => VisitFunctionDeclaration(declaration);

    /// <inheritdoc />
    public void VisitClassDeclaration(ClassDeclaration declaration)
    {
        LoxType? superclass = null;
        if (declaration.Superclass is not null)
        {
            LoxValue superclassValue = Evaluate(declaration.Superclass);
            if (!superclassValue.TryGetObject<LoxType>(out superclass))
            {
                throw new LoxRuntimeException("Superclass must be a class.", declaration.Superclass.Identifier);
            }
        }

        this.CurrentEnvironment.DefineVariable(declaration.Identifier);

        if (superclass is not null)
        {
            this.CurrentEnvironment.PushScope();
            this.CurrentEnvironment.DefineVariable(Token.Super, superclass);
        }

        Dictionary<string, FunctionDefinition> methods = new(declaration.Methods.Count, StringComparer.Ordinal);
        foreach (MethodDeclaration methodDeclaration in declaration.Methods)
        {
            FunctionKind kind = methodDeclaration.Identifier.Lexeme is LoxType.CONSTRUCTOR ? FunctionKind.CONSTRUCTOR : FunctionKind.METHOD;
            methods[methodDeclaration.Identifier.Lexeme] = new FunctionDefinition(methodDeclaration, this.CurrentEnvironment.Capture(), kind);
        }

        TypeDefinition typeDefinition = new(declaration.Identifier, superclass, methods);

        if (superclass is not null)
        {
            this.CurrentEnvironment.PopScope();
        }

        this.CurrentEnvironment.SetVariable(declaration.Identifier, typeDefinition);
    }
    #endregion
}
