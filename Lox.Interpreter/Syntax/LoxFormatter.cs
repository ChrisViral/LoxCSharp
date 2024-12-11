using System.Text;
using Lox.Interpreter.Syntax.Expressions;
using Lox.Interpreter.Syntax.Statements;
using Lox.Interpreter.Syntax.Statements.Declarations;

namespace Lox.Interpreter.Syntax;

/// <summary>
/// Lox code formatter
/// </summary>
public class LoxFormatter : IExpressionVisitor, IStatementVisitor
{
    /// <summary>
    /// Indent scope struct
    /// </summary>
    private readonly ref struct IndentScope
    {
        /// <summary>
        /// Original depth value
        /// </summary>
        private readonly int originalDepth;
        /// <summary>
        /// Depth reference
        /// </summary>
        private readonly ref int depth;

        /// <summary>
        /// Creates a new indent scope
        /// </summary>
        /// <param name="depth">Depth reference</param>
        private IndentScope(ref int depth)
        {
            this.depth         = ref depth;
            this.originalDepth = depth;
        }

        /// <summary>
        /// Opens a new indent scope
        /// </summary>
        /// <param name="depth">Depth reference</param>
        /// <returns>The opened indent scope</returns>
        public static IndentScope Open(ref int depth)
        {
            IndentScope scope = new(ref depth);
            depth++;
            return scope;
        }

        /// <summary>
        /// Opens a new indent scope conditionally
        /// </summary>
        /// <param name="depth">Depth reference</param>
        /// <param name="shouldOpen">If the scope should be opened or not</param>
        /// <returns>The opened indent scope</returns>
        public static IndentScope Open(ref int depth, in bool shouldOpen)
        {
            IndentScope scope = new(ref depth);
            if (shouldOpen)
            {
                depth++;
            }
            return scope;
        }

        /// <summary>
        /// Closes the indent scope
        /// </summary>
        public void Dispose() => this.depth = this.originalDepth;
    }

    #region Fields
    /// <summary>
    /// Code StringBuilder
    /// </summary>
    private readonly StringBuilder codeBuilder = new();
    /// <summary>
    /// Code indent
    /// </summary>
    private readonly char indent;
    /// <summary>
    /// Indent char size
    /// </summary>
    private readonly int indentSize;
    /// <summary>
    /// Current code indent
    /// </summary>
    private int currentDepth;
    #endregion

    #region Properties
    /// <summary>
    /// Current indent size
    /// </summary>
    private int CurrentIndentSize => this.indentSize * this.currentDepth;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new Lox formatter with the specified whitespace indent
    /// </summary>
    /// <param name="indent">Code indent character</param>
    /// <param name="indentSize">Code indent size</param>
    /// <exception cref="ArgumentException">If <paramref name="indent"/> is not a whitespace character</exception>
    public LoxFormatter(char indent = ' ', byte indentSize = 4)
    {
        if (!char.IsWhiteSpace(indent)) throw new ArgumentException("Indent must be whitespace", nameof(indent));

        this.indent     = indent;
        this.indentSize = indentSize;
    }
    #endregion

    #region Methods
    /// <summary>
    /// Formats a given Lox program
    /// </summary>
    /// <param name="program">Program to format</param>
    /// <returns>The formatted code</returns>
    public string Format(IReadOnlyCollection<LoxStatement> program)
    {
        foreach (LoxStatement statement in program)
        {
            FormatStatement(statement);
        }

        string result = this.codeBuilder.ToString();
        this.codeBuilder.Clear();
        return result;
    }

    /// <summary>
    /// Formats a given Lox statement
    /// </summary>
    /// <param name="statement">Statement to format</param>
    /// <param name="withIndent">If the statement should be indented</param>
    /// <param name="withNewline">If the statement should have a newline added at the end</param>
    private void FormatStatement(LoxStatement statement, bool withIndent = true, bool withNewline = true)
    {
        if (withIndent) AppendIndent();

        statement.Accept(this);

        if (withNewline) AppendNewline();
    }

    /// <summary>
    /// Formats a given Lox expression
    /// </summary>
    /// <param name="expression">Expression to format</param>
    private void FormatExpression(LoxExpression expression) => expression.Accept(this);

