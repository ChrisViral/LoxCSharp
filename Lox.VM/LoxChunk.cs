using System.Collections;
using JetBrains.Annotations;

namespace Lox.VM;

/// <summary>
/// Lox bytecode chunk
/// </summary>
[PublicAPI]
public class LoxChunk : IList<byte>, IReadOnlyList<byte>
{
    #region Fields
    private readonly List<byte> code = [];
    private readonly List<LoxValue> values = [];
    #endregion

    #region Properties
    /// <inheritdoc cref="IList{T}.Count" />
    public int Count => this.code.Count;
    #endregion

    #region Indexer
    /// <inheritdoc cref="IList{T}.this" />
    public byte this[int index]
    {
        get => this.code[index];
        set => this.code[index] = value;
    }
    #endregion

    #region Methods
    /// <summary>
    /// Adds the given opcode to the chunk
    /// </summary>
    /// <param name="opcode">Opcode to add</param>
    public void AddOpcode(in Opcode opcode) => this.code.Add((byte)opcode);

    /// <summary>
    /// Adds a constant to the chunk
    /// </summary>
    /// <param name="value">Constant to add</param>
    /// <returns>The index at which the constant was added</returns>
    public int AddConstant(in LoxValue value)
    {
        int index = this.values.Count;
        this.values.Add(value);
        this.code.Add((byte)Opcode.OP_CONSTANT);
        this.code.Add((byte)index);
        return index;
    }

    /// <summary>
    /// Gets a constant's value from the chunk
    /// </summary>
    /// <param name="index">Constant index to get</param>
    /// <returns>The constants value</returns>
    public LoxValue GetConstant(in int index) => this.values[index];

    /// <inheritdoc cref="List{T}.Clear" />
    public void Clear() => this.code.Clear();

    /// <inheritdoc cref="List{T}.GetEnumerator" />
    public List<byte>.Enumerator GetEnumerator() => this.code.GetEnumerator();
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
