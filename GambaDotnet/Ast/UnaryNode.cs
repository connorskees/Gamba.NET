using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public abstract class UnaryNode : AstNode
    {
        protected override int OpCount => 2;

        public UnaryNode(AstNode op1) : base(op1.BitSize, op1)
        {

        }
    }
}
