using Gamba.Ast;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Simplification
{
    public class PolynomialFactorizer
    {
        public record struct Factor(int degree);

        public bool TryFactorizePolynomial(AstNode node)
        {
            // Get the factors of child A.
            var aFactorsAndDegrees = new Dictionary<AstNode, int>();
            GetFactors(node.Children[0], aFactorsAndDegrees);

            // Get the factors of child B.
            var bFactorsAndDegrees = new Dictionary<AstNode, int>();
            GetFactors(node.Children[1], bFactorsAndDegrees);

            // Compute the set of shared factors among child A and B.
            // TODO: Remove bitwise factors. We don't want to factorize out bitwise expressions
            // because it creates nonlinear expressions.
            var commonFactors = aFactorsAndDegrees.Keys.Intersect(bFactorsAndDegrees.Keys).ToHashSet();
            if (!commonFactors.Any())
                return false;

            // Since the input node is multiplication.
            AstNode factoriziedMultiplier = null;
            AstNode factorizedMultipliee = null;

            /*
            foreach((AstNode factor, int degree) in aFactorsAndDegrees)
            {
                // If the factor is common between A and B, this gets the degree in B.
                // Otherwise it returns 0.
                bFactorsAndDegrees.TryGetValue(factor, out int bDegree);

                // Calculate the degree after factorization.
                // So if you have (a^3)(a^1), this is two.
                // If you had (a^2)(a^5), this is three.
                var newDegree = Math.Abs(degree - bDegree);


            }
            */

            // Compute the LHS of the factorized node.
            AstNode factorizedLhs = null;
            foreach(var commonFactor in commonFactors)
            {
                // Compute the maximum degree that we may factor out.
                // E.g. if you have (a^3)(a^1), this returns 1.
                var lowestDegree = Math.Min(aFactorsAndDegrees[commonFactor], bFactorsAndDegrees[commonFactor]);
                aFactorsAndDegrees[commonFactor] -= lowestDegree;
                bFactorsAndDegrees[commonFactor] -= lowestDegree;

                // This ideally should not happen.
                if (lowestDegree == 0)
                    throw new InvalidOperationException();

                var multiplier = lowestDegree == 1 ? commonFactor : new PowerNode(commonFactor, lowestDegree);
                factorizedLhs = factorizedLhs == null ? multiplier : new MulNode(factorizedLhs, multiplier);
            }

            // Copy the factors and degrees of A while removing elements with degrees of 0.
            var rhsFactorsAndDegrees = aFactorsAndDegrees.Where(x => x.Value != 0).ToDictionary(x => x.Key, x => x.Value);
            // Combine the list of A factors and B factors into one dictionary.
            foreach(var factor in bFactorsAndDegrees)
            {
                if (factor.Value == 0)
                    continue;
                rhsFactorsAndDegrees.TryAdd(factor.Key, 0);
                rhsFactorsAndDegrees[factor.Key] += factor.Value;
            }

            AstNode factorizedRhs = null;
            foreach((AstNode factor, int degree) in rhsFactorsAndDegrees) 
            {
                var multiplier = degree == 1 ? factor : new PowerNode(factor, degree);
                factorizedRhs = factorizedRhs == null ? multiplier : new MulNode(factorizedRhs, multiplier);
            }

            var result = new MulNode(factorizedLhs, factorizedRhs);
            Console.WriteLine(result);
            return false;
        }

        public void GetFactors(AstNode node, Dictionary<AstNode, int> factorsAndDegrees)
        {
            // If the node is a multiplication node then we should
            // recursively grab all items it's multiplied by.
            if (node is MulNode mul)
            {
                GetFactors(node.Children[0], factorsAndDegrees);
                GetFactors(node.Children[1], factorsAndDegrees);

                // Add child a to the set of factors. 
                var a = node.Children[0];
                factorsAndDegrees.TryAdd(a, 0);
                factorsAndDegrees[a] += 1;

                // Add child b to the set of factors
                var b = node.Children[1];
                factorsAndDegrees.TryAdd(b, 0);
                factorsAndDegrees[b] += 1;
            }
        }
    }
}
