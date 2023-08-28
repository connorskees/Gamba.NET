using Gamba.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Utility
{
    public class DegreeCalculator
    {
        private Dictionary<AstNode, int?> mapping = new();

        /// <summary>
        /// Calculate the degree of the expression.
        /// </summary>
        public IReadOnlyDictionary<AstNode, int?> GetDegreeMapping(AstNode node)
        {
            // Collect degrees for children nodes.
            foreach (var child in node.Children)
                GetDegreeMapping(child);

            var deg1 = () => mapping[node.Children[0]];
            var deg2 = () => mapping[node.Children[1]];

            switch (node)
            {
                // Multiplication of a variable by a constant number(e.g. x * 7) does not change the degree of the variable.
                case ConstNode:
                    mapping[node] = 0;
                    break;
                // The degree of any unknown input variable is always one.
                case VarNode:
                    mapping[node] = 1;
                    break;

                // The degree of the power node(x^y) is equal to degree(x) * y.
                case PowerNode:
                    var op1 = node.Children[0];
                    var op2 = node.Children[1];
                    // If op1 is a varible and op2 is a constant then deg = degree(op1) * op2.
                    if (op1 is VarNode && op2 is ConstNode op2ConstPower)
                        mapping[node] = ComputePowerNodeDegree(deg1(), (int)op2ConstPower.Value);
                    // Vice versa.
                    else if (op2 is VarNode && op1 is ConstNode op1ConstPower)
                        mapping[node] = ComputePowerNodeDegree(deg2(), (int)op1ConstPower.Value);
                    // Otherwise it's not resolvable.
                    else
                        mapping[node] = null;
                    break;

                case MulNode:
                    mapping[node] = PickHighest(deg1(), deg2());
                    break;
                default:
                    throw new InvalidOperationException($"Cannot handle node type {node}");
            }

            return mapping.AsReadOnly();
        }

        private static int? ComputePowerNodeDegree(int? a, int? b)
        {
            if (a == null || b == null)
                return null;

            return a + b;
        }

        private static int? PickHighest(int? a, int? b)
        {
            if (a == null)
                return b;
            if (b == null)
                return b;

            return a > b ? a : b;
        }
    }
}