    /// <summary>
    /// Formats a function declaration
    /// </summary>
    /// <param name="declaration">Function to format</param>
    private void FormatFunction(FunctionDeclaration declaration)
    {
        this.codeBuilder.Append(declaration.Identifier.Lexeme).Append('(');
        switch (declaration.Parameters.Count)
        {
            case 0:
                break;

            case 1:
                this.codeBuilder.Append(declaration.Parameters[0].Lexeme);
                break;

            default:
                this.codeBuilder.Append(declaration.Parameters[0].Lexeme);
                for (int i = 1; i < declaration.Parameters.Count; i++)
                {
                    this.codeBuilder.Append(", ").Append(declaration.Parameters[i].Lexeme);
                }
                break;
        }

        this.codeBuilder.Append(')');
        AppendNewline();
        FormatStatement(declaration.Body);
    }

    /// <summary>
    /// Appends the current indent
    /// </summary>
    private void AppendIndent() => this.codeBuilder.Append(this.indent, this.CurrentIndentSize);

    /// <summary>
    /// Appends a semicolon
    /// </summary>
    private void AppendSemicolon() => this.codeBuilder.Append(';');

    /// <summary>
    /// Appends a space character
    /// </summary>
    private void AppendSpace() => this.codeBuilder.Append(' ');

    /// <summary>
    /// Appends a newline
    /// </summary>
    private void AppendNewline() => this.codeBuilder.AppendLine();
    #endregion

    #region Statements
    /// <inheritdoc />
    public void VisitExpressionStatement(ExpressionStatement statement)
    {
        FormatExpression(statement.Expression);
        AppendSemicolon();
    }

    /// <inheritdoc />
    public void VisitPrintStatement(PrintStatement statement)
    {
        this.codeBuilder.Append("print ");
        FormatExpression(statement.Expression);
        AppendSemicolon();
    }

    /// <inheritdoc />
    public void VisitReturnStatement(ReturnStatement statement)
    {
        this.codeBuilder.Append("return");
        if (statement.Value is not null)
        {
            AppendSpace();
            FormatExpression(statement.Value);
        }
        AppendSemicolon();
    }

    /// <inheritdoc />
    public void VisitIfStatement(IfStatement statement)
    {
        this.codeBuilder.Append("if (");
        FormatExpression(statement.Condition);
        this.codeBuilder.Append(')').AppendLine();

        using (IndentScope.Open(ref this.currentDepth, statement.IfBranch is not BlockStatement))
        {
            FormatStatement(statement.IfBranch);
        }

        if (statement.ElseBranch is null) return;

        this.codeBuilder.Append("else");
        if (statement.ElseBranch is IfStatement)
        {
            AppendSpace();
            FormatStatement(statement.ElseBranch, withIndent: false);
        }
        else
        {
            AppendNewline();
            using (IndentScope.Open(ref this.currentDepth, statement.ElseBranch is not BlockStatement))
            {
                FormatStatement(statement.ElseBranch);
            }
        }
    }

    /// <inheritdoc />
    public void VisitWhileStatement(WhileStatement statement)
    {
        this.codeBuilder.Append("while (");
        FormatExpression(statement.Condition);
        this.codeBuilder.Append(')').AppendLine();
        using (IndentScope.Open(ref this.currentDepth, statement.BodyStatement is not BlockStatement))
        {
            FormatStatement(statement.BodyStatement);
        }
    }

    /// <inheritdoc />
    public void VisitForStatement(ForStatement statement)
    {
        this.codeBuilder.Append("for (");
        if (statement.Initializer is not null)
        {
            FormatStatement(statement.Initializer, withIndent: false, withNewline: false);
        }
        else
        {
            AppendSemicolon();
        }

        AppendSpace();
        if (statement.Condition is not null)
        {
            FormatExpression(statement.Condition);
        }

        AppendSemicolon();
        AppendSpace();

        if (statement.Increment is not null)
        {
            FormatExpression(statement.Increment.Expression);
        }

        this.codeBuilder.Append('(');
        AppendNewline();

        using (IndentScope.Open(ref this.currentDepth, statement.BodyStatement is not BlockStatement))
        {
            FormatStatement(statement.BodyStatement);
        }
    }

    /// <inheritdoc />
    public void VisitBlockStatement(BlockStatement block)
    {
        this.codeBuilder.Append('{').AppendLine();
        using (IndentScope.Open(ref this.currentDepth))
        {
            foreach (LoxStatement statement in block.Statements)
            {
                FormatStatement(statement);
            }
        }
        this.codeBuilder.Append('}').AppendLine();
    }

