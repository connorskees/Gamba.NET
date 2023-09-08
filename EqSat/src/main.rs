use std::time::{Duration, Instant};

use egg::*;

type Cost = i64;

pub type EEGraph = egg::EGraph<Expr, ConstantFold>;
pub type Rewrite = egg::Rewrite<Expr, ConstantFold>;

define_language! {
    pub enum Expr {
        // arithmetic operations
        "+" = Add([Id; 2]),        // (+ a b)
        "*" = Mul([Id; 2]),        // (* a b)
        "**" = Pow([Id; 2]),       // (** a b)
        // bitwise operations
        "&" = And([Id; 2]),        // (& a b)
        "|" = Or([Id; 2]),         // (| a b)
        "^" = Xor([Id; 2]),        // (^ a b)
        "~" = Neg([Id; 1]),        // (~ a)

        // Values:
        Constant(i64),             // (int)
        Symbol(Symbol),            // (x)
    }
}

#[derive(Default)]
struct BitwiseAnalysis;

#[derive(Debug)]
struct BitwisePowerOfTwoFactorApplier {
    x_factor: &'static str,
    y_factor: &'static str,
}

#[derive(Debug)]
pub struct RewritePowerApplier {
    a: Var,
    b: Var,
    exponent: Var,
}

#[derive(Debug)]
struct DuplicateChildrenMulAddApplier {
    const_factor: &'static str,
    x_factor: &'static str,
}

impl Expr {
    pub fn num(&self) -> Option<i64> {
        match self {
            Expr::Constant(n) => Some(*n),
            _ => None,
        }
    }
}

#[derive(Debug, Clone, Copy)]
pub enum AstClassification {
    Unknown,
    Constant { value: i64 },
    Bitwise,
    Linear { is_variable: bool },
    Nonlinear,
    Mixed,
}

fn try_fold_constant(egraph: &EEGraph, enode: &Expr) -> Option<(i64, PatternAst<Expr>)> {
    let x = |i: &Id| {
        egraph[*i].data.as_ref().map(|c| match c.0 {
            AstClassification::Constant { value } => Some(value),
            _ => None,
        })
    };

    Some(match enode {
        Expr::Constant(c) => {
            let msg = c.to_string().parse().unwrap();
            (*c, msg)
        }
        Expr::Add([a, b]) => {
            let val = x(a)??.wrapping_add(x(b)??);
            (val, val.to_string().parse().unwrap())
        }
        Expr::Mul([a, b]) => {
            let val = x(a)??.wrapping_mul(x(b)??);
            (val, val.to_string().parse().unwrap())
        }
        Expr::Pow([a, b]) => {
            let val = x(a)??.pow((x(b)??).try_into().unwrap());
            (val, val.to_string().parse().unwrap())
        }
        Expr::And([a, b]) => {
            let val = x(a)?? & x(b)??;
            (val, val.to_string().parse().unwrap())
        }
        Expr::Or([a, b]) => {
            let val = x(a)?? | x(b)??;
            (val, val.to_string().parse().unwrap())
        }
        Expr::Xor([a, b]) => {
            let val = x(a)?? ^ x(b)??;
            (val, val.to_string().parse().unwrap())
        }
        Expr::Neg([a]) => {
            let val = !x(a)??;
            (val, val.to_string().parse().unwrap())
        }
        Expr::Symbol(_) => return None,
    })
}

fn classify(
    egraph: &EEGraph,
    enode: &Expr,
) -> Option<(AstClassification, Option<PatternAst<Expr>>)> {
    let op = |i: &Id| egraph[*i].data.as_ref().map(|c| c.0);

    // If we have any operation that may be folded into a constant(e.g. x + 0, x ** 0), then do it.
    if let Some(const_folded) = try_fold_constant(egraph, enode) {
        return Some((
            AstClassification::Constant {
                value: (const_folded.0),
            },
            Some(const_folded.1),
        ));
    }

    // Otherwise const folding has failed. Now we need to classify it.
    let result: AstClassification = match enode {
        Expr::Constant(def) => AstClassification::Constant { value: *def },
        Expr::Symbol(..) => AstClassification::Linear { is_variable: true },
        Expr::Pow(..) => AstClassification::Nonlinear,
        Expr::Mul([a, b]) => {
            // At this point constant propagation has handled all cases where two constant children are used.
            // So now we only have to handle the other cases. First we start by checking the classification of the other(non constant) child.
            let other = get_non_constant_child_classification(op(a)?, op(b)?);

            let result = match other {
                Some(AstClassification::Unknown) => unreachable!("Hopefully this shouldn't happen"),
                Some(AstClassification::Constant { .. }) => unreachable!("Constant propagation should have already handled multiplication of two constants!"),
                // const * bitwise = mixed expression
                Some(AstClassification::Bitwise) => AstClassification::Mixed,
                // const * linear = linear
                Some(AstClassification::Linear { .. }) => AstClassification::Linear { is_variable: false },
                // const * nonlinear = nonlinear
                Some(AstClassification::Nonlinear) => AstClassification::Nonlinear,
                // const * mixed(bitwise and arithmetic) = mixed
                Some(AstClassification::Mixed) => AstClassification::Mixed,
                // If neither operand is a constant then the expression is not linear.
                None => AstClassification::Nonlinear,
            };

            return Some((result, None));
        }

        Expr::Add([a, b]) => {
            // Adding any operand (A) to any non linear operand (B) is always non linear.
            let children = [op(a)?, op(b)?];
            if children
                .into_iter()
                .any(|x| matches!(x, AstClassification::Nonlinear))
            {
                return Some((AstClassification::Nonlinear, None));
            };

            // At this point we've established (above^) that there are no nonlinear children.
            // This leaves potentially constant, linear, bitwise, and mixed expressions left.
            // So now we check if either operand is mixed(bitwise + arithmetic) or bitwise.
            // In both cases, adding anything to a mixed or bitwise expression will be considered a mixed expression.
            if children
                .into_iter()
                .any(|x| matches!(x, AstClassification::Mixed | AstClassification::Bitwise))
            {
                return Some((AstClassification::Mixed, None));
            };

            // Now an expression is either a constant or a linear child.
            // If any child is linear then we consider this to be a linear arithmetic expression.
            // Note that constant folding has already eliminated addition of constants.
            if children.into_iter().any(|x| match x {
                AstClassification::Linear { .. } => true,
                AstClassification::Bitwise => false,
                _ => false,
            }) {
                return Some((
                    AstClassification::Linear {
                        is_variable: (false),
                    },
                    None,
                ));
            };

            // This should never happen?
            unreachable!()
        }
        Expr::Neg([a]) => classify_bitwise(op(a)?, None),
        Expr::And([a, b]) => classify_bitwise(op(a)?, Some(op(b)?)),
        Expr::Or([a, b]) => classify_bitwise(op(a)?, Some(op(b)?)),
        Expr::Xor([a, b]) => classify_bitwise(op(a)?, Some(op(b)?)),
    };

    Some((result, None))
}

