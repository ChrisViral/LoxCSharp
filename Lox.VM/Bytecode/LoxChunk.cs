using System.Collections;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Lox.VM.Runtime;

namespace Lox.VM.Bytecode;

/// <summary>
/// Lox bytecode chunk
/// </summary>
[PublicAPI]
public partial class LoxChunk : IList<byte>, IReadOnlyList<byte>
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
    #endregion

    #region Indexer
    /// <inheritdoc cref="List{T}.this[int]" />
    public byte this[int index]
    {
        get => this.code[index];
        set => this.code[index] = value;
    }

    /// <inheritdoc cref="List{T}.this" />
    public byte this[in Index index]
    {
        get => this.code[index];
        set => this.code[index] = value;
    }

    /// <summary>
    /// Gets a readonly span over a specified range of the bytecode
    /// </summary>
    /// <param name="range">Range to get</param>
    public ReadOnlySpan<byte> this[in Range range] => CollectionsMarshal.AsSpan(this.code)[range];
    #endregion

    #region Methods
    /// <summary>
    /// Adds the given opcode to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to add</param>
    /// <param name="line">Line for this opcode</param>
    public void AddOpcode(in Opcode opcode, in int line)
    {
        this.version++;
        this.code.Add((byte)opcode);
        AddLine(line);
    }

    /// <summary>
    /// Adds a constant to the chunk
    /// </summary>
    /// <param name="value">Constant to add</param>
    /// <param name="line">Line for this constant</param>
    /// <returns>The index at which the constant was added</returns>
    public int AddConstant(in LoxValue value, in int line)
    {
        this.version++;
        int index = this.values.Count;
        this.values.Add(value);

        if (index > byte.MaxValue)
        {
            Span<byte> bytes = stackalloc byte[5];
            bytes[0] = (byte)Opcode.OP_CONSTANT_LONG;
            BitConverter.TryWriteBytes(bytes[1..], index);
            this.code.AddRange(bytes[..4]);
            AddLine(line, 4);
        }
        else
        {
            this.code.AddRange((byte)Opcode.OP_CONSTANT, (byte)index);
            AddLine(line, 2);
        }

        return index;
    }

    /// <summary>
    /// Gets a constant's value from the chunk
    /// </summary>
    /// <param name="index">Constant index to get</param>
    /// <returns>The constants value</returns>
    public LoxValue GetConstant(in int index) => this.values[index];

    /// <summary>
    /// Adds a given line entry
    /// </summary>
    /// <param name="line">Line to add</param>
    /// <param name="repeats">How many time the line appears</param>
    private void AddLine(in int line, in int repeats = 1)
    {
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
        if (currentEncoding > 0)
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
    /// Get bytecode info at the given index
    /// </summary>
    /// <param name="index">Bytecode index</param>
    /// <returns>A tuple containing the bytecode and line for the given index</returns>
    public (byte bytecode, int line) GetBytecodeInfo(in int index) => (this.code[index], GetLine(index));

    /// <inheritdoc cref="List{T}.Clear" />
    public void Clear()
    {
        this.version = 0;
        this.code.Clear();
        this.values.Clear();
        this.lines.Clear();
    }

    /// <inheritdoc cref="List{T}.GetEnumerator" />
    public List<byte>.Enumerator GetEnumerator() => this.code.GetEnumerator();

    /// <summary>
    /// Gets a bytecode enumerator over this chunk
    /// </summary>
    /// <returns>Bytecode enumerator</returns>
    public BytecodeEnumerator GetBytecodeEnumerator() => new(this);
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
