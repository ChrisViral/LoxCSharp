namespace Lox.VM.Compiler;

public partial class LoxCompiler
{
    /// <summary>
    /// Compilation scope utility
    /// </summary>
    private readonly ref struct Scope : IDisposable
    {
        #region Fields
        /// <summary>
        /// Compiler instance
        /// </summary>
        private readonly LoxCompiler compiler;
        #endregion

        #region Constructors
        /// <summary>
        /// Opens a new scope
        /// </summary>
        /// <param name="compiler">Compiler instance</param>
        private Scope(LoxCompiler compiler)
        {
            this.compiler = compiler;
            compiler.OpenScope();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Closes the scope
        /// </summary>
        public void Dispose() => this.compiler.CloseScope();
        #endregion

        #region Static methods
        /// <summary>
        /// Opens a new scope
        /// </summary>
        /// <param name="compiler">Compiler instance</param>
        /// <returns>The disposable scope wrapper</returns>
        public static Scope Open(LoxCompiler compiler) => new(compiler);
        #endregion
    }
}