    /// <inheritdoc />
    public void VisitVariableDeclaration(VariableDeclaration declaration)
    {
        this.codeBuilder.Append("var ").Append(declaration.Identifier.Lexeme);
        if (declaration.Initializer is not null)
        {
            this.codeBuilder.Append(" = ");
            FormatExpression(declaration.Initializer);
        }
        AppendSemicolon();
    }

    /// <inheritdoc />
    public void VisitFunctionDeclaration(FunctionDeclaration declaration)
    {
        this.codeBuilder.Append("fun ");
        FormatFunction(declaration);
    }

    /// <inheritdoc />
    public void VisitMethodDeclaration(MethodDeclaration declaration) => FormatFunction(declaration);

    /// <inheritdoc />
    public void VisitClassDeclaration(ClassDeclaration declaration)
    {
        this.codeBuilder.Append("class ").AppendLine(declaration.Identifier.Lexeme);
        this.codeBuilder.Append('{').AppendLine();
        using (IndentScope.Open(ref this.currentDepth))
        {
            foreach (MethodDeclaration method in declaration.Methods)
            {
                FormatFunction(method);
            }
        }
        this.codeBuilder.Append('}').AppendLine();
    }
    #endregion

    #region Expressions
    /// <inheritdoc />
    public void VisitLiteralExpression(LiteralExpression expression)
    {
        this.codeBuilder.Append(expression.Value.ToString());
    }

    /// <inheritdoc />
    public void VisitThisExpression(ThisExpression expression) => this.codeBuilder.Append("this");

    /// <inheritdoc />
    public void VisitSuperExpression(SuperExpression expression)
    {
        this.codeBuilder.Append("super.").Append(expression.MethodIdentifier.Lexeme);
    }

    /// <inheritdoc />
    public void VisitVariableExpression(VariableExpression expression)
    {
        this.codeBuilder.Append(expression.Identifier.Lexeme);
    }

    /// <inheritdoc />
    public void VisitGroupingExpression(GroupingExpression expression)
    {
        this.codeBuilder.Append('(');
        FormatExpression(expression.InnerExpression);
        this.codeBuilder.Append(')');
    }

    /// <inheritdoc />
    public void VisitUnaryExpression(UnaryExpression expression)
    {
        this.codeBuilder.Append(expression.Operator.Lexeme);
        FormatExpression(expression.InnerExpression);
    }

    /// <inheritdoc />
    public void VisitBinaryExpression(BinaryExpression expression)
    {
        FormatExpression(expression.LeftExpression);
        this.codeBuilder.Append(' ').Append(expression.Operator.Lexeme).Append(' ');
        FormatExpression(expression.RightExpression);
    }

    /// <inheritdoc />
    public void VisitLogicalExpression(LogicalExpression expression) => VisitBinaryExpression(expression);

    /// <inheritdoc />
    public void VisitAssignmentExpression(AssignmentExpression expression)
    {
        this.codeBuilder.Append(expression.Identifier.Lexeme);
        this.codeBuilder.Append(" = ");
        FormatExpression(expression.Value);
    }

    /// <inheritdoc />
    public void VisitAccessExpression(AccessExpression expression)
    {
        FormatExpression(expression.Target);
        this.codeBuilder.Append('.').Append(expression.Identifier.Lexeme);
    }

    /// <inheritdoc />
    public void VisitSetExpression(SetExpression expression)
    {
        FormatExpression(expression.Target);
        this.codeBuilder.Append('.').Append(expression.Identifier.Lexeme).Append(" = ");
        FormatExpression(expression.Value);
    }

    /// <inheritdoc />
    public void VisitInvokeExpression(InvokeExpression expression)
    {
        FormatExpression(expression.Target);
        this.codeBuilder.Append('(');
        switch (expression.Arguments.Count)
        {
            case 0:
                break;

            case 1:
                FormatExpression(expression.Arguments[0]);
                break;

            default:
                FormatExpression(expression.Arguments[0]);
                for (int i = 1; i < expression.Arguments.Count; i++)
                {
                    this.codeBuilder.Append(", ");
                    FormatExpression(expression.Arguments[i]);
                }
                break;
        }
        this.codeBuilder.Append(')');
    }
    #endregion
}
