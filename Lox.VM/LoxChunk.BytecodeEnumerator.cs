using System.Collections;

namespace Lox.VM;

public partial class LoxChunk
{
    /// <summary>
    /// Enumerates bytecode and lines simultaneously
    /// </summary>
    /// <param name="chunk">Chunk to enumerate over</param>
    public ref struct BytecodeEnumerator(LoxChunk chunk) : IEnumerator<(byte bytecode, int offset, int line)>
    {
        #region Fields
        private int currentIndex = -1;
        private int lineIndex    = 0;
        private int currentLine  = 0;
        private int lineRepeat   = 0;
        private readonly int version = chunk.version;
        #endregion

        #region Properties
        /// <inheritdoc />
        public (byte bytecode, int offset, int line) Current => (chunk.code[this.currentIndex], this.currentIndex, this.currentLine);

        /// <inheritdoc />
        object? IEnumerator.Current => this.Current;
        #endregion

        #region Methods
        /// <inheritdoc />
        public bool MoveNext()
        {
            if (chunk.version != this.version) throw new InvalidOperationException("Chunk modified during iteration");

            this.currentIndex++;
            if (this.currentIndex == chunk.code.Count) return false;

            this.lineRepeat++;
            if (this.lineRepeat < 0) return true;

            this.currentLine = chunk.lines[this.lineIndex++];
            if (this.currentLine > 0) return true;

            this.lineRepeat  = this.currentLine;
            this.currentLine = chunk.lines[this.lineIndex++];
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (chunk.version != this.version) throw new InvalidOperationException("Chunk modified during iteration");

            this.currentIndex = -1;
            this.lineIndex    = 0;
            this.lineRepeat   = 0;
            this.currentLine  = 0;
        }

        /// <inheritdoc />
        void IDisposable.Dispose() { }
        #endregion
    }
}
