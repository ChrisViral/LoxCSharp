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
    private void ReadConstant(ushort index) => this.stack.Push(this.currentChunk.GetConstant(index));

    /// <summary>
    /// Defines an initialized global variable
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DefineGlobal(ushort index)
    {
        ref LoxValue identifier = ref this.currentChunk.GetConstant(index);
        // ReSharper disable once SuggestVarOrType_Elsewhere
        var internedLookup = this.globals.GetAlternateLookup<ReadOnlySpan<char>>();
        internedLookup[identifier.RawStringUnsafe.AsSpan()] = this.stack.Pop();
    }

    /// <summary>
    /// Defines an uninitialized global variable with the specified identifier
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NDefineGlobal(ushort index)
    {
        ref LoxValue identifier = ref this.currentChunk.GetConstant(index);
        // ReSharper disable once SuggestVarOrType_Elsewhere
        var internedLookup = this.globals.GetAlternateLookup<ReadOnlySpan<char>>();
        internedLookup[identifier.RawStringUnsafe.AsSpan()] = LoxValue.Nil;
    }

    /// <summary>
    /// Puts the value of a global variable onto the stack
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetGlobal(ushort index)
    {
        ref LoxValue identifier = ref this.currentChunk.GetConstant(index);
        // ReSharper disable once SuggestVarOrType_Elsewhere
        var internedLookup = this.globals.GetAlternateLookup<ReadOnlySpan<char>>();
        if (!internedLookup.TryGetValue(identifier.RawStringUnsafe.AsSpan(), out LoxValue value))
        {
            throw new LoxRuntimeException($"Undefined variable {identifier.RawStringUnsafe.AsSpan()}");
        }

        this.stack.Push(value);
    }

    /// <summary>
    /// Sets the value of a global variable
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetGlobal(ushort index)
    {
        ref LoxValue identifier = ref this.currentChunk.GetConstant(index);
        // ReSharper disable once SuggestVarOrType_Elsewhere
        var internedLookup = this.globals.GetAlternateLookup<ReadOnlySpan<char>>();
        ReadOnlySpan<char> identifierSpan = identifier.RawStringUnsafe.AsSpan();
        if (!internedLookup.ContainsKey(identifierSpan))
        {
            throw new LoxRuntimeException($"Undefined variable {identifierSpan}");
        }

        internedLookup[identifierSpan] = this.stack.Peek();
    }

    /// <summary>
    /// Puts the value of a local variable onto the stack
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetLocal(ushort index) => this.stack.Push(this.stack[index]);

    /// <summary>
    /// Sets the value of a local variable
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetLocal(ushort index) => this.stack[index] = this.stack.Peek();

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
        // ReSharper disable once SuggestVarOrType_Elsewhere
        var internedLookup = this.interned.GetAlternateLookup<ReadOnlySpan<char>>();
        if (internedLookup.TryGetValue(concat, out LoxValue value)) return value;

        // If not found, allocate and intern
        RawString concatRaw = RawString.Allocate(concat, out IntPtr address);
        this.allocations.Add(address);
        internedLookup[concat] = concatRaw;
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
    /// Jumps to the given offset
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void Jump() => this.instructionPointer += ReadUInt16();

    /// <summary>
    /// Jumps to the given offset if the top value of the stack evaluates to true, otherwise pops it
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void JumpTrue()
    {
        if (this.stack.Peek().IsTruthy)
        {
            this.instructionPointer += ReadUInt16();
        }
        else
        {
            this.stack.Pop();
            this.instructionPointer += 2;
        }
    }

    /// <summary>
    /// Jumps to the given offset if the top value of the stack evaluates to true and pops it
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void JumpTruePop()
    {
        if (this.stack.Pop().IsTruthy)
        {
            this.instructionPointer += ReadUInt16();
        }
        else
        {
            this.instructionPointer += 2;
        }
    }

    /// <summary>
    /// Jumps to the given offset if the top value of the stack evaluates to false, otherwise pops it
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void JumpFalse()
    {
        if (this.stack.Peek().IsFalsey)
        {
            this.instructionPointer += ReadUInt16();
        }
        else
        {
            this.stack.Pop();
            this.instructionPointer += 2;
        }
    }

    /// <summary>
    /// Jumps to the given offset if the top value of the stack evaluates to false and pops it
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void JumpFalsePop()
    {
        if (this.stack.Pop().IsFalsey)
        {
            this.instructionPointer += ReadUInt16();
        }
        else
        {
            this.instructionPointer += 2;
        }
    }

    /// <summary>
    /// Loops back by a given offset
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void Loop() => this.instructionPointer -= ReadUInt16();

    /// <summary>
    /// Return operation
    /// </summary>
    /// <returns>The execution result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static InterpretResult Return() => InterpretResult.SUCCESS;
}