fn classify_bitwise(a: AstClassification, b: Option<AstClassification>) -> AstClassification {
    // TODO: Throw if we see negation with a constant, that should be fixed.
    let children = if let Some(maybe_b) = b {
        vec![a, maybe_b]
    } else {
        vec![a]
    };

    // Check if the expression contains constants or arithmetic expressions.
    let contains_constant_or_arithmetic = children.iter().any(|x| match x {
        AstClassification::Constant { .. } => true,
        // We only want to match linear arithmetic expressions - variables are excluded here.
        AstClassification::Linear { is_variable } => !*is_variable,
        _ => false,
    });

    // Check if the expression contains constants or arithmetic expressions.
    let contains_mixed_or_non_linear = children
        .iter()
        .any(|x| matches!(x, AstClassification::Mixed | AstClassification::Nonlinear));

    // Bitwise expressions are considered to be nonlinear if they contain constants,
    // arithmetic(linear) expressions, or non linear subexpressions.
    if contains_constant_or_arithmetic || contains_mixed_or_non_linear {
        return AstClassification::Nonlinear;
    } else if children.iter().any(|x: &AstClassification| match x {
        AstClassification::Linear { is_variable } => !*is_variable,
        AstClassification::Mixed => true,
        _ => false,
    }) {
        return AstClassification::Mixed;
    }

    // If none of the children are nonlinear or arithmetic then this is a pure
    // bitwise expression.
    AstClassification::Bitwise
}

// If either one of the children is a constant, return the other one.
fn get_non_constant_child_classification(
    a: AstClassification,
    b: AstClassification,
) -> Option<AstClassification> {
    let mut const_child: Option<AstClassification> = None;
    let mut other_child: Option<AstClassification> = None;
    if matches!(a, AstClassification::Constant { .. }) {
        const_child = Some(a);
    } else {
        other_child = Some(a);
    }

    if matches!(b, AstClassification::Constant { .. }) {
        const_child = Some(b);
    } else {
        other_child = Some(b);
    }

    const_child?;

    other_child
}

fn get_cost(egraph: &EEGraph, enode: &Expr) -> Cost {
    let cost = |i: &Id| egraph[*i].data.as_ref().unwrap().2;
    match enode {
        Expr::Add([a, b]) | Expr::Mul([a, b]) => cost(a) + cost(b),
        Expr::Pow([a, b]) | Expr::And([a, b]) | Expr::Or([a, b]) | Expr::Xor([a, b]) => {
            cost(a) + cost(b) + 1
        }
        Expr::Neg([a]) => cost(a) + 1,
        Expr::Constant(_) | Expr::Symbol(_) => 1,
    }
}

fn print(egraph: &EEGraph, expr: &Expr) {
    let cost_func = EGraphCostFn { egraph };

    let extractor = Extractor::new(egraph, cost_func);

    match expr {
        Expr::Add([a, b]) => {
            println!(
                "extracted:(+ {}  {})",
                extractor.find_best(*a).1,
                extractor.find_best(*b).1
            );
        }
        Expr::Mul([a, b]) => {
            println!(
                "extracted:(* {}  {})",
                extractor.find_best(*a).1,
                extractor.find_best(*b).1
            );
        }
        Expr::Pow([a, b]) => {
            println!(
                "extracted:(** {}  {})",
                extractor.find_best(*a).1,
                extractor.find_best(*b).1
            );
        }
        Expr::And([a, b]) => {
            println!(
                "extracted:(& {}  {})",
                extractor.find_best(*a).1,
                extractor.find_best(*b).1
            );
        }
        Expr::Or([a, b]) => {
            println!(
                "extracted:(| {}  {})",
                extractor.find_best(*a).1,
                extractor.find_best(*b).1
            );
        }
        Expr::Xor([a, b]) => {
            println!(
                "extracted:(^ {}  {})",
                extractor.find_best(*a).1,
                extractor.find_best(*b).1
            );
        }
        Expr::Neg([a]) => {
            println!("extracted:(~ {})", extractor.find_best(*a).1);
        }
        Expr::Constant(_) => todo!(),
        Expr::Symbol(_) => todo!(),
    }
}

#[derive(Default)]
pub struct ConstantFold;
impl Analysis<Expr> for ConstantFold {
    type Data = Option<(AstClassification, Option<PatternAst<Expr>>, Cost)>;

    fn make(egraph: &EEGraph, enode: &Expr) -> Self::Data {
        let classification = classify(egraph, enode).unwrap();

        let cost = get_cost(egraph, enode);

        // If we classified the AST and returned a new PatternAst<Expr>, that means
        // constant folding succeeded. So now we return the newly detected
        // classification and the pattern ast
        if classification.1.is_some() {
            return Some((classification.0, classification.1, cost));
        }

        if cost > 30000 {
            match classification.0 {
                AstClassification::Unknown => panic!(),
                AstClassification::Constant { .. } => (),
                AstClassification::Bitwise => match enode {
                    Expr::Add(..)
                    | Expr::Mul(_)
                    | Expr::Pow(_)
                    | Expr::And(_)
                    | Expr::Or(_)
                    | Expr::Xor(_)
                    | Expr::Neg(_) => print(egraph, enode),
                    Expr::Constant(_) | Expr::Symbol(_) => (),
                },
                AstClassification::Linear { .. } => match enode {
                    Expr::Add(..)
                    | Expr::Mul(_)
                    | Expr::Pow(_)
                    | Expr::And(_)
                    | Expr::Or(_)
                    | Expr::Xor(_)
                    | Expr::Neg(_) => print(egraph, enode),
                    Expr::Constant(_) | Expr::Symbol(_) => (),
                },
                AstClassification::Nonlinear => {}
                AstClassification::Mixed => {
                    match enode {
                        Expr::Add(..)
                        | Expr::Mul(_)
                        | Expr::Pow(_)
                        | Expr::And(_)
                        | Expr::Or(_)
                        | Expr::Xor(_)
                        | Expr::Neg(_) => print(egraph, enode),
                        Expr::Constant(_) | Expr::Symbol(_) => (),
                    }

                    match enode {
                        Expr::Add(..) => print(egraph, enode),
                        Expr::Mul(_) => print(egraph, enode),
                        Expr::Pow(_) => print(egraph, enode),
                        Expr::And(_) => print(egraph, enode),
                        Expr::Or(_) => print(egraph, enode),
                        Expr::Xor(_) => print(egraph, enode),
                        Expr::Neg(_) => print(egraph, enode),
                        Expr::Constant(_) | Expr::Symbol(_) => (),
                    }
                }
            }
        }

        // Otherwise we've classified the AST but there was no constant folding
        // to be performed.
        Some((classification.0, classification.1, get_cost(egraph, enode)))
    }

