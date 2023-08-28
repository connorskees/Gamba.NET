using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public class RefNode : UnaryNode
    {
        public override AstKind Kind => AstKind.Ref;

        public RefNode(AstNode op1) : base(op1)
        {

        }
    }
}
