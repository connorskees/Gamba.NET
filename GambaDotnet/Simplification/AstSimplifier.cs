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
    public class AstSimplifier
    {
        private AstNode root;

        private IReadOnlyDictionary<AstNode, AstClassification> classificationMapping;

        public AstSimplifier(AstNode root)
        {
            this.root = root;
            classificationMapping = AstClassifier.Classify(root);
        }

        public AstNode Simplify()
        {
            // If the whole expression is linear then we can just use SIMBA.
            if(AstClassifier.IsLinear(classificationMapping[root]))
                return FastSimba.SimplifyLinearMba(root);

            // If the expression is not linear then we try to use SIMBA on all linear subtrees.
            SimplifyLinearSubtrees();
            return root;
        }

        private void SimplifyLinearSubtrees()
        {
            // Repeatedly simplify subtrees until we reached a fixed point.
            while(true)
            {
                // Try to simplify linear subtrees. We repeat this until either there are no linear subtrees,
                // or until SIMBA cannot find any simpler results.
                bool changed = LinearSubtreeSimplifier.SimplifyLinearSubtrees(root, classificationMapping);
                if (!changed)
                    break;

                classificationMapping = AstClassifier.Classify(root);
            }
        }

    }
}