    fn merge(&mut self, maybe_a: &mut Self::Data, maybe_b: Self::Data) -> DidMerge {
        merge_option(maybe_a, maybe_b, |_, _| DidMerge(false, false))
    }

    fn modify(egraph: &mut EEGraph, id: Id) {
        // TODO: Call egraph.union_instanations when with_explanations_enabled is set?
        if let Some(c) = &egraph[id].data {
            if let Some(new) = c.1.clone() {
                let instantiation = egraph.add_instantiation(&new, &Default::default());
                egraph.union(id, instantiation);
            }
        }
    }
}

fn read_constant(
    data: &Option<(AstClassification, Option<PatternAst<Expr>>, Cost)>,
) -> Option<i64> {
    match data.as_ref()?.0 {
        AstClassification::Constant { value } => Some(value),
        _ => None,
    }
}

// Given an AND, XOR, or OR, of `2*x&2*y`, where a power of two can be factored
// out of its children, transform it into `2*(x&y)`.
// TODO: As of right now this transform only works if the constant multiplier is a power of 2,
// but it should work if any power of two can be factored out of the immediate multipliers.
impl Applier<Expr, ConstantFold> for BitwisePowerOfTwoFactorApplier {
    fn apply_one(
        &self,
        egraph: &mut EEGraph,
        eclass: Id,
        subst: &Subst,
        _searcher_ast: Option<&PatternAst<Expr>>,
        _rule_name: Symbol,
    ) -> Vec<Id> {
        // Get the eclass, expression, and of the expressions relating to X.
        let x_id = subst["?x".parse().unwrap()];

        let x_factor_data = &egraph[subst[self.x_factor.parse().unwrap()]].data;
        let x_factor_constant = read_constant(x_factor_data).unwrap();

        // Get the eclass, expression, and of the expressions relating to X.
        let y_id = subst["?y".parse().unwrap()];

        let y_factor_data = &egraph[subst[self.y_factor.parse().unwrap()]].data;
        let y_factor_constant = read_constant(y_factor_data).unwrap();

        let min = x_factor_constant.min(y_factor_constant);
        let min_id = egraph.add(Expr::Constant(min));
        let max = x_factor_constant.max(y_factor_constant);

        // Here we're dealing with expressions like "4*x&4*y" and ""4*x&8*y",
        // where x_factor and y_factor are the constant multipliers.
        // If the constant multipliers are the same, then for example
        // we can factor `4*x&4*y` into `4*(x&y)`.
        let factored: Id = if min == max {
            // Create an egraph node for (x & y).
            let anded = egraph.add(Expr::And([x_id, y_id]));

            // Create an egraph node for factored_out_constant * (x & y);
            egraph.add(Expr::Mul([min_id, anded]))
        }
        // If the factors are not equal(e.g. "4*x&8*y"), then we need to factor
        // out only the minimum factor, giving us something like "4*(x&2*y)".
        else {
            let remaining_factor = egraph.add(Expr::Constant(max / min));

            // If x has the large factor then the RHS becomes ((max/min) * x) & y;
            let rhs: Id = if x_factor_constant == max {
                let x_times_remaining_factor = egraph.add(Expr::Mul([remaining_factor, x_id]));
                egraph.add(Expr::And([x_times_remaining_factor, y_id]))
            // If y has the large factor then the RHS becomes ((max/min) * y) & x;
            } else {
                let y_times_remaining_factor = egraph.add(Expr::Mul([remaining_factor, y_id]));
                egraph.add(Expr::And([y_times_remaining_factor, x_id]))
            };

            // Create the final expression of (min * factored_rhs);
            egraph.add(Expr::Mul([min_id, rhs]))
        };

        if egraph.union(eclass, factored) {
            vec![factored]
        } else {
            vec![]
        }
    }
}

impl Applier<Expr, ConstantFold> for DuplicateChildrenMulAddApplier {
    fn apply_one(
        &self,
        egraph: &mut EEGraph,
        eclass: Id,
        subst: &Subst,
        _searcher_ast: Option<&PatternAst<Expr>>,
        _rule_name: Symbol,
    ) -> Vec<Id> {
        let new_const_expr = &egraph[subst[self.const_factor.parse().unwrap()]].data;

        let const_factor = read_constant(new_const_expr).unwrap();

        let x = subst[self.x_factor.parse().unwrap()];

        let new_const = egraph.add(Expr::Constant(const_factor + 1));
        let new_expr = egraph.add(Expr::Mul([new_const, x]));

        if egraph.union(eclass, new_expr) {
            vec![new_expr]
        } else {
            vec![]
        }
    }
}

impl Applier<Expr, ConstantFold> for RewritePowerApplier {
    fn apply_one(
        &self,
        egraph: &mut EEGraph,
        eclass: Id,
        subst: &Subst,
        _searcher_ast: Option<&PatternAst<Expr>>,
        _rule_name: Symbol,
    ) -> Vec<Id> {
        let exponent_id = subst[self.exponent];
        let exponent_data = &egraph[exponent_id].data;
        let exponent_constant = read_constant(exponent_data).unwrap();

        let a_id = subst[self.a];
        let a_data = &egraph[a_id].data;
        let a_constant = read_constant(a_data).unwrap();

        let b_id = subst[self.b];

        let const_value = if let Ok(exponent_constant) = u32::try_from(exponent_constant) {
            a_constant.pow(exponent_constant)
        } else {
            return Vec::new();
        };

        let const_id = egraph.add(Expr::Constant(const_value));
        let id = egraph.add(Expr::Pow([b_id, exponent_id]));
        let new_expr = egraph.add(Expr::Mul([id, const_id]));

        if egraph.union(eclass, new_expr) {
            vec![new_expr]
        } else {
            vec![]
        }
    }
}

