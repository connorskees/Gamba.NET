using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public class PowerNode : BinaryNode
    {
        public override AstKind Kind => AstKind.Power;

        public PowerNode(AstNode op1, int power) : base(op1, new ConstNode(power, op1.BitSize))
        {
        }

        public PowerNode(AstNode op1, AstNode op2) : base(op1, op2)
        {

        }
    }
}
