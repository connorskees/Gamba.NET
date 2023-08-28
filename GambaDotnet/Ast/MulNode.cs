using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public class MulNode : BinaryNode
    {
        public override AstKind Kind => AstKind.Mul;

        public MulNode(AstNode op1, AstNode op2) : base(op1, op2)
        {

        }
    }
}