fn make_rules() -> Vec<Rewrite> {
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
        rewrite!("mba-6"; "(+ (& ?d ?a) (& ?a (~ ?d)))" => "?a"),
        rewrite!("mba-7"; "(+ (& ?a (~ ?d)) (& ?d ?a))" => "?a"),
        rewrite!("mba-8"; "(+ (* (& ?d (* ?a ?d)) (| ?d (* ?a ?d))) (* (& (~ ?d) (* ?a ?d)) (& ?d (~ (* ?a ?d)))))" => "(* (** ?d 2) ?a)"),
        rewrite!("mba-9"; "(+ (+ ?a (* -2 ?d)) (* 2 (& (~ ?a) (* 2 ?d))))" => "(^ ?a (* 2 ?d))"),
        rewrite!("mba-10"; "(~ (* ?x ?y))" => "(+ (* (~ ?x) ?y) (+ ?y -1))"),
        rewrite!("mba-11"; "(~ (+ ?x ?y))" => "(+ (~ ?x) (+ (~ ?y) 1))"),
        rewrite!("mba-12"; "(~ (+ ?x (* ?y -1)))" => "(+ (~ ?x) (* (+ (~ ?y) 1) -1))"),
        rewrite!("mba-13"; "(~ (& ?x ?y))" => "(| (~ ?x) (~ ?y))"),
        rewrite!("mba-14"; "(~ (^ ?x ?y))" => "(| (& ?x ?y) (~ (| ?x ?y)))"),
        rewrite!("mba-15"; "(~ (| ?x ?y))" => "(& (~ ?x) (~ ?y))"),
        rewrite!("mba-16"; "(* (* ?x ?y) -1)" => "(* (* ?x -1) ?y)"),
        rewrite!("mba-17"; "(* (* ?x -1) ?y)" => "(* (* ?x ?y) -1)"),
        rewrite!("mba-18"; "(* (+ ?x ?y) -1)" => "(+ (* ?x -1) (* ?y -1))"),
        rewrite!("mba-19"; "(~ (& (~ ?a48) (~ ?a46)))" => "(| ?a46 ?a48)"),
        rewrite!("mba-20"; "(~ (& (~ ?a48) (~ ?a46)))" => "(| ?a46 ?a48)"),
        rewrite!("mba-21"; "(~ (& (~ ?a21) (~ ?a46)))" => "(| ?a21 ?a46)"),
        rewrite!("mba-22"; "(& (~ (& (~ ?a48) (~ ?a46))) (~ (& ?a48 ?a46)))" => "(^ ?a46 ?a48)"),
        rewrite!("mba-23"; "(& (~ (& (~ ?a48) (~ ?a46))) (~ (& ?a48 ?a46)))" => "(^ ?a46 ?a48)"),
        rewrite!("mba-24"; "(& (~ (& ?a21 ?a46)) (~ (& (~ ?a21) (~ ?a46))))" => "(^ ?a21 ?a46)"),
        rewrite!("mba-25"; "(~ (& (~ (& (~ ?a48) (~ ?a46))) (~ (& ?a48 ?a46))))" => "(~ (^ ?a46 ?a48))"),
        rewrite!("mba-26"; "(~ (& (~ (& ?a21 ?a46)) (~ (& (~ ?a21) (~ ?a46)))))" => "(~ (^ ?a21 ?a46))"),
        rewrite!("mba-27"; "(& (& (~ (& (~ ?a48) (~ ?a46))) (~ (& ?a48 ?a46))) (~ ?a21))" => "(& (~ ?a21) (^ ?a46 ?a48))"),
        rewrite!("mba-28"; "(& ?a21 (~ (& (~ (& (~ ?a48) (~ ?a46))) (~ (& ?a48 ?a46)))))" => "(+ 0 (~ (| (~ ?a21) (^ ?a46 ?a48))))"),
        rewrite!("mba-29"; "(~ (& (& (~ (& (~ ?a48) (~ ?a46))) (~ (& ?a48 ?a46))) (~ ?a21)))" => "(~ (& (~ ?a21) (^ ?a46 ?a48)))"),
        rewrite!("mba-30"; "(~ (& ?a21 (~ (& (~ (& (~ ?a48) (~ ?a46))) (~ (& ?a48 ?a46))))))" => "(| (~ ?a21) (^ ?a46 ?a48))"),
        rewrite!("mba-31"; "(& (~ ?a48) (~ (& (~ (& ?a21 ?a46)) (~ (& (~ ?a21) (~ ?a46))))))" => "(~ (| ?a48 (^ ?a21 ?a46)))"),
        rewrite!("mba-32"; "(~ (& (~ ?a48) (~ (& (~ (& ?a21 ?a46)) (~ (& (~ ?a21) (~ ?a46)))))))" => "(| ?a48 (^ ?a21 ?a46))"),
        rewrite!("mba-33"; "(+ (& ?a48 (| (~ ?a21) (~ ?a46))) (+ (& (~ ?a48) (~ (^ ?a21 ?a46))) (| (~ ?a48) (| ?a21 ?a46))))" => "(+ (* 2 -1) (* -1 (^ ?a21 (^ ?a46 ?a48))))"),
        rewrite!("mba-34"; "(+ (^ ?a48 (^ ?a21 ?a46)) (+ (& ?a48 (| (~ ?a21) (~ ?a46))) (+ (& (~ ?a48) (~ (^ ?a21 ?a46))) (| (~ ?a48) (| ?a21 ?a46)))))" => "(* 2 -1)"),
    ]
}

fn can_rewrite_power(
    a: &'static str,
    exponent: &'static str,
) -> impl Fn(&mut EEGraph, Id, &Subst) -> bool {
    let a = a.parse().unwrap();
    let b = "?b".parse().unwrap();
    let exponent = exponent.parse().unwrap();

    move |egraph, _, subst| {
        let a = &egraph[subst[a]].data;
        let b = &egraph[subst[b]].data;
        let exponent = &egraph[subst[exponent]].data;
        read_constant(a).is_some()
            && read_constant(b).is_none()
            && read_constant(exponent).is_some()
    }
}

fn is_const(var: &str) -> impl Fn(&mut EEGraph, Id, &Subst) -> bool {
    let var = var.parse().unwrap();

    move |egraph, _, subst| read_constant(&egraph[subst[var]].data).is_some()
}

fn is_power_of_two(var: &str, var2: &str) -> impl Fn(&mut EEGraph, Id, &Subst) -> bool {
    let var = var.parse().unwrap();
    let var2: Var = var2.parse().unwrap();

    move |egraph, _, subst| {
        let v1 = if let Some(c) = read_constant(&egraph[subst[var]].data) {
            c & (c - 1) == 0 && c != 0
        } else {
            false
        };

        let v2 = if let Some(c) = read_constant(&egraph[subst[var2]].data) {
            c & (c - 1) == 0 && c != 0
        } else {
            false
        };

        v1 && v2
    }
}

/// parse an expression, simplify it using egg, and pretty print it back out
fn simplify(s: &str, optimize_for_linearity: bool) -> String {
    let expr: RecExpr<Expr> = s.parse().unwrap();

    // Create the runner. You can enable explain_equivalence to explain the equivalence,
    // but it comes at a severe performance penalty.
    let explain_equivalence = false;
    let mut runner: Runner<Expr, ConstantFold> = Runner::default()
        .with_time_limit(Duration::from_millis(5000))
        .with_expr(&expr);

    if explain_equivalence {
        runner = runner.with_explanations_enabled();
    }

    let rules = make_rules();

    let start = Instant::now();
    runner = runner.run(&rules);

    // the Runner knows which e-class the expression given with `with_expr` is in
    let root = runner.roots[0];

    // use an Extractor to pick the best element of the root eclass
    let (best_cost, best) = if optimize_for_linearity {
        let cost_func = EGraphCostFn {
            egraph: &runner.egraph,
        };

        let extractor = Extractor::new(&runner.egraph, cost_func);
        extractor.find_best(root)
    } else {
        let extractor = Extractor::new(&runner.egraph, AstSize);
        extractor.find_best(root)
    };

    let duration = start.elapsed();
    println!("Time elapsed in simplify() is: {:?}", duration);

    println!(
        "Simplified {} \n\nto:\n{}\n with cost {}\n\n",
        expr, best, best_cost
    );

    best.to_string()
}

