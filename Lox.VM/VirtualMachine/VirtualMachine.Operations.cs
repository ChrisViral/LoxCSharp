using System.Runtime.CompilerServices;
using Lox.VM.Exceptions.Runtime;
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
        ref LoxValue topValue = ref *top;
        if (!topValue.TryGetNumber(out double number)) throw new LoxRuntimeException("Negation operand must be a number.", this.CurrentLine);

        *top = -number;
    }

    /// <summary>
    /// Adds the top two value of the stack together and returns them
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Add()
    {
        if (this.stack.TryPopNumbers(out double a, out double b))
        {
            this.stack.Push(a + b);
        }
        else if (this.stack.TryPopStrings(out RawString left, out RawString right))
        {
            IntPtr allocatedResult = RawString.Concat(left, right, out RawString concatenated);
            this.allocations.Add(allocatedResult);
            this.stack.Push(concatenated);
        }
        else
        {
            throw new LoxRuntimeException("Operands must be a numbers or strings.", this.CurrentLine);
        }
    }

    /// <summary>
    /// Subtracts the top two value of the stack together and returns them
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Subtract()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a - b);
    }

    /// <summary>
    /// Multiplies the top two value of the stack together and returns them
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Multiply()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a * b);
    }

    /// <summary>
    /// Divides the top two value of the stack together and returns them
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Divide()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a / b);
    }

    /// <summary>
    /// Checks if both values atop the stack are equal
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Equal()
    {
        ref LoxValue b = ref this.stack.Pop();
        ref LoxValue a = ref this.stack.Pop();
        this.stack.Push(a.Equals(b));
    }

    /// <summary>
    /// Checks if both values atop the stack aren't equal
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NotEqual()
    {
        ref LoxValue b = ref this.stack.Pop();
        ref LoxValue a = ref this.stack.Pop();
        this.stack.Push(a.NotEquals(b));
    }

    /// <summary>
    /// Compares the two values atop the stack to see if the first is greater than the second
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Greater()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a > b);
    }

    /// <summary>
    /// Compares the two values atop the stack to see if the first is greater or equal to the second
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GreaterEqual()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a >= b);
    }

    /// <summary>
    /// Compares the two values atop the stack to see if the first is less than the second
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Less()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a < b);
    }

    /// <summary>
    /// Compares the two values atop the stack to see if the first is less or equal to the second
    /// </summary>
    /// <returns>The result of the operation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LessEqual()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a <= b);
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
