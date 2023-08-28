using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public class VarNode : AstNode
    {
        public override AstKind Kind => AstKind.Var;

        protected override int OpCount => 0;

        public string Name { get; }

        public VarNode(string name, uint bitSize) : base(bitSize, out Action recalcHash)
        {
            Name = name;
            recalcHash();
        }

        protected override int ComputeHash()
        {
            return base.ComputeHash() * 23 + Name.GetHashCode();
        }
    }
}
