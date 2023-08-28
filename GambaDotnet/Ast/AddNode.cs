using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public class AddNode : BinaryNode
    {
        public override AstKind Kind => AstKind.Add;

        public AddNode(AstNode op1, AstNode op2) : base(op1, op2)
        {

        }
    }
}
