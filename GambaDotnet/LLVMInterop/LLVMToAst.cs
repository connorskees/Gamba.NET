using Gamba.Ast;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.LLVMInterop
{
    public class LLVMToAst
    {
        public static AstNode ToAst(LLVMValueRef value, uint bitSize, Func<LLVMValueRef, VarNode> getArg)
        {
            // Concise operand AST getter methods.
            var op1 = () => ToAst(value.GetOperand(0), bitSize, getArg);
            var op2 = () => ToAst(value.GetOperand(1), bitSize, getArg);
            var op3 = () => ToAst(value.GetOperand(2), bitSize, getArg);

            if (value.Kind == LLVMValueKind.LLVMArgumentValueKind)
                return getArg(value);
            if (value.Kind == LLVMValueKind.LLVMConstantIntValueKind)
                return new ConstNode(value.ConstIntZExt, value.TypeOf.IntWidth);
            else if (value.Kind != LLVMValueKind.LLVMInstructionValueKind)
                throw new InvalidOperationException($"Cannot lift llvm value kind {value.Kind} to ast.");

            AstNode ast = value.InstructionOpcode switch
            {
                LLVMOpcode.LLVMOr => new OrNode(op1(), op2()),
                LLVMOpcode.LLVMXor => new XorNode(op1(), op2()),
                LLVMOpcode.LLVMAnd => new AndNode(op1(), op2()),
                LLVMOpcode.LLVMMul => new MulNode(op1(), op2()),
                LLVMOpcode.LLVMAdd => new AddNode(op1(), op2()),
                LLVMOpcode.LLVMShl => new MulNode(op1(), new PowerNode(new ConstNode(2, bitSize), op2())),
                // Rewrite (a + b) as (a + (b * -1))
                LLVMOpcode.LLVMSub => new AddNode(op1(), new MulNode(op2(), new ConstNode(ulong.MaxValue, bitSize))),
                _ => throw new InvalidOperationException($"Cannot lift instruction opcode {value.InstructionOpcode} to llvm.")
            }; ;

            return ast;
        }
    }
}
