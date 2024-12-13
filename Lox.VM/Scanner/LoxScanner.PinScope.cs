namespace Lox.VM.Scanner;

public partial class LoxScanner
{
    /// <summary>
    /// Scanner source pin scope
    /// </summary>
    public readonly ref struct PinScope : IDisposable
    {
        #region Fields
        private readonly LoxScanner scanner;
        #endregion

        #region Methods
        /// <summary>
        /// Creates a new source pin scope
        /// </summary>
        /// <param name="scanner">Scanner to pin the source for</param>
        /// <param name="source">Source to pin</param>
        public PinScope(LoxScanner scanner, string source)
        {
            this.scanner = scanner;
            this.scanner.PinSource(source);
        }

        /// <summary>
        /// Unpins the source without disposing the scanner
        /// </summary>
        public void Dispose() => this.scanner.FreeSource();
        #endregion
    }
}
