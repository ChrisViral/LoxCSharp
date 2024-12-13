﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Lox.VM.Runtime;

namespace Lox.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// VM value stack
    /// </summary>
    [PublicAPI]
    private sealed unsafe class Stack : IDisposable
    {
        #region Constants
        /// <summary>
        /// Max stack size
        /// </summary>
        private const int DEFAULT_SIZE = byte.MaxValue + 1;
        /// <summary>
        /// Stack print stringbuilder
        /// </summary>
        private static readonly StringBuilder StackBuilder = new();
        #endregion

        #region Fields
        private IntPtr handle;
        private LoxValue* stack;
        private LoxValue* top;
        #endregion

        #region Properties
        /// <summary>
        /// Stack capacity
        /// </summary>
        public nint Capacity { get; private set; }

        /// <summary>
        /// Current stack size
        /// </summary>
        public nint Size => (nint)(this.top - this.stack);
        #endregion

        #region Constructor
        /// <summary>
        /// Allocates a new stack to unmanaged memory
        /// </summary>
        public Stack()
        {
            this.Capacity = DEFAULT_SIZE;
            this.handle  = Marshal.AllocHGlobal(DEFAULT_SIZE * sizeof(LoxValue));
            this.stack   = (LoxValue*)this.handle.ToPointer();
            this.top     = this.stack;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Pushes a value onto the stack
        /// </summary>
        /// <param name="value">Value to push</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(in LoxValue value)
        {
            if (this.Size == this.Capacity) GrowStack();
            *this.top = value;
            this.top++;
        }

        /// <summary>
        /// Pops a value from the stack
        /// </summary>
        /// <returns>The popped value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoxValue Pop()
        {
            this.top--;
            return *this.top;
        }

        /// <summary>
        /// Returns a the top value of the stack
        /// </summary>
        /// <returns>Top value of the stack</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoxValue Peek() => *(this.top - 1);

        /// <summary>
        /// Gets a pointer to the top value of the stack
        /// </summary>
        /// <returns>Pointer to the last value in the stack</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoxValue* GetTop() => this.top - 1;

        /// <summary>
        /// Resets the stack
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => this.top = this.stack;

        /// <summary>
        /// Grows the stack
        /// </summary>
        private void GrowStack()
        {
            nint oldCapacity = this.Capacity;
            this.Capacity *= 2;
            this.handle    = Marshal.ReAllocHGlobal(this.handle, this.Capacity * sizeof(LoxValue));
            this.stack     = (LoxValue*)this.handle.ToPointer();
            this.top       = this.stack + oldCapacity;
        }

        /// <summary>
        /// Prints the stack to the console
        /// </summary>
        public void PrintStack()
        {
            StackBuilder.Append("          ");
            for (LoxValue* slot = this.stack; slot < this.top; slot++)
            {
                StackBuilder.Append($"[ {(*slot).ToString()} ]");
            }

            StackBuilder.AppendLine();
            Console.Write(StackBuilder.ToString());
            StackBuilder.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Marshal.FreeHGlobal(this.handle);
            this.Capacity = 0;
            this.handle   = IntPtr.Zero;
            this.stack    = null;
            this.top      = null;
        }
        #endregion
    }
}