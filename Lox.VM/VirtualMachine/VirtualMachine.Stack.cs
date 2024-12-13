using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Lox.VM.Runtime;

namespace Lox.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// VM value stack
    /// </summary>
    private unsafe struct Stack : IDisposable
    {
        private static readonly StringBuilder StackBuilder = new();

        /// <summary>
        /// Max stack size
        /// </summary>
        private const int MAX_SIZE = byte.MaxValue + 1;

        private IntPtr handle;
        private LoxValue* stack;
        private LoxValue* top;

        /// <summary>
        /// Current stack size
        /// </summary>
        public int Size => (int)(this.top - this.stack);

        /// <summary>
        /// Allocates a new stack to unmanaged memory
        /// </summary>
        public Stack()
        {
            this.handle = Marshal.AllocHGlobal(MAX_SIZE * sizeof(LoxValue));
            this.stack  = (LoxValue*)this.handle.ToPointer();
            this.top    = this.stack;
        }

        /// <summary>
        /// Pushes a value onto the stack
        /// </summary>
        /// <param name="value">Value to push</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(in LoxValue value)
        {
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
        /// Resets the stack
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => this.top = this.stack;

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
            this.handle = IntPtr.Zero;
            this.stack  = null;
            this.top    = null;
        }
    }
}
