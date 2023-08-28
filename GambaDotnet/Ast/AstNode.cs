using Gamba.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Ast
{
    public enum AstKind
    {
        Const,
        Var,
        Ref,
        Power,
        Add,
        Mul,
        And,
        Or,
        Xor,
        Neg,
    }

    public abstract class AstNode
    {
        private List<AstNode> operands;

        public IReadOnlyList<AstNode> Children => operands.AsReadOnly();

        public abstract AstKind Kind { get; }

        public string Operator => AstFormatter.GetOperatorName(Kind);

        public uint BitSize { get; }

        protected int Hash { get; private set; }

        protected abstract int OpCount { get; }

        public AstNode(uint bitSize, params AstNode[] nodes)
        {
            BitSize = bitSize;
            operands = nodes.ToList();
            Hash = ComputeHash();
        }

        public AstNode(uint bitSize, out Action forceComputeHash)
        {
            BitSize = bitSize;
            operands = new();

            // Temporary workaround to allow properties
            // to be set by derived classes before we 
            // calculate the hash.
            forceComputeHash = (() =>
            {
                Hash = ComputeHash();
            });
        }

        public void SetOperand(int index, AstNode operand)
        {
            var last = operands[index];
            if (last != null && last.BitSize != operand.BitSize)
                throw new InvalidOperationException($"Cannot replace operand {last} with {operand}. The bit sizes do not match.");

            // Update the hash on write.
            operands[index] = operand;
            Hash = ComputeHash();
        }

        protected virtual int ComputeHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Kind.GetHashCode();
                hash = hash * 23 + BitSize.GetHashCode();
                for (int i = 0; i < operands.Count; i++)
                    hash = hash * 23 + operands[i].GetHashCode();
                return hash;
            }
        }

        public override int GetHashCode() => Hash;

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is not AstNode astNode)
                return false;
            if (BitSize != astNode.BitSize || Kind != astNode.Kind || Hash != astNode.Hash)
                return false;
            return AstFormatter.FormatAst(this) == AstFormatter.FormatAst(astNode);
        }

        public override string ToString() => AstFormatter.FormatAst(this);
    }
}
