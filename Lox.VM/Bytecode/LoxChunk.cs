using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Lox.VM.Runtime;

namespace Lox.VM.Bytecode;

/// <summary>
/// Constant value type
/// </summary>
public enum ConstantType : byte
{
    CONSTANT   = LoxOpcode.CONSTANT,
    NDF_GLOBAL = LoxOpcode.NDF_GLOBAL,
    DEF_GLOBAL = LoxOpcode.DEF_GLOBAL,
    GET_GLOBAL = LoxOpcode.GET_GLOBAL,
    SET_GLOBAL = LoxOpcode.SET_GLOBAL
}

/// <summary>
/// Lox bytecode chunk
/// </summary>
[PublicAPI]
public partial class LoxChunk : IList<byte>, IReadOnlyList<byte>, IDisposable
{
    #region Fields
    private int version;
    private readonly List<byte> code       = [];
    private readonly List<LoxValue> values = [];
    private readonly List<int> lines       = [];
    #endregion

    #region Properties
    /// <inheritdoc cref="List{T}.Count" />
    public int Count => this.code.Count;

    /// <summary>
    /// If this object has been disposed or not
    /// </summary>
    public bool IsDisposed { get; private set; }
    #endregion

    #region Indexer
    /// <inheritdoc cref="List{T}.this[int]" />
    public byte this[int index]
    {
        get => this.code[index];
        set => this.code[index] = value;
    }

    /// <inheritdoc cref="List{T}.this" />
    public byte this[Index index]
    {
        get => this.code[index];
        set => this.code[index] = value;
    }

    /// <summary>
    /// Gets a readonly span over a specified range of the bytecode
    /// </summary>
    /// <param name="range">Range to get</param>
    public ReadOnlySpan<byte> this[Range range] => CollectionsMarshal.AsSpan(this.code)[range];
    #endregion

    #region Constructors
    /// <summary>
    /// Chunk finalizer
    /// </summary>
    ~LoxChunk() => Dispose();
    #endregion

    #region Methods
    /// <summary>
    /// Span of the bytecode
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan() => CollectionsMarshal.AsSpan(this.code);

    /// <summary>
    /// Adds the given opcode to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to add</param>
    /// <param name="line">Line for this opcode</param>
    public void AddOpcode(LoxOpcode opcode, int line)
    {
        this.version++;
        this.code.Add((byte)opcode);
        AddLine(line);
    }

    /// <summary>
    /// Adds the given opcode and its operand to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to add</param>
    /// <param name="operand">Opcode operand</param>
    /// <param name="line">Line for this opcode</param>
    public void AddOpcode(LoxOpcode opcode, byte operand, int line)
    {
        this.version++;
        this.code.AddRange((byte)opcode, operand);
        AddLine(line, 2);
    }

    /// <summary>
    /// Adds the given opcode and its 16bit operand to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to add</param>
    /// <param name="operand">Opcode operand</param>
    /// <param name="line">Line for this opcode</param>
    public void AddOpcode(LoxOpcode opcode, ushort operand, int line)
    {
        this.version++;
        Span<byte> bytes = stackalloc byte[2];
        BitConverter.TryWriteBytes(bytes, operand);
        this.code.AddRange((byte)opcode, bytes[0], bytes[1]);
        AddLine(line, 3);
    }

    /// <summary>
    /// Patches a given operand at the specified address
    /// </summary>
    /// <param name="address">Operand address</param>
    /// <param name="operand">Operand value</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PatchJump(int address, ushort operand)
    {
        Span<byte> target = CollectionsMarshal.AsSpan(this.code).Slice(address, 2);
        BitConverter.TryWriteBytes(target, operand);
    }

    /// <summary>
    /// Adds a constant to the chunk
    /// </summary>
    /// <param name="value">Constant to add</param>
    /// <param name="line">Line for this constant</param>
    /// <param name="index">Index the constant was added at</param>
    /// <param name="type">Constant value type</param>
    /// <returns><see langword="true"/> if the constant was successfully added, otherwise <see langword="false"/> if the constant limit has been reached</returns>
    public bool AddConstant(in LoxValue value, int line, out ushort index, ConstantType type)
    {
        int count = this.values.Count;
        if (count >= ushort.MaxValue)
        {
            index = 0;
            return false;
        }

        index = (ushort)count;
        this.values.Add(value);
        AddIndexedConstant(index, line, type);
        return true;
    }

    /// <summary>
    /// Adds a constant opcode for the constant at the given index
    /// </summary>
    /// <param name="index">Index of the constant to add the opcode for</param>
    /// <param name="line">Opcode line</param>
    /// <param name="type">Constant value type</param>
    public void AddIndexedConstant(ushort index, int line, ConstantType type)
    {
        this.version++;
        if (index <= byte.MaxValue)
        {
            this.code.AddRange((byte)type, (byte)index);
            AddLine(line, 2);
        }
        else
        {
            Span<byte> bytes = stackalloc byte[2];
            BitConverter.TryWriteBytes(bytes, index);
            this.code.AddRange((byte)(type + 1), bytes[0], bytes[1]);
            AddLine(line, 3);
        }
    }

    /// <summary>
    /// Gets a constant's value from the chunk
    /// </summary>
    /// <param name="index">Constant index to get</param>
    /// <returns>The constants value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LoxValue GetConstant(ushort index) => ref CollectionsMarshal.AsSpan(this.values)[index];

