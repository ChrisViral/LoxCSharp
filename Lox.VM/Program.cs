
using Lox.VM;
using Lox.VM.Utils;

Console.WriteLine("Hello, World!");

Chunk test = [];

int constant = test.AddConstant(new LoxValue(1.2d));
test.Add((byte)Opcode.OP_CONSTANT);
test.Add((byte)constant);
test.Add((byte)Opcode.OP_RETURN);
BytecodeUtils.PrintChunk(test, "test chunk");