using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public class AndNode : BinaryNode
    {
        public override AstKind Kind => AstKind.And;

        public AndNode(AstNode op1, AstNode op2) : base(op1, op2)
        {

        }
    }
}
