using Gamba.Ast;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Utility
{
    public enum AstClassification
    {
        // Bitwise expression(e.g. a & b)
        Bitwise,
        // Linear expression(e.g. a + b, x * 3434) but never (a * b, x & 3453443)
        Linear,
        // Anything with that has a polynomial degree greater than one OR contains arithmetic/nonlinear/constant expressions inside
        // of bitwise expressions.
        Nonlinear,
        // Mixed arithmetic and bitwise expressions. These are technically linear.
        // But, they have an important distinction from linear expressions because 
        // mixed expressions cannot be used inside of bitwise expressions.
        // If a mixed expression is used inside of a bitwise operand, the root becomes nonlinear.
        Mixed,
    }

    public class AstClassifier
    {
        public static IReadOnlyDictionary<AstNode, AstClassification> Classify(AstNode src)
        {
            var mapping = new Dictionary<AstNode, AstClassification>();
            Classify(src, mapping, new HashSet<AstNode>());
            return mapping.AsReadOnly();
        }

        private static void Classify(AstNode node, Dictionary<AstNode, AstClassification> mapping, HashSet<AstNode> constMapping)
        {
            var op1 = () => node.Children[0];
            var op2 = () => node.Children[1];   

            foreach (var operand in node.Children)
            {
                Classify(operand, mapping, constMapping);
            }

            // Constant operations are always linear.
            if (node.Children.Any() && node.Children.All(x => constMapping.Contains(x)))
            {
                constMapping.Add(node);
                mapping[node] = AstClassification.Linear;
                return;
            }

            switch (node)
            {
                // Treat constant and varnodes as linear expressions.
                case ConstNode:
                    constMapping.Add(node);
                    mapping[node] = AstClassification.Linear;
                    break;
                case VarNode:
                    mapping[node] = AstClassification.Linear;
                    break;
                // Any power raise other than (1) will make the expression nonlinear.
                // TODO: Handle negative exponents?
                case PowerNode:
                    mapping[node] = AstClassification.Nonlinear;
                    break;
                case MulNode:
                    // If neither operand is a constant, then the expression must be nonlinear.
                    var constMul = OneOf(op1(), op2(), (x) => constMapping.Contains(x) ? x : null);
                    if (constMul == null)
                    {
                        mapping[node] = AstClassification.Nonlinear;
                        break;
                    }

                    var other = node.Children.Single(x => x != constMul);
                    var otherKind = mapping[other];
                    // const * bitwise = mixed expression
                    if (otherKind == AstClassification.Bitwise)
                        mapping[node] = AstClassification.Mixed;
                    // const * linear = linear
                    else if (otherKind == AstClassification.Linear)
                        mapping[node] = AstClassification.Linear;
                    // const * nonlinear = nonlinear
                    else if (otherKind == AstClassification.Nonlinear)
                        mapping[node] = AstClassification.Nonlinear;
                    // const * mixed(bitwise and arithmetic) = mixed.
                    else
                        mapping[node] = AstClassification.Mixed;
                    break;
                case AddNode:
                    if (node.Children.Any(x => mapping[x] == AstClassification.Nonlinear))
                        mapping[node] = AstClassification.Nonlinear;
                    else if (node.Children.Any(x => mapping[x] == AstClassification.Mixed || mapping[x] == AstClassification.Bitwise))
                        mapping[node] = AstClassification.Mixed;
                    else if (node.Children.Any(x => mapping[x] == AstClassification.Linear))
                        mapping[node] = node.Children.Any(x => mapping[x] == AstClassification.Bitwise) ? AstClassification.Mixed : AstClassification.Linear;
                    else
                        mapping[node] = AstClassification.Bitwise;
                    break;
                case AndNode:
                case OrNode:
                case XorNode:
                case NegNode:
                    bool containsConstantOrArithmetic = node.Children.Any(x => constMapping.Contains(x) || mapping[x] == AstClassification.Linear && x is not VarNode);
                    bool containsMixedOrNonLinear = node.Children.Any(x => mapping[x] == AstClassification.Mixed || mapping[x] == AstClassification.Nonlinear);

                    // If a bitwise expression contains nontrivial constants or arithmetic operations, it is nonlinear.
                    // Alternatively if the bitwise expression has any mixed or nonlinear operations, then it is also nonlinear.
                    if (containsConstantOrArithmetic || containsMixedOrNonLinear)
                        mapping[node] = AstClassification.Nonlinear;
                    else if (node.Children.Any(x => (mapping[x] == AstClassification.Linear || mapping[x] == AstClassification.Mixed) && x is not VarNode))
                        mapping[node] = AstClassification.Mixed;
                    else
                        mapping[node] = AstClassification.Bitwise;

                    break;
                default:
                    throw new InvalidOperationException($"Cannot classify ast kind: {node.GetType().FullName}");
            }
        }

        private static AstNode OneOf(AstNode a, AstNode b, Func<AstNode, AstNode> predicate)
        {
            var resultA = predicate(a);
            if (resultA != null)
                return resultA;

            var resultB = predicate(b);
            if (resultB != null)
                return resultB;

            return null;
        }

        public static bool IsLinear(AstClassification classification)
        {
            return classification switch
            {
                AstClassification.Bitwise => true,
                AstClassification.Linear => true,
                AstClassification.Nonlinear => false,
                AstClassification.Mixed => true,
                _ => false,
            };
        }
    }
}