    /// <summary>
    /// Adds a given line entry
    /// </summary>
    /// <param name="line">Line to add</param>
    /// <param name="repeats">How many time the line appears</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="line"/> is smaller than zero</exception>
    private void AddLine(int line, int repeats = 1)
    {
        if (line < 0) throw new ArgumentOutOfRangeException(nameof(line), line, "Line number cannot be negative");

        // No lines stored
        if (this.lines.Count is 0)
        {
            if (repeats > 1)
            {
                this.lines.Add(-repeats);
            }
            this.lines.Add(line);
            return;
        }

        // Get the lines as a span for easier manipulation
        Span<int> linesSpan = CollectionsMarshal.AsSpan(this.lines);
        ref int lastLine = ref linesSpan[^1];
        // Previous line is different from current line
        if (lastLine != line)
        {
            if (repeats > 1)
            {
                this.lines.Add(-repeats);
            }

            this.lines.Add(line);
            return;
        }

        // Only one value stored
        if (linesSpan.Length is 1)
        {
            // Set the last line to an encoding value and push the line to the end
            lastLine = -repeats - 1;
            this.lines.Add(line);
            return;
        }

        // Get encoding value
        ref int currentEncoding = ref linesSpan[^2];
        // Value before is not an encoding value
        if (currentEncoding >= 0)
        {
            lastLine = -repeats - 1;
            this.lines.Add(line);
            return;
        }

        // Decrease encoding value
        currentEncoding -= repeats;
    }

    /// <summary>
    /// Get the line number of the specified bytecode
    /// </summary>
    /// <param name="index">Bytecode index</param>
    /// <returns>The line number for this bytecode index</returns>
    public int GetLine(int index)
    {
        int offset = 0;
        int currentLine;
        do
        {
            currentLine = this.lines[offset++];
            int currentEncoding = -1;
            if (currentLine < 0)
            {
                currentEncoding = currentLine;
                currentLine     = this.lines[offset++];
            }

            index += currentEncoding;
        }
        while (index >= 0);

        return currentLine;
    }

    /// <summary>
    /// Fills the provided buffer with the bytes at the end of the current chunk, then clears them from the chunk
    /// </summary>
    /// <param name="buffer">Buffer to fill</param>
    public void RequestLastBytes(in Span<byte> buffer)
    {
        if (buffer.Length is 0) return;
        if (buffer.Length > this.code.Count) throw new ArgumentOutOfRangeException(nameof(buffer), buffer.Length, "Expected buffer length is longer than current chunk length.");

        // Copy to target buffer
        int start = this.code.Count - buffer.Length;
        Span<byte> requested = CollectionsMarshal.AsSpan(this.code)[start..];
        requested.CopyTo(buffer);

        // Clear requested buffer
        this.code.RemoveRange(start, buffer.Length);
    }

    /// <summary>
    /// Appends the provided buffer at the end of the current chunk
    /// </summary>
    /// <param name="buffer">Buffer to append</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBytes(ReadOnlySpan<byte> buffer) => this.code.AddRange(buffer);

    /// <summary>
    /// Get bytecode info at the given index
    /// </summary>
    /// <param name="index">Bytecode index</param>
    /// <returns>A tuple containing the bytecode and line for the given index</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (byte bytecode, int line) GetBytecodeInfo(int index) => (this.code[index], GetLine(index));

    /// <inheritdoc cref="List{T}.Clear" />
    public void Clear()
    {
        this.version = 0;
        this.code.Clear();
        this.lines.Clear();
    }

    /// <summary>
    /// Grabs an array of the bytecode
    /// </summary>
    /// <returns>Bytecode array</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ToBytecodeArray() => this.code.ToArray();

    /// <inheritdoc cref="List{T}.GetEnumerator" />
    public List<byte>.Enumerator GetEnumerator() => this.code.GetEnumerator();

    /// <summary>
    /// Gets a bytecode enumerator over this chunk
    /// </summary>
    /// <returns>Bytecode enumerator</returns>
    public BytecodeEnumerator GetBytecodeEnumerator() => new(this);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (LoxValue value in this.values)
        {
            value.FreeResources();
        }
        this.values.Clear();
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Explicit interface implementations
    /// <inheritdoc />
    bool ICollection<byte>.IsReadOnly => false;

    /// <inheritdoc />
    void ICollection<byte>.Add(byte item) => this.code.Add(item);

    /// <inheritdoc />
    bool ICollection<byte>.Contains(byte item) => this.code.Contains(item);

    /// <inheritdoc />
    void ICollection<byte>.CopyTo(byte[] array, int arrayIndex) => this.code.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    bool ICollection<byte>.Remove(byte item) => this.code.Remove(item);

    /// <inheritdoc />
    int IList<byte>.IndexOf(byte item) => this.code.IndexOf(item);

    /// <inheritdoc />
    void IList<byte>.Insert(int index, byte item) => this.code.Insert(index, item);

    /// <inheritdoc />
    void IList<byte>.RemoveAt(int index) => this.code.RemoveAt(index);

    /// <inheritdoc />
    IEnumerator<byte> IEnumerable<byte>.GetEnumerator() => this.code.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.code).GetEnumerator();
    #endregion
}
