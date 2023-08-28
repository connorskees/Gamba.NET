using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public class XorNode : BinaryNode
    {
        public override AstKind Kind => AstKind.Xor;

        public XorNode(AstNode op1, AstNode op2) : base(op1, op2)
        {

        }
    }
}
