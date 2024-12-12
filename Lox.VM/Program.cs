
using Lox.VM;
using Lox.VM.Utils;

Console.WriteLine("Hello, World!");

LoxChunk chunk = new();

chunk.AddConstant(new LoxValue(1.2d));
chunk.AddOpcode(Opcode.OP_RETURN);
BytecodeUtils.PrintChunk(chunk, "test chunk");