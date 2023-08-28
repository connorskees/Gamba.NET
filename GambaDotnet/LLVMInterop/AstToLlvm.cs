using Gamba.Ast;
using Gamba.Utility;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.LLVMInterop
{
    public class AstToLlvm
    {
        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        private readonly AstNode root;

        // Mapping of <VarNode, LLVM IR function argument index>.
        public Dictionary<VarNode, uint> varArgIndices = new();

        public static LLVMValueRef LiftIntoFunction(LLVMModuleRef module, AstNode node)
        {
            var lifter = new AstToLlvm(module, node);
            return lifter.Lift(node);
        }

        private AstToLlvm(LLVMModuleRef module, AstNode root)
        {
            this.module = module;
            this.builder = LLVMBuilderRef.Create(module.Context);
            this.root = root;

            // Collect the set of expression into variables.
            var set = new HashSet<VarNode>();
            InputVariableUtility.CollectInputVariables(root, set);

            // Apply an arbitrary LLVM function parameter index to each input variable.
            uint i = 0;
            foreach (var inputVariable in set)
            {
                varArgIndices.Add(inputVariable, i);
                i++;
            }

        }

        private LLVMValueRef Lift(AstNode node)
        {
            // Create the function prototype.
            var intType = LLVMTypeRef.CreateInt(root.BitSize);
            var argTypes = Enumerable.Repeat(intType, varArgIndices.Count).ToArray();
            var prototype = LLVMTypeRef.CreateFunction(intType, argTypes);

            // Create the function.
            var func = module.AddFunction("LiftedAst", prototype);
            var entryBlock = func.AppendBasicBlock("bb0");
            builder.PositionAtEnd(entryBlock);

            // Add a 'ret ast' to the end of the block.
            var retValue = ToLlvm(node, func);
            builder.BuildRet(retValue);

            // Return the lifted LLVM IR function.
            return func;
        }

        private LLVMValueRef ToLlvm(AstNode expr, LLVMValueRef func)
        {
            // Concise operand AST getter methods.
            var op1 = () => ToLlvm(expr.Children[0], func);
            var op2 = () => ToLlvm(expr.Children[1], func);
            var op3 = () => ToLlvm(expr.Children[2], func);

            LLVMValueRef result = expr switch
            {
                ConstNode constNode => LLVMValueRef.CreateConstInt(LLVMTypeRef.CreateInt(expr.BitSize), (ulong)constNode.Value),
                VarNode varNode => func.GetParam(varArgIndices[varNode]),
                PowerNode powerNode => LiftPower(powerNode, func),
                AddNode => builder.BuildAdd(op1(), op2()),
                MulNode => builder.BuildMul(op1(), op2()),
                AndNode => builder.BuildAnd(op1(), op2()),
                OrNode => builder.BuildOr(op1(), op2()),
                XorNode => builder.BuildXor(op1(), op2()),
                NegNode => builder.BuildNot(op1()),
                _ => throw new InvalidOperationException($"Cannot lift ast kind: {expr.Kind} to llvm.")
            };

            return result;
        }

        private LLVMValueRef LiftPower(PowerNode powerNode, LLVMValueRef func)
        {
            ConstNode constNode = null;
            if (powerNode.Children[0] is ConstNode const1)
                constNode = const1;
            else if (powerNode.Children[1] is ConstNode const2)
                constNode = const2;

            // If one of the nodes is constant then we unroll it down to repeated multiplier.
            if (constNode != null && constNode.Value <= 32)
            {
                if (constNode.Value <= 0)
                    throw new InvalidOperationException();

                var nonConst = ToLlvm(powerNode.Children.Single(x => x != constNode), func);
                for(ulong i = 0; i < (ulong)(constNode.Value - 1); i++)
                {
                    nonConst = builder.BuildMul(nonConst, nonConst);
                }

                return nonConst;
            }

            else
            {
                Debugger.Break();
                // Cast the input operands to doubles.
                var dblTy = LLVMTypeRef.Double;
                var op1 = ToLlvm(powerNode.Children[0], func);
                var op2 = ToLlvm(powerNode.Children[1], func);

                var fp1 = builder.BuildUIToFP(op1, dblTy);
                var fp2 = builder.BuildUIToFP(op2, dblTy);

                // Create the power intrinsic.
                var prototype = LLVMTypeRef.CreateFunction(dblTy, new LLVMTypeRef[] { dblTy, dblTy });
                var intrinsic = module.AddFunction("pow", prototype);

                // Invoke the intrinsic.
                var pow = builder.BuildCall2(prototype, intrinsic, new LLVMValueRef[] { fp1, fp2 });
                var result = builder.BuildFPToUI(pow, op1.TypeOf);
                return result;
            }
        }
    }
}