struct EGraphCostFn<'a> {
    egraph: &'a EGraph<Expr, ConstantFold>,
}

impl<'a> CostFunction<Expr> for EGraphCostFn<'a> {
    type Cost = usize;
    fn cost<C>(&mut self, enode: &Expr, _costs: C) -> Self::Cost
    where
        C: FnMut(Id) -> Self::Cost,
    {
        get_cost(self.egraph, enode) as usize
    }
}

fn main() {
    // Get the program arguments.
    let mut args = std::env::args();

    // Skip the first argument since it's always the path to the current program.
    args.next();

    // Read the optional expression to simplify.
    let expr = if let Some(next) = args.next() {
        next
    } else {
        "(+ (+ (+ (* (+ y (^ y (~ x))) (+ (* (| y x) (* x (* (* y x) (* y -4963545269917450240)))) (* (* x 1053768439367204864) (* y (& x (~ y)))))) (* (^ y (~ x)) (* (^ y (~ x)) (* (^ y x) 6500170837692252160)))) (+ (+ (+ (+ (* (& x (~ y)) (+ (* (^ y (~ x)) (* (^ y (~ x)) -1242936803585949696)) (* 1242936803585949696 (* (& y x) (& x (~ y)))))) (+ (* (* (^ y x) (& y x)) (+ (* (^ y x) (+ (* (| y x) 156381889551138816) (* -1 (* (* y x) 4494399601264033792)))) (+ (* (* y y) (* -1 8701149918271635456)) (* (* y x) (* (* y x) 2481772634958725120))))) (+ (* -1 (+ (* (^ y (~ x)) (* (| y x) (* (^ y (~ x)) (* (| y x) 6049732328593293312)))) (* 6347279416522964992 (* (| y x) (* (* (& y x) (| y x)) (+ y (^ y (~ x)))))))) (+ (* (* (^ y x) (^ y x)) (+ (* (^ y x) 9197308388596252672) (* (* y x) 4494399601264033792))) (+ (* (| y x) (* x (* (* y x) (* y -4963545269917450240)))) (* (+ (* (& x (~ y)) 1242936803585949696) (* y 414312267861983232)) (* y (* y -1)))))))) (+ (+ (+ 7477701790215481006 (* (^ y x) (* (^ y x) 2647290229186101248))) (+ (+ (* (| y x) (* (+ (& x (~ y)) (+ (^ y (~ x)) (* (& y x) -1))) (* y 7553939277059457024))) (* (* (* y y) (* 
            x 7029109185614708736)) (* (+ (& x (~ y)) (* (& y x) -1)) (* x (^ y (~ x)))))) (+ (+ (* 8701149918271635456 (* (& x (~ y)) (* y (* (^ y x) (& x (~ y)))))) (* (+ (* 579243341355417600 (* y y)) (* (& y x) (* (^ y x) 1044444237166280704))) (* y (+ (& x (~ y)) (^ y (~ x)))))) (+ (* -1 (+ (* y (+ (* (* x 1053768439367204864) (* y (& y x))) (* x (* x (* y 7878655039613435904))))) (+ 
            (* (* (^ y (~ x)) (^ y (~ x))) (* (* y x) 8696487817171173376)) (* (^ y x) (* (| y x) 7857583156965146624))))) (* (& x (~ y)) (+ (* (^ y (~ x)) 7381264205732642816) (* (| y x) (* (| y x) 4539428588151111680)))))))) (+ (* (+ (& x (~ y)) (^ y (~ x))) (+ (* (* (& y x) (& y x)) (* (^ y x) 8701149918271635456)) (* (* (| y x) (| y x)) (* (| y x) -208509186068185088)))) (+ (* (* (& x 
            (~ y)) (& x (~ y))) (+ (* (* y y) 868865012033126400) (* (& y x) (* -1 (+ (* (& x (~ y)) 579243341355417600) (* (^ y (~ x)) 1737730024066252800)))))) (+ (* (^ y (~ x)) (* (+ (* (& x (~ y)) (& x (~ y))) (* (& y x) (& y x))) (* (^ y (~ x)) 868865012033126400))) (+ (+ (* -1 (+ (* 9078561201515921408 (* y (* y (* y y)))) (* (* (& y x) (& x (~ y))) (* (^ y x) (* (& x (~ y)) 8701149918271635456))))) (* (* (* y x) 4494399601264033792) (* (* (^ y x) (^ y x)) (+ y (+ (& x (~ y)) (^ y (~ x))))))) (+ (* (* (& y x) (& y x)) (+ (* 2900383306090545152 (* (^ y x) (* (& y x) -1))) (* (^ y (~ x)) (+ (* (& x (~ y)) 1737730024066252800) (* -1 (* (& y x) 579243341355417600)))))) (* (^ y (~ x)) (+ (* (^ y (~ x)) (* (* y y) 868865012033126400)) (* (* y (& x (~ y))) (* (& 
            x (~ y)) 1737730024066252800))))))))))) (+ (* (^ y (~ x)) (* (* y y) (* x 1053768439367204864))) (+ (* (+ y (^ y (~ x))) (+ (* (* (^ y x) (| y x)) (+ (* (| y x) -312763779102277632) (+ (* (* y x) -469145668653416448) (* (^ y x) -156381889551138816)))) (* (* y (* x x)) (+ (* (^ y x) (* y -2481772634958725120)) (* 1240886317479362560 (* -1 (* x (* y y)))))))) (+ (* (| y x) (+ (* 
            (& x (~ y)) (+ (* (& x (~ y)) (* x (* y -9074598492889939968))) (* (^ y x) (* y 6347279416522964992)))) (+ (* (^ y (~ x)) (* (* (| y x) (& x (~ y))) 6347279416522964992)) (+ (+ (* (^ y (~ x)) (* (* y -297547087929671680) (* x (& y x)))) (* x (* y -2563002698592944128))) (+ (* (* x (* y y)) (* (| y x) -469145668653416448)) (* (^ y x) (* -1 (+ (* 6049732328593293312 (* (^ y (~ x)) (^ y (~ x)))) (* (& y x) (* y 6347279416522964992)))))))))) (+ (+ (* (* (^ y x) (^ y (~ x))) (* (^ y (~ x)) (* y 8701149918271635456))) (* -1 (+ (* (| y x) (* (^ y (~ x)) (* (& y x) 7553939277059457024))) (* (* x (* y (* y y))) 8696487817171173376)))) (+ (+ (+ (* (* (& y x) (& y x)) (* (& y x) (* (* y x) (* -1 4350574959135817728)))) (* (& x (~ y)) (+ (* (& x (~ y)) (* (* y x) (* (& x (~ y)) 4350574959135817728))) (* (^ y (~ x)) (* (^ y x) (* y -1044444237166280704)))))) (+ (* (+ (* x (* (^ y (~ x)) 4350574959135817728)) (+ (* (& x (~ y)) (* x -5395019196302098432)) (* (& y x) (* x 5395019196302098432)))) (* y (* (^ y (~ x)) (^ y (~ x))))) (+ (* (* (& y x) (& x (~ y))) (* 9074598492889939968 (* (^ y x) (* y x)))) (* (* y y) (* (* x x) (* y 4859271590048694272)))))) (+ (* (* (& x (~ y)) (^ y (~ x))) (* (| y x) 7553939277059457024)) (* (& y x) (* (* y (* y y)) (* x 5395019196302098432)))))))))) (+ (* (& x (~ y)) (+ (* (* (^ y x) (& x (~ y))) (+ (* (^ y (~ x)) 8701149918271635456) (* (& x (~ y)) 2900383306090545152))) (+ (+ (* (* (^ y x) (| y x)) (+ (* (| y x) -312763779102277632) (+ (* (* y x) -469145668653416448) (* (^ y 
            x) -156381889551138816)))) (* (* y (* x x)) (+ (* (^ y x) (* y -2481772634958725120)) (* 1240886317479362560 (* -1 (* x (* y y))))))) (* (| y x) (+ (* (| y x) (* y 6347279416522964992)) (* x (* (* y x) (* y -4963545269917450240)))))))) (+ (+ (+ (* (* y (* x (* (| y x) (| y x)))) (* (& y x) 469145668653416448)) (* (* (| y x) (& x (~ y))) (+ (* (* y y) -1044444237166280704) (* (^ y (~ x)) (* -1 (* y 2088888474332561408)))))) (+ (* (& y x) (+ (* -1 (* (* (* y (* y y)) (* x x)) 7029109185614708736)) (+ (* (* (| y x) (| y x)) (+ (* (^ y x) 312763779102277632) (* (| y x) 208509186068185088))) (* (* y (| y x)) (+ (* (* x x) (* y 4963545269917450240)) (* (^ y x) (* x 469145668653416448))))))) (+ (* -1 (+ (* (^ y x) 911170646057156606) (+ (* x (* y 1366755969085734909)) (* (| y x) (+ (* (| y x) 7857583156965146624) 1822341292114313212))))) (* (* (^ y x) (& y x)) (+ (* (& y x) 6500170837692252160) (* (& x (~ y)) 5446402398325047296)))))) (+ (* (* (^ y (~ x)) (^ y (~ x))) (+ (* -5395019196302098432 (* x (* y y))) (+ (* (^ y (~ x)) (* y 579243341355417600)) (+ (* (& x (~ y)) (* y 1737730024066252800)) (* -1 (+ (* (& y x) (* y 1737730024066252800)) (* (^ y (~ x)) (* (^ y (~ x)) 9078561201515921408)))))))) (+ (+ (* (| y x) (+ (* (* y y) (+ (* (& y x) 1044444237166280704) (* y 5800766612181090304))) (* (* (^ y (~ x)) (^ y (~ x))) (+ (* y -1044444237166280704) (* (^ y (~ x)) 5800766612181090304))))) (+ (* (^ y x) (+ (* (* y (^ y x)) (* y 7710938954706452480)) (* (* (^ y x) (& x (~ y))) (+ (* (^ y (~ x)) -3024866164296646656) (* (& x (~ y)) 7710938954706452480))))) (+ (* (& y x) (* (+ (& x (~ y)) (^ y (~ x))) (* -1 (* (* y y) 1737730024066252800)))) (+ (* (* -1 (+ (* (^ y (~ x)) 579243341355417600) (* (& x (~ y)) 1737730024066252800))) (* (^ y (~ x)) (* (& y x) (^ y (~ x))))) (* (* x (& x (~ y))) (* (| y x) (* y 6809142882226667520))))))) (+ (+ (* (* (^ y x) (& x (~ y))) (+ (* -1 (* (^ y (~ x)) (* x (* y 9074598492889939968)))) (+ (* (^ y x) (* y -3024866164296646656)) (* (* y x) (* (& x (~ y)) 4686072790409805824))))) (+ (* (^ y (~ x)) (* (* (^ y x) (^ y (~ x))) (* (^ y x) 7710938954706452480))) (+ (* (+ (* (& x (~ y)) (& x (~ y))) (* (& y x) (& y x))) (* (| y x) (* y -1044444237166280704))) (+ (* (* (& y x) (& x (~ y))) (* (^ y x) (* (^ y x) 3024866164296646656))) (* (+ (& x (~ y)) (^ y (~ x))) (+ (* (| y x) (* (^ y x) 4539428588151111680)) (* (* y (* x x)) (* y 4859271590048694272)))))))) (+ (* (& y x) (+ (* (^ y x) (* -1 2688616885544550400)) (* (& y x) (* y -1242936803585949696)))) (+ (* (^ y x) (+ (* (| y x) (* (^ y x) -156381889551138816)) (* y 2688616885544550400))) (* y (+ (* (^ y (~ x)) (+ (* (| y x) (* y -1044444237166280704)) (* (^ y x) (* (^ y x) -3024866164296646656)))) (+ (* (+ (& x (~ y)) (^ y (~ x))) (* (^ y x) (* y 8701149918271635456))) (* (& y x) (* (& y x) (* (^ y x) (* x 4686072790409805824)))))))))))))))) (+ (+ (* (* y (| y x)) (+ (* (^ y x) 4539428588151111680) (* (+ y (^ y (~ x))) (* x 6809142882226667520)))) (+ (+ (+ (* (^ y x) (* x (+ (* (* (^ y (~ x)) (^ y (~ x))) (* y 4686072790409805824)) (* 9074598492889939968 (* (& y x) (* y (+ y (^ y (~ x))))))))) (+ (* 4539428588151111680 (* (+ y (^ y (~ x))) (* (| y x) (| y x)))) (* (* x -5395019196302098432) (+ (* (^ y (~ x)) (* y (* (& y x) (& y x)))) (* (& x (~ y)) (* y (* y y))))))) (+ (+ (* (* (& y x) (& x (~ y))) (+ (* -5395019196302098432 (* (& y x) (* y x))) (* (* y (& x (~ y))) (* x 5395019196302098432)))) (+ (+ (* (& y x) (* (| y x) (* (^ y x) (* -1 4539428588151111680)))) (* (* (& x (~ y)) (& x (~ y))) (+ 3690632102866321408 (* (^ y (~ x)) (* y (* x -5395019196302098432)))))) (* (& y x) (+ (* (| y x) (+ (* (^ y (~ x)) (* y 2088888474332561408)) (* (* -1 (& x (~ y))) 7553939277059457024))) (+ (* -1 (+ (* (* y (* x x)) (* y 4859271590048694272)) (* (& x (~ y)) 7381264205732642816))) (+ (* (^ y x) (* x (* y 5818800595741442048))) (* (& y x) 3690632102866321408))))))) (+ (* (* (| y x) (& x (~ y))) (+ (* (& y x) (* y 2088888474332561408)) (* (^ y (~ x)) (* (^ y (~ x)) -1044444237166280704)))) (* (* (^ y x) 9197308388596252672) (* (* (^ y x) (^ y x)) (+ y (+ (& x (~ y)) (^ y (~ x))))))))) (+ (* (| y x) (* y 5377233771089100800)) (+ (+ (* (* y x) (+ (* (* (| y x) (| y x)) -469145668653416448) (* (^ y x) (+ (* (| y x) -469145668653416448) (+ (* y (* x -2481772634958725120)) 7941870687558303744))))) (* (& x (~ y)) (+ (* (* (^ y x) (^ y (~ x))) (+ (* (^ y (~ x)) 8701149918271635456) (* (& y x) 1044444237166280704))) (+ (* (* (& y x) (& y x)) (+ (* (| y x) -1044444237166280704) (* (& x (~ y)) 868865012033126400))) (+ (* (* (| y x) (& x (~ y))) (+ (* (& y x) 1044444237166280704) (+ (* (^ y (~ x)) -1044444237166280704) (* (& x (~ y)) 5800766612181090304)))) (* -1 (+ (* (* (& y x) (& y x)) (* (& y x) 579243341355417600)) (+ (* 7656705681105354752 (* (^ y (~ x)) (* (& y x) (* y x)))) (* 9078561201515921408 (* (& x (~ y)) (* (& x (~ y)) (& x (~ y))))))))))))) (+ (* (& y x) (+ (* -1 (+ (* (| y x) 5377233771089100800) 2504084959427864238)) (+ (+ (* (* (& y x) (& y x)) 414312267861983232) (* x (* -1 (* y 4032925328316825600)))) (+ (* (& x (~ y)) (* (^ y (~ x)) 2485873607171899392)) (* (+ (& x (~ y)) (^ y (~ x))) (* (& y x) -1242936803585949696)))))) (+ (* (* (& y x) (^ y (~ x))) (+ (* -1 7381264205732642816) (* (^ y (~ x)) 1242936803585949696))) (+ (* (* y y) 3690632102866321408) (* (& y x) (+ (* (* (| y x) (^ y (~ x))) (+ (* (& y x) -1044444237166280704) (* (^ y (~ x)) 1044444237166280704))) (+ (* (| y x) (* (& x (~ y)) (* (^ y (~ x)) 2088888474332561408))) (+ (* (* (| y x) (& x (~ y))) (+ (* x (* y -297547087929671680)) (* (^ y x) (* -1 6347279416522964992)))) (* -1 (+ (* (+ (& x (~ y)) (^ y (~ x))) (* (* y y) (* x 7656705681105354752))) (* (* (& y x) (| y x)) (+ (* (* y x) 9074598492889939968) (* (& y x) 5800766612181090304)))))))))))))))) (+ (+ (+ (+ (+ (* (^ y (~ x)) (+ (* 6347279416522964992 (* (| y x) (* (^ y x) (+ y (& x (~ y)))))) (* -1 (+ (* (| y x) (* (^ y x) (* (& y x) 6347279416522964992))) (* (^ y (~ x)) (* y (* (* x x) (* y 5708817444047421440)))))))) (+ (* (* (& y x) (| y x)) (+ (* (| y x) (* -1 4539428588151111680)) (* (* y x) -6809142882226667520))) (* (^ y x) (* x (* (* y (* y y)) 4686072790409805824))))) (+ (+ (+ (* (^ y (~ x)) (* (& x (~ y)) (* (* y y) (* x 7656705681105354752)))) (+ (* (+ y (^ y (~ x))) (* (* (| y x) (& x (~ y))) (* (* y x) 297547087929671680))) (* (+ (* (& x (~ y)) (& x (~ y))) (* (& y x) (& y x))) (* -5395019196302098432 (* x (* y y)))))) (+ (* (& y x) (+ (* (& y x) (* (* y y) 868865012033126400)) (* 1737730024066252800 (* (* y (& x (~ y))) (+ (& y x) (* -1 (& x (~ y)))))))) (* (* x (* y (* y y))) (+ (* y 4350574959135817728) (* (^ y (~ x)) -5395019196302098432))))) (+ (+ (* (+ (& x (~ y)) (^ y (~ x))) (* y (* (^ y x) (* x -5818800595741442048)))) (* (* (^ y (~ x)) 2900383306090545152) (* (^ y (~ x)) (* (^ y x) (^ y (~ x)))))) 
            (+ (* (* (& y x) (& y x)) (+ (* (^ y x) (* (^ y x) 7710938954706452480)) (* y (* -1 (* (* x x) (* y 5708817444047421440)))))) (+ (* -1 (+ (* (| y x) (* (| y x) (* 6347279416522964992 (* (& y x) (& x (~ y)))))) (* (* (| y x) (* (| y x) 6049732328593293312)) (+ (* y y) (* (& y x) (& y x)))))) (+ (* (* (& x (~ y)) (& x (~ y))) (* (& x (~ y)) (* y 579243341355417600))) (+ (* (^ y (~ x)) (* (| y x) (* (| y x) (* y 6347279416522964992)))) (* (* (& y x) (^ y (~ x))) (+ (* (& y x) (* y 1737730024066252800)) (* -1 (* (* y (& x (~ y))) 3475460048132505600))))))))))) (+ (+ (* (* (| y x) -5446402398325047296) (+ (* (& y x) (& y x)) (+ (* (^ y (~ x)) (^ y (~ x))) (+ (* y y) (* (& x (~ y)) (& x (~ y))))))) (+ (* (* (* y (* y y)) (* x x)) (+ (* (& y x) (* x 1240886317479362560)) (* (+ (& x (~ y)) (^ y (~ x))) 7029109185614708736))) (+ (* (* y (^ y x)) (+ (* (^ y (~ x)) -5446402398325047296) (* y 6500170837692252160))) (+ (* x (* (^ y x) (* (* y y) -5818800595741442048))) (+ (* (* (& x (~ y)) (^ y (~ x))) (+ (* (* y y) 1737730024066252800) (* 579243341355417600 (* (^ y (~ x)) (^ y (~ x)))))) (+ (* (+ y (^ y (~ x))) (* (^ y x) (* (^ y x) -8088514889816997888))) (* -1 (+ (* (* (& x (~ y)) (& x (~ y))) (* (| y x) (* (| y x) 6049732328593293312))) (+ (* (+ (& x (~ y)) (^ y (~ x))) (+ (* (* x 1053768439367204864) (* y (& y x))) (* (^ y x) (* (* y y) (* x 9074598492889939968))))) (+ (* (+ (* (& x (~ y)) (& x (~ y))) (* (& y x) (& y x))) (* (* y x) 8696487817171173376)) (+ (* 5708817444047421440 (* (* x (* y (& x (~ y)))) (* x (* y (& x (~ y)))))) (+ (* (* y y) (* (* (& x (~ y)) (* x (& y x))) (* x 7029109185614708736))) (* (* (^ y x) 9197308388596252672) (* (& y x) (* (^ y x) (^ y x)))))))))))))))) (* (* (& x (~ y)) (& x (~ y))) (+ (* (^ y (~ x)) (* (& x (~ y)) 579243341355417600)) (* (| y x) (* (^ y x) (* -1 6049732328593293312))))))) (+ (+ (* y (+ (* -1 (* 5708817444047421440 (* (* y (* 
            y y)) (* x x)))) (* (^ y (~ x)) (* 297547087929671680 (* (| y x) (* y x)))))) (* (| y x) (+ (* (^ y (~ x)) (* (^ y (~ x)) (* x (* y -9074598492889939968)))) (+ (* (* y y) (+ (* x (* y -9074598492889939968)) (* (^ y x) (* -1 6049732328593293312)))) (* y (* (* y -297547087929671680) (* x (& y x)))))))) (+ (* (^ y x) (+ (* (* (& y x) (| y x)) (* (& y x) (* -1 6049732328593293312))) (* (^ y x) (+ (* (& x (~ y)) -8088514889816997888) (* (& y x) 8088514889816997888))))) (+ (* (* (& y x) (+ y (^ y (~ x)))) (* (^ y x) (* (^ y x) 3024866164296646656))) (* (& y x) (* -1 (+ (* (* (^ y x) (^ y (~ x))) (* (^ y (~ x)) 8701149918271635456)) (+ (* (+ (* y 579243341355417600) (* (& y x) 9078561201515921408)) (* (& y x) (& y x))) (* y (* 579243341355417600 (* y y))))))))))) (+ (+ (* (& x (~ y)) (+ (* (^ y (~ x)) (* y -2485873607171899392)) (* (| y x) (* (| y x) (* (* y x) -469145668653416448))))) (+ (+ (* (* (& x (~ y)) (& x (~ y))) (+ (* (^ y (~ x)) -1242936803585949696) (* -1 (* (& x (~ y)) 414312267861983232)))) (* (* (^ y x) (& x (~ y))) (+ (* (^ y (~ x)) -5446402398325047296) (* (& x (~ y)) 6500170837692252160)))) (+ (* -5446402398325047296 (* (& x (~ y)) (* y (^ y x)))) (+ (* (* y y) (* (& y x) 1242936803585949696)) (* (* y (& x (~ y))) (* (& x (~ y)) -1242936803585949696)))))) (+ (+ (* y (* (| y x) (* (| y x) (* x (* (^ y (~ x)) -469145668653416448))))) (+ (* (* (& y x) (+ y (^ y (~ x)))) (* (^ y x) 5446402398325047296)) (* (* (| y x) (| y x)) (+ (* -208509186068185088 (+ (| y x) (* y (| y x)))) (* (^ y x) -312763779102277632))))) (+ (+ (* (+ (& x (~ y)) (^ y (~ x))) (+ (+ 2504084959427864238 (* (& y x) (* y 2485873607171899392))) (+ (* (| y x) 5377233771089100800) (+ (* x (* y 4032925328316825600)) (* (^ y x) 2688616885544550400))))) (+ (* (& x (~ y)) (* y 7381264205732642816)) (+ (* (* (^ y (~ x)) (^ y (~ x))) (+ 3690632102866321408 (* -1 (* (^ y (~ x)) 414312267861983232)))) 
            (+ (+ (* (+ y (^ y (~ x))) (* (^ y (~ x)) (* y -1242936803585949696))) (* (* y (& y x)) (+ (* -1 7381264205732642816) (* (& y x) (* (^ y x) 8701149918271635456))))) (* (^ y x) (* (* y y) (* y 2900383306090545152))))))) (* y (+ (* (^ y (~ x)) 7381264205732642816) (+ (* -1 (* (* y (* x x)) (* (* y x) 1240886317479362560))) (+ 2504084959427864238 (* x (* y 4032925328316825600))))))))))))".to_owned()
    };

    println!("Attempting to simplify expression: {}", expr);

    let mut simplified = simplify(expr.as_str(), true);

    for i in 0..10 {
        simplified = simplify(&simplified, i % 2 == 0);
    }

    simplified = simplify(&simplified, false);
    simplified = simplify(&simplified, false);
    println!("{}", simplified);
}

