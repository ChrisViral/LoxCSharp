using System.Runtime.CompilerServices;
using Lox.Common;
using Lox.VM.Bytecode;

namespace Lox.VM.Compiler;

public partial class LoxCompiler
{
    /// <summary>
    /// Parse function delegate
    /// </summary>
    /// <param name="compiler">Compiler instance</param>
    private delegate void ParseFunc(LoxCompiler compiler);

    /// <summary>
    /// Parse rule struct
    /// </summary>
    /// <param name="prefix">Prefix parse function</param>
    /// <param name="infix">Infix parse function</param>
    /// <param name="precedence">Operation precedence</param>
    private readonly struct ParseRule(ParseFunc? prefix, ParseFunc? infix, Precedence precedence)
    {
        #region Fields
        /// <summary>
        /// Prefix parse function
        /// </summary>
        public readonly ParseFunc? prefix     = prefix;
        /// <summary>
        /// Infix parse function
        /// </summary>
        public readonly ParseFunc? infix      = infix;
        /// <summary>
        /// Operation precedence
        /// </summary>
        public readonly Precedence precedence = precedence;
        #endregion
    }

    /// <summary>
    /// Parse rules table
    /// </summary>
    private static class Table
    {
        #region Fields
        /// <summary>
        /// Rules table
        /// </summary>
        private static readonly ParseRule[] Rules = new ParseRule[byte.MaxValue + 1];
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the rules table
        /// </summary>
        static Table()
        {
            // End of File
            Rules[(int)TokenType.EOF]           = new ParseRule(null,     null,   Precedence.NONE);
            // Groupings
            Rules[(int)TokenType.LEFT_PAREN]    = new ParseRule(Grouping, null,   Precedence.NONE);
            Rules[(int)TokenType.RIGHT_PAREN]   = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.LEFT_BRACE]    = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.RIGHT_BRACE]   = new ParseRule(null,     null,   Precedence.NONE);
            // Delimiters
            Rules[(int)TokenType.COMMA]         = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.DOT]           = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.SEMICOLON]     = new ParseRule(null,     null,   Precedence.NONE);
            // Mathematical operation symbols
            Rules[(int)TokenType.PLUS]          = new ParseRule(null,     Binary, Precedence.TERM);
            Rules[(int)TokenType.MINUS]         = new ParseRule(Unary,    Binary, Precedence.TERM);
            Rules[(int)TokenType.STAR]          = new ParseRule(null,     Binary, Precedence.FACTOR);
            Rules[(int)TokenType.SLASH]         = new ParseRule(null,     Binary, Precedence.FACTOR);
            // Equality
            Rules[(int)TokenType.BANG]          = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.BANG_EQUAL]    = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.EQUAL]         = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.EQUAL_EQUAL]   = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.GREATER]       = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.GREATER_EQUAL] = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.LESS]          = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.LESS_EQUAL]    = new ParseRule(null,     null,   Precedence.NONE);
            // Literals
            Rules[(int)TokenType.NIL]           = new ParseRule(Nil,      null,   Precedence.NONE);
            Rules[(int)TokenType.TRUE]          = new ParseRule(True,     null,   Precedence.NONE);
            Rules[(int)TokenType.FALSE]         = new ParseRule(False,    null,   Precedence.NONE);
            Rules[(int)TokenType.NUMBER]        = new ParseRule(Number,   null,   Precedence.NONE);
            Rules[(int)TokenType.STRING]        = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.IDENTIFIER]    = new ParseRule(null,     null,   Precedence.NONE);
            // Conditional keywords
            Rules[(int)TokenType.AND]           = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.OR]            = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.ELSE]          = new ParseRule(null,     null,   Precedence.NONE);
            // OOP Keywords
            Rules[(int)TokenType.THIS]          = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.SUPER]         = new ParseRule(null,     null,   Precedence.NONE);
            // Branching Keywords
            Rules[(int)TokenType.IF]            = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.FOR]           = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.WHILE]         = new ParseRule(null,     null,   Precedence.NONE);
            // Functional Keywords
            Rules[(int)TokenType.VAR]           = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.FUN]           = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.RETURN]        = new ParseRule(null,     null,   Precedence.NONE);
            Rules[(int)TokenType.PRINT]         = new ParseRule(null,     null,   Precedence.NONE);
            // Object Keywords
            Rules[(int)TokenType.CLASS]         = new ParseRule(null,     null,   Precedence.NONE);
            // Error token
            Rules[(int)TokenType.ERROR]         = new ParseRule(null,     null,   Precedence.NONE);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the parse rule for the given token type
        /// </summary>
        /// <param name="type">Token type to get the rule for</param>
        /// <returns>The rule for this token type</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref ParseRule GetRule(TokenType type) => ref Rules[(int)type];
        #endregion

        #region Parse functions
        /// <summary>
        /// Grouping parse
        /// </summary>
        /// <param name="compiler">Compiler instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Grouping(LoxCompiler compiler) => compiler.ParseGrouping();

        /// <summary>
        /// Binary operation parse
        /// </summary>
        /// <param name="compiler">Compiler instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Binary(LoxCompiler compiler) => compiler.ParseBinary();

        /// <summary>
        /// Unary operation parse
        /// </summary>
        /// <param name="compiler">Compiler instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Unary(LoxCompiler compiler) => compiler.ParseUnary();

        /// <summary>
        /// Number parse
        /// </summary>
        /// <param name="compiler">Compiler instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Number(LoxCompiler compiler) => compiler.ParseNumber();

        /// <summary>
        /// Nil parse
        /// </summary>
        /// <param name="compiler">Compiler instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Nil(LoxCompiler compiler) => compiler.EmitOpcode(LoxOpcode.NIL);

        /// <summary>
        /// True parse
        /// </summary>
        /// <param name="compiler">Compiler instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void True(LoxCompiler compiler) => compiler.EmitOpcode(LoxOpcode.TRUE);

        /// <summary>
        /// False parse
        /// </summary>
        /// <param name="compiler">Compiler instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void False(LoxCompiler compiler) => compiler.EmitOpcode(LoxOpcode.FALSE);
        #endregion
    }
}
