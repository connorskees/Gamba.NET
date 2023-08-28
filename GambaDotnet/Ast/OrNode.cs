using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public class OrNode : BinaryNode
    {
        public override AstKind Kind => AstKind.Or;

        public OrNode(AstNode op1, AstNode op2) : base(op1, op2)
        {
        }
    }
}
