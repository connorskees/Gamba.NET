using Gamba.Ast;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Symbolic
{
    public class AstToZ3
    {
        private readonly uint bitWidth;

        private int nopeBitwiseCount;

        public Context ctx = new();

        public AstToZ3(uint bitWidth)
        {
            ctx.PrintMode = Z3_ast_print_mode.Z3_PRINT_LOW_LEVEL;
            
            this.bitWidth = bitWidth;
        }

        public Expr Translate(AstNode expression, AstNode? parent = null)
        {
            // Concise operand AST getter methods.
            var op1 = () => Translate(expression.Children[0], expression);
            var op2 = () => Translate(expression.Children[1], expression);
            var op3 = () => Translate(expression.Children[2], expression);

            // Concise bit vector AST getter methods.
            var bv1 = () => (BitVecExpr)Translate(expression.Children[0], expression);
            var bv2 = () => (BitVecExpr)Translate(expression.Children[1], expression);
            var bv3 = () => (BitVecExpr)Translate(expression.Children[2], expression);

            /*
            if(IsBitwise(expression))
            {
                var res = ctx.MkBVConst($"goodbye_bitwise_{nopeBitwiseCount}", bitWidth);
                nopeBitwiseCount++;
                return res;
            }
            */

            Expr z3Ast = expression switch
            {
                VarNode varNode => ctx.MkBVConst(varNode.Name, bitWidth),
                ConstNode constNode => IsBitwise(parent) ? ctx.MkBVConst($"replaced_constant_{constNode.Value}", bitWidth) : ctx.MkBV(constNode.Value, bitWidth),
                AddNode => ctx.MkBVAdd(bv1(), bv2()),
                MulNode => ctx.MkBVMul(bv1(), bv2()),
                AndNode => ctx.MkBVAND(bv1(), bv2()),
                OrNode => ctx.MkBVOR(bv1(), bv2()),
                XorNode => ctx.MkBVXOR(bv1(), bv2()),
                NegNode => ctx.MkBVNeg(bv1()),
                PowerNode => throw new InvalidOperationException(),
                _ => throw new InvalidOperationException()
            };

            return z3Ast;
        }

        private static bool IsBitwise(AstNode expression)
        {
            if (expression == null)
                return false;

            var kind = expression.Kind;
            if (kind == AstKind.And || kind == AstKind.Or || kind == AstKind.Xor)
                return true;

            return false;
        }
    }
}
