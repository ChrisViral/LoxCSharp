using Lox.VM;
using Lox.VM.Bytecode;
using Lox.VM.Runtime;

LoxChunk chunk = new();

chunk.AddConstant(new LoxValue(1.2), 1);
chunk.AddOpcode(Opcode.RETURN, 2);

VirtualMachine virtualMachine = new(chunk);
InterpretResult result = virtualMachine.Interpret();

Environment.Exit((int)result);
