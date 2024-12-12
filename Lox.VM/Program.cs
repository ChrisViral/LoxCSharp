using Lox.VM;
using Lox.VM.Bytecode;
using Lox.VM.Runtime;
using Lox.VM.Utils;

LoxChunk chunk = new();

for (int i = 0; i < 260; i++)
{
    chunk.AddConstant(new LoxValue(i), i);
}

BytecodeUtils.PrintChunk(chunk, "test");