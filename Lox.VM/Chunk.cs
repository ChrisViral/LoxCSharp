using System.Collections;
using JetBrains.Annotations;

namespace Lox.VM;

[PublicAPI]
public class Chunk : IList<byte>
{
    /// <summary>
    /// Chunk enumerator class
    /// </summary>
    /// <param name="chunk">Chunk to enumerate from</param>
    public struct ChunkEnumerator(Chunk chunk) : IEnumerator<byte>
    {
        private readonly int version = chunk.version;
        private int currentIndex = 0;

        /// <inheritdoc />
        public byte Current { get; private set; }

        /// <inheritdoc />
        object? IEnumerator.Current => this.Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (this.version != chunk.version) throw new InvalidOperationException("Chunk was modified during enumeration.");
            if (this.currentIndex == chunk.Count) return false;

            this.Current = chunk.code[this.currentIndex++];
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (this.version != chunk.version) throw new InvalidOperationException("Chunk was modified during enumeration.");

            this.currentIndex = 0;
            this.Current      = 0;
        }

        /// <inheritdoc />
        void IDisposable.Dispose() { }
    }

    #region Constants
    /// <summary>
    /// Default chunk capacity
    /// </summary>
    private const int DEFAULT_CAPACITY = 8;
    #endregion

    #region Fields
    private int version;
    private int capacity;
    private byte[] code = [];
    private readonly List<LoxValue> values = [];
    #endregion

    #region Properties
    /// <inheritdoc />
    public int Count { get; private set; }

    /// <summary>
    /// Internal array as a span
    /// </summary>
    private Span<byte> Span => new(this.code, 0, this.Count);

    /// <summary>
    /// Internal array as a readonly span
    /// </summary>
    private ReadOnlySpan<byte> ReadOnlySpan => new(this.code, 0, this.Count);

    /// <inheritdoc />
    bool ICollection<byte>.IsReadOnly => false;
    #endregion

    #region Indexers
    /// <inheritdoc />
    public byte this[int index]
    {
        get => index < this.Count ? this.code[index] : throw new ArgumentOutOfRangeException(nameof(index), index, "Out of chunk range");
        set
        {
            if (index >= this.Count) throw new ArgumentOutOfRangeException(nameof(index), index, "Out of chunk range");
            this.code[index] = value;
        }
    }

    /// <inheritdoc cref="IList{T}.this" />
    public byte this[in Index index]
    {
        get => this[index.GetOffset(this.Count)];
        set => this[index.GetOffset(this.Count)] = value;
    }

    /// <summary>
    /// Accesses a range of this chunk
    /// </summary>
    /// <param name="range">Range to access</param>
    public Span<byte> this[in Range range]
    {
        get
        {
            (int start, int length) = range.GetOffsetAndLength(this.Count);
            return new Span<byte>(this.code, start, length);
        }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Gets a ref value to a member of the chunk
    /// </summary>
    /// <param name="index">Index to get the value from</param>
    /// <returns>The ref value at the given index</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the provided index is out of range</exception>
    public ref byte GetRef(in int index)
    {
        if (index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException(nameof(index), index, "Out of chunk range");
        return ref this.code[index];
    }

    /// <summary>
    /// Adds a constant to the chunk
    /// </summary>
    /// <param name="value">Constant to add</param>
    /// <returns>The index at which the constant was added</returns>
    public int AddConstant(in LoxValue value)
    {
        int index = this.values.Count;
        this.values.Add(value);
        return index;
    }

    /// <summary>
    /// Gets a constant's value from the chunk
    /// </summary>
    /// <param name="index">Constant index to get</param>
    /// <returns>The constants value</returns>
    public LoxValue GetConstant(in int index) => this.values[index];

    /// <inheritdoc />
    public void Add(byte value)
    {
        EnsureAddCapacity();
        this.code[this.Count++] = value;
        this.version++;
    }

    /// <inheritdoc />
    public void Insert(int index, byte item)
    {
        if (index < 0 || index > this.Count) throw new ArgumentOutOfRangeException(nameof(index), index, "Cannot remove outside of chunk");

        if (index == this.Count)
        {
            Add(item);
            return;
        }

        EnsureAddCapacity();
        int to = index + 1;
        int length = this.Count - index;
        Buffer.BlockCopy(this.code, index, this.code, to, length);
        this.code[index] = item;
        this.Count++;
        this.version++;
    }

    /// <summary>
    /// Ensures that the chunk has capacity to add an item
    /// </summary>
    private void EnsureAddCapacity()
    {
        if (this.Count != this.capacity) return;

        if (this.capacity is 0)
        {
            this.capacity = DEFAULT_CAPACITY;
            this.code     = new byte[DEFAULT_CAPACITY];
            return;
        }

        this.capacity *= 2;
        byte[] newCode = new byte[this.capacity];
        Buffer.BlockCopy(this.code, 0, newCode, 0, this.Count);
        this.code = newCode;
    }

    /// <inheritdoc />
    public bool Contains(byte item) => this.ReadOnlySpan.Contains(item);

    /// <inheritdoc />
    public int IndexOf(byte item) => this.ReadOnlySpan.IndexOf(item);

    /// <inheritdoc />
    public bool Remove(byte item)
    {
        int index = IndexOf(item);
        if (index is -1) return false;

        RemoveAtInternal(index);
        return true;
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException(nameof(index), index, "Cannot remove outside of chunk");
        RemoveAtInternal(index);
    }

    /// <inheritdoc cref="IList{T}.RemoveAt" />
    private void RemoveAtInternal(in int index)
    {
        this.Count--;
        this.version++;
        if (index == this.Count) return;

        int from = index + 1;
        int length = this.Count - index;
        Buffer.BlockCopy(this.code, from, this.code, index, length);
    }

    /// <inheritdoc />
    public void CopyTo(byte[] array, int arrayIndex) => Buffer.BlockCopy(this.code, 0, array, arrayIndex, this.Count);

    /// <inheritdoc />
    public void Clear()
    {
        this.Span.Clear();
        this.version++;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    public ChunkEnumerator GetEnumerator() => new(this);

    /// <inheritdoc />
    IEnumerator<byte> IEnumerable<byte>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion
}
