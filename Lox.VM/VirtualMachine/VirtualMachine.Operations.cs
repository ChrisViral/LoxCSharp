using System.Runtime.CompilerServices;
using Lox.VM.Runtime;

namespace Lox.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// Reads the next constant in the bytecode
    /// </summary>
    /// <returns>Stored constant value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadConstant8() => this.stack.Push(this.currentChunk.GetConstant(ReadByte()));

    /// <summary>
    /// Reads the next 16bit constant in the bytecode
    /// </summary>
    /// <returns>Stored constant value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadConstant16()
    {
        byte a = ReadByte();
        byte b = ReadByte();
        int index = BitConverter.ToUInt16([a, b]);
        this.stack.Push(this.currentChunk.GetConstant(index));
    }

    /// <summary>
    /// Reads the next 24bit constant in the bytecode
    /// </summary>
    /// <returns>Stored constant value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadConstant24()
    {
        byte a = ReadByte();
        byte b = ReadByte();
        byte c = ReadByte();
        int index = BitConverter.ToInt32([a, b, c, 0]);
        this.stack.Push(this.currentChunk.GetConstant(index));
    }

    /// <summary>
    /// Adds the top two value of the stack together and returns them
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void Negate()
    {
        LoxValue* top = this.stack.GetTop();
        *top = -(*top).value;
    }

    /// <summary>
    /// Adds the top two value of the stack together and returns them
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Add()
    {
        LoxValue b = this.stack.Pop();
        LoxValue a = this.stack.Pop();
        this.stack.Push(a.value + b.value);
    }

    /// <summary>
    /// Subtracts the top two value of the stack together and returns them
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Subtract()
    {
        LoxValue b = this.stack.Pop();
        LoxValue a = this.stack.Pop();
        this.stack.Push(a.value - b.value);
    }

    /// <summary>
    /// Multiplies the top two value of the stack together and returns them
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Multiply()
    {
        LoxValue b = this.stack.Pop();
        LoxValue a = this.stack.Pop();
        this.stack.Push(a.value * b.value);
    }

    /// <summary>
    /// Divides the top two value of the stack together and returns them
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Divide()
    {
        LoxValue b = this.stack.Pop();
        LoxValue a = this.stack.Pop();
        this.stack.Push(a.value / b.value);
    }

    /// <summary>
    /// Return operation
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private InterpretResult Return()
    {
        PrintValue(this.stack.Pop());
        return InterpretResult.SUCCESS;
    }
}
