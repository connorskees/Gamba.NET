using Gamba.Ast;
using Gamba.Interop;
using Gamba.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Simplification
{
    public static class LinearSubtreeSimplifier
    {
        public static bool SimplifyLinearSubtrees(AstNode node, IReadOnlyDictionary<AstNode, AstClassification> classificationMapping)
        {
            bool changed = false;

            // Otherwise the expression is nonlinear and we must recurse into simplifying child nodes.
            for(int i = 0; i < node.Children.Count; i++)
            {
                // If the child is linear then we simplify it and update the current operand.
                var child = node.Children[i];
                if (AstClassifier.IsLinear(classificationMapping[child]))
                {
                    // Simplify the expression.
                    var simplified = FastSimba.SimplifyLinearMba(child);

                    // Replace the child expression only if SIMBA returns a shorter and syntactically different AST.
                    if (child.GetHashCode() != simplified.GetHashCode() && simplified.ToString() != child.ToString())
                    {
                        node.SetOperand(i, simplified);
                        changed = true;
                    }
                }

                // If the child is nonlinear then we recurse into simplifying child nodes.
                else
                {
                    SimplifyLinearSubtrees(child, classificationMapping);
                }
            }

            return changed;
        }
    }
}
