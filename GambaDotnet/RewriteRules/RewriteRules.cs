using Gamba.Prototyping.Transpiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GambaDotnet.RewriteRules
{
    public class RuleMapping
    {
        Dictionary<string, (string, string?)> rules = new();

        public void Add(string input, string output) => rules.Add(input, (output, null));

        public void Add(string input, string output, string when) => rules.Add(input, (output, when));
    }

    public class RewriteRules
    {
        private RuleMapping rules = new();

        public RewriteRules()
        {
            InspectConstants();

            RefineStep2();
        }

        public void InspectConstants()
        {
            OrRules();
            XorRules();
            AndRules();
            AddRules();
            MulRules();
            PowerRules();
            NegationRules();
        }

        public void OrRules()
        {
            rules.Add("x|0", "x");
            // -1 has all bits set so this is always going to be
            rules.Add("x|-1", "-1");
            // Anything ORed by itself is just itself
            rules.Add("x|x", "x");
            // Commutativity
            rules.Add("x|y", "y|x");
        }

        public void XorRules()
        {
            // Anything xored with 0 is itself
            rules.Add("x^0", "x");
            // This is equivalent to negation.
            rules.Add("x^-1", "~x");
            // This is equivalent to zero
            rules.Add("x^x", "0");
            // Commutativity
            rules.Add("x^y", "y^x");
        }

        public void AndRules()
        {
            // Anything anded with itself is just itself
            rules.Add("a&a", "a");
            // Anything anded with 0 is zero
            rules.Add("a&0", "0");
            // Anything anded with -1 is just the non constant.
            rules.Add("a&-1", "a");
            // Commutativity
            rules.Add("a&b", "b&a");

        }

        public void AddRules()
        {
            rules.Add("a+a", "a*2");
            rules.Add("a+0", "a");
            // Commutativity
            rules.Add("a+b", "b+a");
        }

        public void MulRules()
        {
            rules.Add("a*0", "0");
            rules.Add("a*1", "a");
            rules.Add("(a*-1) * -1", "a");
            rules.Add("a*a", "a**2");
            // Commutativity
            rules.Add("a*b", "b*a");
        }

        public void PowerRules()
        {
            rules.Add("a*0", "1");
            rules.Add("a*1", "a");
        }

        public void NegationRules()
        {
            // TODO: Commutativity.
            rules.Add("~~a", "a");
        }

        public void RefineStep2()
        {
            // Assuming that this node is a bitwise negation and its child is a product,
            // replace this node using the formula ~x = -x - 1.
            rules.Add("~(x*y)", "(-x + -1) * (-y + -1)");

            // Assuming that this node is a bitwise negation and its child is a sum,
            // replace this node using the formula ~x = -(x + 1).
            rules.Add("~(x+y)", "(-x + -1) * (-y + -1)");

            // If a child of this node is a conjunction or inclusive disjunction of two
            // children such that one equals the other one after multiplication by -1,
            // and if this node is a product with a constant divisible by 2, try to
            // apply the rules "2*(x|-x) == x^-x" and "-2*(x&-x) == x^-x".
            rules.Add("y*(x|-x)", "(y / 2) * (x^-x)", "y is const && y % 2 == 0");
            rules.Add("-2*(x&-x)", "(y / 2) * (x^-x)", "y is const && y % 2 == 0");
        }

    }
}
