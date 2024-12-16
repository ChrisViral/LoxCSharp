using System.Runtime.CompilerServices;
using Lox.VM.Exceptions.Runtime;
using Lox.VM.Runtime;

namespace Lox.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// Reads the next constant in the bytecode
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReadConstant8() => this.stack.Push(this.currentChunk.GetConstant(ReadByte()));

    /// <summary>
    /// Reads the next 16bit constant in the bytecode
    /// </summary>
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
    /// Negates the current top value on the stack
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Not()
    {
        ref LoxValue topValue = ref this.stack.Peek();
        topValue = topValue.IsFalsey;
    }

    /// <summary>
    /// Adds the top two value of the stack together and returns them
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Negate()
    {
        ref LoxValue topValue = ref this.stack.Peek();
        if (!topValue.TryGetNumber(out double number)) throw new LoxRuntimeException("Negation operand must be a number.", this.CurrentLine);

        topValue = -number;
    }

    /// <summary>
    /// Adds the top two value of the stack together and returns them
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Add()
    {
        if (this.stack.TryPopNumbers(out double a, out double b))
        {
            this.stack.Push(a + b);
        }
        else if (this.stack.TryPopStrings(out RawString left, out RawString right))
        {
            this.stack.Push(ConcatStrings(left, right));
        }
        else
        {
            throw new LoxRuntimeException("Operands must be a numbers or strings.", this.CurrentLine);
        }
    }

    /// <summary>
    /// Concatenates two strings and pushes the result to the stack
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>The concatenated string value struct</returns>
    private LoxValue ConcatStrings(in RawString left, in RawString right)
    {
        // Concat the string in a local span
        Span<char> concat = stackalloc char[left.length + right.length];
        left.AsSpan().CopyTo(concat);
        right.AsSpan().CopyTo(concat[left.length..]);

        // Try and get from interned strings
        if (this.interned.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(concat, out LoxValue value)) return value;

        // If not found, allocate and intern
        this.allocations.Add(RawString.Allocate(concat, out RawString concatRaw));
        this.interned.Add(concat.ToString(), concatRaw);
        return concatRaw;
    }

    /// <summary>
    /// Subtracts the top two value of the stack together and returns them
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Subtract()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a - b);
    }

    /// <summary>
    /// Multiplies the top two value of the stack together and returns them
    /// </summary>
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Greater()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a > b);
    }

    /// <summary>
    /// Compares the two values atop the stack to see if the first is greater or equal to the second
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GreaterEqual()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a >= b);
    }

    /// <summary>
    /// Compares the two values atop the stack to see if the first is less than the second
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Less()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a < b);
    }

    /// <summary>
    /// Compares the two values atop the stack to see if the first is less or equal to the second
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LessEqual()
    {
        if (!this.stack.TryPopNumbers(out double a, out double b)) throw new LoxRuntimeException("Operands must be a numbers.", this.CurrentLine);
        this.stack.Push(a <= b);
    }

    /// <summary>
    /// Print operation
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Print() => PrintValue(this.stack.Pop());

    /// <summary>
    /// Return operation
    /// </summary>
    /// <returns>The execution result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static InterpretResult Return() => InterpretResult.SUCCESS;
}
