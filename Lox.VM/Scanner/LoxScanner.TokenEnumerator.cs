using System.Collections;
using System.Runtime.CompilerServices;

namespace Lox.VM.Scanner;

public partial class LoxScanner
{
    /// <summary>
    /// Scanner token enumerator
    /// </summary>
    /// <param name="scanner">Scanner to enumerate the tokens for</param>
    public struct TokenEnumerator(LoxScanner scanner) : IEnumerator<Token>
    {
        #region Properties
        private Token current;
        /// <summary>
        /// Current scanner token
        /// </summary>
        public Token Current => this.current;

        /// <inheritdoc />
        object IEnumerator.Current => this.current;
        #endregion

        #region Methods
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => scanner.ScanNextToken(out this.current);

        /// <inheritdoc />
        public void Dispose() => scanner.FreeSource();

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">This Enumerator does not support restting</exception>
        void IEnumerator.Reset() => throw new NotSupportedException("Resetting the scanner is not supported");
        #endregion
    }
}
