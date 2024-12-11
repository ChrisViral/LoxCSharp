using Lox.Runtime.Functions;
using Lox.Runtime.Types;
using Lox.Scanner;
using Lox.Syntax.Statements;
using Lox.Syntax.Statements.Declarations;
using Lox.Utils;

namespace Lox;

public sealed partial class LoxResolver
{
    #region Statements
    /// <inheritdoc />
    public void VisitExpressionStatement(ExpressionStatement statement) => Resolve(statement.Expression);

    /// <inheritdoc />
    public void VisitPrintStatement(PrintStatement statement) => Resolve(statement.Expression);

    /// <inheritdoc />
    public void VisitReturnStatement(ReturnStatement statement)
    {
        if (this.currentFunctionKind is FunctionKind.NONE)
        {
            LoxErrorUtils.ReportParseError(statement.Keyword, "Can't return from top-level code.");
        }

        if (statement.Value is not null)
        {
            if (this.currentFunctionKind is FunctionKind.CONSTRUCTOR)
            {
                LoxErrorUtils.ReportParseError(statement.Keyword, "Can't return a value from an initializer.");
            }

            Resolve(statement.Value);
        }
    }

    /// <inheritdoc />
    public void VisitIfStatement(IfStatement statement)
    {
        Resolve(statement.Condition);
        Resolve(statement.IfBranch);
        if (statement.ElseBranch is not null)
        {
            Resolve(statement.ElseBranch);
        }
    }

    /// <inheritdoc />
    public void VisitWhileStatement(WhileStatement statement)
    {
        Resolve(statement.Condition);
        Resolve(statement.BodyStatement);
    }

    /// <inheritdoc />
    public void VisitForStatement(ForStatement statement)
    {
        bool hasScope = false;
        if (statement.Initializer is not null)
        {
            if (statement.Initializer is VariableDeclaration)
            {
                hasScope = true;
                OpenScope();
            }

            Resolve(statement.Initializer);
        }

        if (statement.Condition is not null)
        {
            Resolve(statement.Condition);
        }

        if (statement.Increment is not null)
        {
            Resolve(statement.Increment);
        }

        Resolve(statement.BodyStatement);

        if (hasScope)
        {
            CloseScope();
        }
    }

    /// <inheritdoc />
    public void VisitBlockStatement(BlockStatement block)
    {
        OpenScope();
        Resolve(block.Statements);
        CloseScope();
    }

    /// <inheritdoc />
    public void VisitVariableDeclaration(VariableDeclaration declaration)
    {
        DeclareVariable(declaration.Identifier);
        if (declaration.Initializer is not null)
        {
            Resolve(declaration.Initializer);
        }

        DefineVariable(declaration.Identifier);
    }

    /// <inheritdoc />
    public void VisitFunctionDeclaration(FunctionDeclaration declaration)
    {
        DeclareVariable(declaration.Identifier, State.DEFINED);
        ResolveFunction(declaration, FunctionKind.FUNCTION);
    }

    /// <inheritdoc />
    public void VisitMethodDeclaration(MethodDeclaration declaration)
    {
        DeclareVariable(declaration.Identifier, State.DEFINED);
        ResolveFunction(declaration, FunctionKind.METHOD);
    }

    /// <inheritdoc />
    public void VisitClassDeclaration(ClassDeclaration declaration)
    {
        TypeKind enclosingKind = this.currentTypeKind;
        this.currentTypeKind = TypeKind.CLASS;

        DeclareVariable(declaration.Identifier, State.DEFINED);

        if (declaration.Superclass is not null)
        {
            if (declaration.Identifier.Lexeme == declaration.Superclass.Identifier.Lexeme)
            {
                LoxErrorUtils.ReportParseError(declaration.Superclass.Identifier, "A class can't inherit from itself.");
            }

            Resolve(declaration.Superclass);

            OpenScope();
            DeclareVariable(Token.Super, State.DEFINED);
        }

        OpenScope();
        DeclareVariable(Token.This, State.DEFINED);

        foreach (MethodDeclaration method in declaration.Methods)
        {
            FunctionKind kind = method.Identifier.Lexeme == LoxType.CONSTRUCTOR ? FunctionKind.CONSTRUCTOR : FunctionKind.METHOD;
            ResolveFunction(method, kind);
        }

        CloseScope();

        if (declaration.Superclass is not null)
        {
            CloseScope();
        }

        this.currentTypeKind = enclosingKind;
    }
    #endregion
}
