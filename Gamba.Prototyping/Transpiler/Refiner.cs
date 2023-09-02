using Gamba.Prototyping.Transpiled;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiler
{
    public static class Refiner
    {
        public static void InstrumentedRefine(Node node)
        {
            for(int i = 0; i < 10; i++)
            {
                RefineStep1(node);
                RefineStep2(node, null);
            }
        }

        public static void RefineStep1(Node node)
        {
            foreach (var c in node.children)
                RefineStep1(c);

            // Refine step 1
          //  if (node.ToString() == "-1073741823")
             //   Debugger.Break();
            Step(node, "__inspect_constants");
            Step(node, "__flatten");
            Step(node, "__check_duplicate_children");
            //node.__check_duplicate_children();
            Step(node, "__resolve_inverse_nodes");
            Step(node, "__remove_trivial_nodes");
        }

        public static void RefineStep2(Node node, Node parent)
        {
            foreach (var c in node.children)
                RefineStep2(c, node);

            Step(node, "__eliminate_nested_negations_advanced");
            StepWithParent(node, parent, "__check_bitwise_negations");
            Step(node, "__check_bitwise_powers_of_two");
            Step(node, "__check_beautify_constants_in_products");
            Step(node, "__check_move_in_bitwise_negations");
            Step(node, "__check_bitwise_negations_in_excl_disjunctions");
            StepWithParent(node, parent, "__check_rewrite_powers");
            Step(node, "__check_resolve_product_of_powers");
            Step(node, "__check_resolve_product_of_constant_and_sum");
            Step(node, "__check_factor_out_of_sum");
            Step(node, "__check_resolve_inverse_negations_in_sum");
            Step(node, "__insert_fixed_in_conj");
            Step(node, "__insert_fixed_in_disj");
            Step(node, "__check_trivial_xor");
            Step(node, "__check_xor_same_mult_by_minus_one");
            Step(node, "__check_conj_zero_rule");
            Step(node, "__check_conj_neg_xor_zero_rule");
            Step(node, "__check_conj_neg_xor_minus_one_rule");
            Step(node, "__check_conj_negated_xor_zero_rule");
            Step(node, "__check_conj_xor_identity_rule");
            Step(node, "__check_disj_xor_identity_rule");
            Step(node, "__check_conj_neg_conj_identity_rule");
            Step(node, "__check_disj_disj_identity_rule");
            Step(node, "__check_conj_conj_identity_rule");
            Step(node, "__check_disj_conj_identity_rule");
            Step(node, "__check_disj_conj_identity_rule_2");
            Step(node, "__check_conj_disj_identity_rule");
            Step(node, "__check_disj_neg_disj_identity_rule");
            Step(node, "__check_disj_sub_disj_identity_rule");
            Step(node, "__check_disj_sub_conj_identity_rule");
            Step(node, "__check_conj_add_conj_identity_rule");
            Step(node, "__check_disj_disj_conj_rule");
            Step(node, "__check_conj_conj_disj_rule");
            Step(node, "__check_disj_disj_conj_rule_2");
        }

        public static void Step(Node node, string methodName)
        {
            var method = node.GetType().GetMethod(methodName);
            var before = node.ToString();
            method.Invoke(node, new object[] { });
            var after = node.ToString();

            if (before != after)
            {
               Console.WriteLine(methodName);
              Console.WriteLine($"    Before {methodName}: {before}");
              Console.WriteLine($"    After {methodName} : {after}");
            }
        }

        public static void StepWithParent(Node node, Node parent, string methodName)
        {
            var method = node.GetType().GetMethod(methodName);
            var before = node.ToString();
            method.Invoke(node, new object[] { parent });
            var after = node.ToString();

            if (before != after)
            {
                Console.WriteLine(methodName);
                Console.WriteLine($"    Before: {before}");
                Console.WriteLine($"    After: {after}");
            }
        }
    }
}