#[cfg(test)]
mod test {
    use crate::simplify;

    macro_rules! validate_output {
        ($input:expr, $output:expr) => {
            assert_eq!(simplify($input, false), $output)
        };
    }

    #[test]
    fn basic_arith_const_prop() {
        validate_output!("(+ 1 2)", "3");
        validate_output!("(* 1 2)", "2");
        validate_output!("(** 1 2)", "1");
    }

    #[test]
    fn basic_arith_const_prop_with_negative() {
        validate_output!("(+ -1 -2)", "-3");
        validate_output!("(* -1 2)", "-2");
        validate_output!("(** -1 3)", "-1");
    }

    #[test]
    fn basic_bitwise_const_prop() {
        validate_output!("(& 1 2)", "0");
        validate_output!("(| 1 2)", "3");
        validate_output!("(^ 1 2)", "3");
        validate_output!("(~ 1)", "-2");
    }

    #[test]
    fn already_simplified() {
        validate_output!("(+ ?a 1)", "(+ ?a 1)");
        validate_output!("(~ ?a)", "(~ ?a)");
        validate_output!("?a", "?a");
        validate_output!("5", "5");
    }

    #[test]
    fn simplifies_double_negation() {
        validate_output!("(* (* ?a -1) -1)", "?a");
        validate_output!("(~ (~ ?a))", "?a");
    }

    #[test]
    fn adding_same_value_is_multiplication() {
        validate_output!("(+ ?a (+ ?a ?a))", "(* ?a 3)");
    }

    #[test]
    fn extracts_constant_from_pow() {
        validate_output!("(** (* ?a 2) 2)", "(* 4 (** ?a 2))");
        validate_output!("(** (* 2 ?a) 2)", "(* 4 (** ?a 2))");
        validate_output!("(* (** (* ?a 2) 2) 2)", "(* (** ?a 2) 8)");
        validate_output!("(* (* ?a 2) (* ?a 2))", "(* (* ?a ?a) 4)");
    }
}
