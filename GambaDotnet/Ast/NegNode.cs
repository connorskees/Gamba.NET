using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public class NegNode : UnaryNode
    {
        public override AstKind Kind => AstKind.Neg;

        public NegNode(AstNode op1) : base(op1)
        {

        }
    }
}
