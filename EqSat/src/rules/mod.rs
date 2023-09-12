use egg::*;

use crate::{
    classification::{AstClassification, ClassificationResult},
    const_fold::{wrap, Rewrite},
    rules::{
        bitwise_power_of_two::{is_power_of_two, BitwisePowerOfTwoFactorApplier},
        duplicate_children_mul_add::{is_const, DuplicateChildrenMulAddApplier},
        rewrite_power::{can_rewrite_power, RewritePowerApplier},
    },
};

mod bitwise_power_of_two;
mod duplicate_children_mul_add;
mod rewrite_power;

pub fn make_rules() -> Vec<Rewrite> {
    vec![
        // Or rules
        rewrite!("or-zero"; "(| ?a 0)" => "?a"),
        rewrite!("or-maxint"; "(| ?a -1)" => "-1"),
        rewrite!("or-itself"; "(| ?a ?a)" => "?a"),
        rewrite!("or-negated-itself"; "(| ?a (~ ?a))" => "-1"), // formally proved
        rewrite!("or-commutativity"; "(| ?a ?b)" => "(| ?b ?a)"), //  formally proved
        rewrite!("or-associativity"; "(| ?a (| ?b ?c))" => "(| (| ?a ?b) ?c)"), // formally proved
        // Xor rules
        rewrite!("xor-zero"; "(^ ?a 0)" => "?a"), // formally proved
        rewrite!("xor-maxint"; "(^ ?a -1)" => "(~ ?a)"), // formally proved
        rewrite!("xor-itself"; "(^ ?a ?a)" => "0"), // formally proved
        rewrite!("xor-commutativity"; "(^ ?a ?b)" => "(^ ?b ?a)"),
        rewrite!("xor-associativity"; "(^ ?a (^ ?b ?c))" => "(^ (^ ?a ?b) ?c)"), // formally proved
        // And rules
        rewrite!("and-zero"; "(& ?a 0)" => "0"),
        rewrite!("and-maxint"; "(& ?a -1)" => "?a"), // formally proved
        rewrite!("and-itself"; "(& ?a ?a)" => "?a"), // formally proved
        rewrite!("and-negated-itself"; "(& ?a (~ ?a))" => "0"), // formally proved
        rewrite!("and-commutativity"; "(& ?a ?b)" => "(& ?b ?a)"),
        rewrite!("and-associativity"; "(& ?a (& ?b ?c))" => "(& (& ?a ?b) ?c)"), // formally proved
        // Add rules
        rewrite!("add-itself"; "(+ ?a ?a)" => "(* ?a 2)"), // formally proved
        rewrite!("add-zero"; "(+ ?a 0)" => "?a"),          // formally proved
        rewrite!("add-cancellation"; "(+ ?a (* ?a -1))" => "0"), // formally proved
        rewrite!("add-commutativity"; "(+ ?a ?b)" => "(+ ?b ?a)"), // formally proved
        rewrite!("add-associativity"; "(+ ?a (+ ?b ?c))" => "(+ (+ ?a ?b) ?c)"), // formally proved
        // Mul rules
        rewrite!("mul-zero"; "(* ?a 0)" => "0"),
        rewrite!("mul-one"; "(* ?a 1)" => "?a"),
        rewrite!("mul-commutativity"; "(* ?a ?b)" => "(* ?b ?a)"), // formally proved
        rewrite!("mul-associativity"; "(* ?a (* ?b ?c))" => "(* (* ?a ?b) ?c)"), // formally proved
        rewrite!("mul-distributivity-expand-add"; "(* ?a (+ ?b ?c))" => "(+ (* ?a ?b) (* ?a ?c))"), // formally proved
        // Power rules
        rewrite!("power-zero"; "(** ?a 0)" => "1"),
        rewrite!("power-one"; "(** ?a 1)" => "?a"),
        // __check_duplicate_children
        rewrite!("expanded-add"; "(+ (* ?const ?x) ?x)" => {
            DuplicateChildrenMulAddApplier {
                const_factor : "?const",
                x_factor : "?x",
            }
        } if is_const("?const")),
        // ported rules:
        // __eliminate_nested_negations_advanced
        rewrite!("minus-twice"; "(* (* ?a -1) -1))" => "?a"), // formally proved
        rewrite!("negate-twice"; "(~ (~ ?a))" => "?a"),       // formally proved
        // __check_bitwise_negations
        // bitwise -> arith
        rewrite!("add-bitwise-negation"; "(+ (~ ?a) ?b)" => "(+ (+ (* ?a -1) -1) ?b)"), // formally proven
        rewrite!("sub-bitwise-negation"; "(+ (~ ?a) (* ?b -1))" => "(+ (+ (* ?a -1) -1) (* ?b -1))"), // formally proven
        rewrite!("mul-bitwise-negation"; "(* (~ ?a) ?b)" => "(* (+ (* ?a -1) -1) ?b)"), // formally proven at reduced bit width(but it's still correct at all bitwidths)
        rewrite!("pow-bitwise-negation"; "(** (~ ?a) ?b)" => "(** (+ (* ?a -1) -1) ?b)"),
        // arith -> bitwise
        rewrite!("and-bitwise-negation"; "(& (+ (* ?a -1) -1) ?b)" => "(& (~ ?a) ?b)"), // formally proved
        rewrite!("or-bitwise-negation"; "(| (+ (* ?a -1) -1) ?b)" => "(| (~ ?a) ?b)"), // formally proved
        rewrite!("xor-bitwise-negation"; "(^ (+ (* ?a -1) -1) ?b)" => "(^ (~ ?a) ?b)"), // formally proved
        // __check_bitwise_powers_of_two
        rewrite!("bitwise_powers_of_two: "; "(& (* ?factor1 ?x) (* ?factor2 ?y))" => { // not formally proved but most likely bug free
            BitwisePowerOfTwoFactorApplier {
                x_factor : "?factor1",
                y_factor : "?factor2",
            }
        } if (is_power_of_two("?factor1", "?factor2"))),
        // __check_beautify_constants_in_products: todo
        // __check_move_in_bitwise_negations
        rewrite!("and-move-bitwise-negation-in"; "(~ (& (~ ?a) ?b))" => "(| ?a (~ ?b))"), // formally proved
        rewrite!("or-move-bitwise-negation-in"; "(~ (| (~ ?a) ?b))" => "(& ?a (~ ?b))"), // formally proved
        rewrite!("xor-move-bitwise-negation-in"; "(~ (^ (~ ?a) ?b))" => "(^ ?a ?b)"), // formally proved
        // __check_bitwise_negations_in_excl_disjunctions
        rewrite!("xor-flip-negations"; "(^ (~ ?a) (~ ?b))" => "(^ ?a ?b)"), // formally proved
        // __check_rewrite_powers
        rewrite!("extract-constant-factor-from-power"; "(** (* ?a ?b) ?exponent)" => {
            RewritePowerApplier {
                a: "?a".parse().unwrap(),
                b: "?b".parse().unwrap(),
                exponent: "?exponent".parse().unwrap(),
            }
        } if (can_rewrite_power("?a", "?exponent"))),
        // __check_resolve_product_of_powers
        // note: they say "Moreover merge factors that occur multiple times",
        // but I'm not sure what they mean
        rewrite!("merge-power-same-base"; "(* (** ?a ?b) (** ?a ?c))" => "(** ?a (+ ?b ?c))"),
        // __check_resolve_product_of_constant_and_sum: implemented above
        // __check_factor_out_of_sum
        rewrite!("factor"; "(+ (* ?a ?b) (* ?a ?c))" => "(* ?a (+ ?b ?c))"), // formally proved
        // __check_resolve_inverse_negations_in_sum
        rewrite!("invert-add-bitwise-not-self"; "(+ ?a (~ ?a))" => "-1"), // formally proved
        // formally proved
        rewrite!("invert-mul-bitwise-not-self"; "(+ (* ?a (~ ?b)) (* ?a ?b))" => "(* ?a -1)"), // formally proved
        // __insert_fixed_in_conj: todo
        // __insert_fixed_in_disj: todo
        // __check_trivial_xor: implemented above
        // __check_xor_same_mult_by_minus_one
        // "2*(x|-x) == x^-x"
        rewrite!("xor_same_mult_by_minus_one_1"; "(* (| ?a (* ?a -1)) 2)" => "(^ ?a (* ?a -1))"),
        // "-2*(x&-x) == x^-x"
        rewrite!("xor_same_mult_by_minus_one_2"; "(* (& ?a (* ?a -1)) -2)" => "(^ ?a (* ?a -1))"),
        // __check_conj_zero_rule
        // x&-x&2*x
        rewrite!("conj_zero_rule"; "(& ?a (& (* ?a -1) (* ?a 2)))" => "0"), // formally proved
        // __check_conj_neg_xor_zero_rule
        // ~(2*x)&-(x^-x)
        rewrite!("conj_neg_xor_zero_rule"; "(& (~ (* ?a 2)) (* (^ ?a (* ?a -1)) -1))" => "0"), // formally proved
        // __check_conj_neg_xor_minus_one_rule
        // 2*x|~-(x^-x)
        rewrite!("conj_neg_xor_minus_one_rule"; "(| (* ?a 2) (~ (* (^ ?a (* ?a -1)) -1)))" => "-1"), // formally proved
        // __check_conj_negated_xor_zero_rule
        // 2*x&~(x^-x)
        rewrite!("conj_negated_xor_zero_rule"; "(& (* ?a 2) (~ (^ ?a (* ?a -1))))" => "0"), // formally proved
        // __check_conj_xor_identity_rule
        // 2*x&(x^-x)
        rewrite!("conj_xor_identity_rule"; "(& (* ?a 2) (^ ?a (* ?a -1)))" => "(* ?a 2)"), // formally proved
        // __check_disj_xor_identity_rule
        // 2*x|-(x^-x)
        rewrite!("disj_xor_identity_rule"; "(| (* ?a 2) (* -1 (^ ?a (* ?a -1))))" => "(* ?a 2)"), // formally proved
        // __check_conj_neg_conj_identity_rule
        // -x&~(x&2*x)
        rewrite!("conj_neg_conj_identity_rule_1"; "(& (* ?a -1) (~ (& ?a (* ?a 2))))" => "(* ?a -1)"), // formally proved
        // -x&~(x&-2*x)
        rewrite!("conj_neg_conj_identity_rule_2"; "(& (* ?a -1) (~ (& ?a (* ?a -2))))" => "(* ?a -1)"), // formally proved
        // -x&(~x|~(2*x))
        rewrite!("conj_neg_conj_identity_rule_3"; "(& (* ?a -1) (| (~ ?a) (~ (* ?a 2))))" => "(* ?a -1)"), // formally proved
        // -x&(~x|~(-2*x))
        rewrite!("conj_neg_conj_identity_rule_4"; "(& (* ?a -1) (| (~ ?a) (~ (* ?a -2))))" => "(* ?a -1)"), // formally proved
        // __check_disj_disj_identity_rule
        // x|-(x|-x)
        rewrite!("disj_disj_identity_rule"; "(| ?a (* (| ?a (* ?a -1)) -1))" => "?a"), // formally proved
        // __check_conj_conj_identity_rule
        // x&-(x&-x)
        rewrite!("conj_conj_identity_rule"; "(& ?a (* (& ?a (* ?a -1)) -1))" => "?a"), // formally proved
        // __check_disj_conj_identity_rule
        // -x|(~x&2*x)
        rewrite!("disj_conj_identity_rule_1"; "(| (* ?a -1) (& (~ ?a) (* ?a 2)))" => "(* ?a -1)"), // formally proved
        // -x|(~x&-2*x)
        rewrite!("disj_conj_identity_rule_2"; "(| (* ?a -1) (& (~ ?a) (* ?a -2)))" => "(* ?a -1)"), // formally proved
        // -x|~(x|~(2*x))
        rewrite!("disj_conj_identity_rule_3"; "(| (* ?a -1) (~ (| ?a (~ (* ?a 2)))))" => "(* ?a -1)"), // formally proved
        // -x|~(x|~(-2*x))
        rewrite!("disj_conj_identity_rule_4"; "(| (* ?a -1) (~ (| ?a (~ (* ?a -2)))))" => "(* ?a -1)"), // formally proved
        // __check_disj_conj_identity_rule_2
        // x|(-~x&2*~x)
        rewrite!("disj_conj_identity_rule_2_1"; "(| ?x (& (* (~ ?x) -1) (* 2 (~ ?x))))" => "?x"), // formally proved
        // x|(-~x&-2*~x)
        rewrite!("disj_conj_identity_rule_2_2"; "(| ?x (& (* (~ ?x) -1) (* (* 2 -1) (~ ?x))))" => "?x"), // formally proved
        // __check_conj_disj_identity_rule
        // x&(-~(2*x)|-~x)
        rewrite!("conj_disj_identity_rule_1"; "(& ?x (| (* (~ (* 2 ?x)) -1) (* (~ ?x) -1)))" => "?x"), // formally proved - reprove up from here
        // x&(~(2*~x)|-~x)
        rewrite!("conj_disj_identity_rule_2"; "(& ?x (| (~ (* 2 (~ ?x))) (* (~ ?x) -1)))" => "?x"), // formally proved
        // x&(~(-2*~x)|-~x)
        // Note that while GAMBA only solves this pattern for the constant '2', it is true if 'Y' is substituted with any value.
        rewrite!("conj_disj_identity_rule_3"; "(& ?x (| (~ (* (* ?y -1) (~ ?x))) (* (~ ?x) -1)))" => "?x"), // formally proved
        // __check_disj_neg_disj_identity_rule
        // x|-(-x|2*x)
        rewrite!("disj_neg_disj_identity_rule_1"; "(| ?x (* (| (* ?x -1) (* ?y ?x)) -1))" => "?x"), // formally proved
        // x|-(-x|-2*x)
        rewrite!("disj_neg_disj_identity_rule_2"; "(| ?x (* (| (* ?x -1) (* (* ?y -1) ?x)) -1))" => "?x"), // formally proved
        // __check_disj_sub_disj_identity_rule
        // x|(x|y)-y
        rewrite!("disj_sub_disj_identity_rule_1"; "(| ?x (+ (| ?x ?y) (* ?y -1)))" => "?x"), // formally proved
        // __check_disj_sub_disj_identity_rule
        // x|x-(x&y)
        rewrite!("disj_sub_disj_identity_rule_2"; "(| ?x (+ ?x (* (& ?x ?y) -1)))" => "?x"), // formally proved
        // __check_conj_add_conj_identity_rule
        // x&x+(~x&y)
        rewrite!("conj_add_conj_identity_rule"; "(& ?x (+ ?x (& (~ ?x) ?y)))" => "?x"), // formally proved
        // __check_disj_disj_conj_rule
        // x|-(-y|(x&y))
        rewrite!("disj_disj_conj_rule"; "(| ?x (* (| (* ?y -1) (& ?x ?y)) -1))" => "(| ?x ?y)"), // formally proved
        // __check_conj_conj_disj_rule
        // x&-(-y&(x|y))
        rewrite!("conj_conj_disj_rule"; "(& ?x (* (& (* ?y -1) (| ?x ?y)) -1))" => "(& ?x ?y)"),
        // __check_disj_disj_conj_rule_2
        // -(-x|x&y&z)|x&y
        rewrite!("disj_disj_conj_rule_2"; "(| (* (| (* ?x -1) (& (& ?x ?y) ?z)) -1) (& ?x ?y))" => "?x"),
        // Additional rules:
        rewrite!("mba-1"; "(+ ?d (* -1 (& ?d ?a)))" => "(& (~ ?a) ?d)"),
        rewrite!("mba-2"; "(+ (* -1 (& ?d ?a)) ?d)" => "(& (~ ?a) ?d)"),
        rewrite!("mba-3"; "(+ (+ ?a (* 2 -1)) (* (* 2 -1) ?d))" => "(+ (+ (* 2 -1) ?a) (* (* 2 ?d) -1))"),
        rewrite!("mba-4"; "(+ (| ?d ?a) (* -1 (& ?a (~ ?d))))" => "?d"),
        rewrite!("mba-5"; "(+ (* -1 (& ?a (~ ?d))) (| ?d ?a))" => "?d"),
        rewrite!("mba-6"; "(+ (& ?a (~ ?d)) (& ?d ?a))" => "?a"),
        rewrite!("mba-9"; "(+ (+ ?a (* -2 ?d)) (* 2 (& (~ ?a) (* 2 ?d))))" => "(^ ?a (* 2 ?d))"),
        rewrite!("mba-10"; "(~ (* ?x ?y))" => "(+ (* (~ ?x) ?y) (+ ?y -1))"),
        rewrite!("mba-12"; "(~ (+ ?x (* ?y -1)))" => "(+ (~ ?x) (* (+ (~ ?y) 1) -1))"),
        rewrite!("mba-13"; "(~ (& ?x ?y))" => "(| (~ ?x) (~ ?y))"),
        rewrite!("mba-14"; "(~ (^ ?x ?y))" => "(| (& ?x ?y) (~ (| ?x ?y)))"),
        rewrite!("mba-15"; "(~ (| ?x ?y))" => "(& (~ ?x) (~ ?y))"),
        rewrite!("mba-19"; "(~ (& (~ ?a) (~ ?b)))" => "(| ?b ?a)"),
        rewrite!("mba-22"; "(& (~ (& (~ ?a) (~ ?b))) (~ (& ?a ?b)))" => "(^ ?b ?a)"),
        // my new rules
        rewrite!("not-to-arith"; "(~ ?a)" => "(+ (* ?a -1) -1)"),
        rewrite!("arith-to-not"; "(+ (* ?a -1) -1)" => "(~ ?a)"),
        rewrite!("distribute"; "(* ?a (+ ?b ?c))" => "(+ (* ?a ?b) (* ?a ?c))"),
        rewrite!("new-0"; "(+ ?x (& ?y (~ ?x)))" => "(| ?x ?y)"),
        rewrite!("new-1"; "(+ (^ ?x ?y) (* -1 ?x))" => "(+ ?y (* -2 (& ?x ?y)))"),
        rewrite!("new-2"; "(+ (^ ?x ?y) (* -1 (| ?x ?y)))" => "(* -1 (& ?x ?y))"),
        rewrite!("new-3"; "(+ (| ?a ?b) (* ?a -1))" => "(& (~ ?a) ?b)"),
        rewrite!("new-4"; "(+ (* ?a -1) ?b)" => "(* -1 (+ ?a (* ?b -1)))"),
        rewrite!("new-5"; "(* ?a 2)" => "(+ ?a ?a)"),
        rewrite!("new-6"; "(+ (& ?a ?b) (^ ?a ?b))" => "(| ?a ?b)"),
        rewrite!("new-7"; "(+ (& ?a ?b) (| ?a ?b))" => "(+ ?a ?b)"),
        rewrite!("new-8"; "(+ (| ?a ?b) (* -1 (^ ?a ?b)))" => "(& ?a ?b)"),
        rewrite!("new-9"; "(+ (| ?a ?b) (* -1 (& ?a ?b)))" => "(^ ?a ?b)"),
        rewrite!("new-10"; "(+ (~ (& ?a ?b)) ?a)" => "(+ -1 (& ?a (~ ?b)))"),
        rewrite!("new-11"; "(| (& ?a ?b) ?a)" => "?a"),
        rewrite!("new-12"; "(| (| ?a ?b) ?a)" => "(| ?a ?b)"),
        rewrite!("new-13"; "(| (^ ?a ?b) ?a)" => "(| ?a ?b)"),
        rewrite!("new-14"; "(+ (^ ?a ?b) ?a)" => "(+ ?b (* 2 (& ?a (~ ?b))))"),
        rewrite!("new-15"; "(+ (| ?a ?b) ?a)" => "(+ (~ (| ?a (~ ?b))) (* 2 ?a))"),
        rewrite!("new-16"; "(& (^ ?a ?b) ?a)" => "(& ?a (~ ?b))"),
        rewrite!("new-17"; "(+ (* ?a ?a) (* ?a (~ ?a)))" => "(* ?a -1)"),
        rewrite!("new-18"; "(+ (* ?a ?b) 1)" => "(* (~ (* ?a ?b)) -1)"),
        rewrite!("new-19"; "(+ (* ?a ?b) -1)" => "(~ (* (* ?a -1) ?b))"),
        rewrite!("new-20"; "(+ (* ?a ?b) ?a)" => "(* (* -1 ?a) (~ ?b))"),
        rewrite!("new-21"; "(& ?a (~ ?b))" => "(+ ?a (* (& ?a ?b) -1))"),
    ]
}

pub fn read_constant(data: &Option<ClassificationResult>) -> Option<i128> {
    match data.as_ref()?.classification {
        AstClassification::Constant { value } => Some(wrap(value)),
        _ => None,
    }
}
