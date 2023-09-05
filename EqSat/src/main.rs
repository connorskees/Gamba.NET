use core::panic;
use std::time::{Duration, Instant};

use egg::*;

type Cost = f64;

type ApplierEGraph = egg::EGraph<Expr, BitwiseAnalysis>;
type ApplierREwrite = egg::Rewrite<Expr, BitwiseAnalysis>;

pub type EEGraph = egg::EGraph<Expr, ConstantFold>;
pub type Rewrite = egg::Rewrite<Expr, ConstantFold>;

//pub type Constant = i64;

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
pub struct BitwisePowerOfTwoFactorApplier {
    x_factor: String,
    y_factor: String,
}

#[derive(Debug)]
pub struct DuplicateChildrenMulAddApplier {
    const_factor: String,
    x_factor: String,
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

    //println!("applying const prop to: {}", enode);
    Some(match enode {
        Expr::Constant(c) => {
            let msg = format!("{}", c).parse().unwrap();
            //println!("constant const prop: {}", msg);
            (*c, msg)
        }
        Expr::Add([a, b]) => {
            let msg = format!("(+ {} {})", x(a)??, x(b)??).parse().unwrap();
            //println!("add const prop: {}", msg);
            (x(a)?? + x(b)??, msg)
        }
        Expr::Mul([a, b]) => (
            x(a)?? * x(b)??,
            format!("(* {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::Pow([a, b]) => (
            x(a)??.pow((x(b)??).try_into().unwrap()),
            format!("(** {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::And([a, b]) => (
            x(a)?? & x(b)??,
            format!("(& {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::Or([a, b]) => (
            x(a)?? | x(b)??,
            format!("(| {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::Xor([a, b]) => (
            x(a)?? ^ x(b)??,
            format!("(^ {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::Neg([a]) => {
            //println!("NEGATION: ~ {}", x(a)?);
            let msg = format!("(~ {})", x(a)??).parse().unwrap();
            //println!("{}", msg);
            let result = (!x(a)??, msg);
            result
        }
        Expr::Symbol(_) => return None,
    })
}

fn to_pattern_ast(egraph: &EEGraph, enode: &Expr) -> Option<(i64, PatternAst<Expr>)> {
    let x = |i: &Id| {
        egraph[*i].data.as_ref().map(|c| match c.0 {
            AstClassification::Constant { value } => Some(value),
            _ => None,
        })
    };

    //println!("applying const prop to: {}", enode);
    Some(match enode {
        Expr::Constant(c) => {
            let msg = format!("{}", c).parse().unwrap();
            //println!("constant const prop: {}", msg);
            (*c, msg)
        }
        Expr::Add([a, b]) => {
            let msg = format!("(+ {} {})", x(a)??, x(b)??).parse().unwrap();
            //println!("add const prop: {}", msg);
            (x(a)?? + x(b)??, msg)
        }
        Expr::Mul([a, b]) => (
            x(a)?? * x(b)??,
            format!("(* {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::Pow([a, b]) => (
            x(a)??.pow((x(b)??).try_into().unwrap()),
            format!("(** {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::And([a, b]) => (
            x(a)?? & x(b)??,
            format!("(& {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::Or([a, b]) => (
            x(a)?? | x(b)??,
            format!("(| {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::Xor([a, b]) => (
            x(a)?? ^ x(b)??,
            format!("(^ {} {})", x(a)??, x(b)??).parse().unwrap(),
        ),
        Expr::Neg([a]) => {
            //println!("NEGATION: ~ {}", x(a)?);
            let msg = format!("(~ {})", x(a)??).parse().unwrap();
            //println!("{}", msg);
            let result = (!x(a)??, msg);
            result
        }
        Expr::Symbol(_) => return None,
    })
}

fn classify(
    egraph: &EEGraph,
    enode: &Expr,
) -> Option<(AstClassification, Option<PatternAst<Expr>>)> {
    //let child_eval = |x: &Expr| eval(egraph, x);
    let op = |i: &Id| egraph[*i].data.as_ref().map(|c| c.0);

    // If we have any operation that may be folded into a constant(e.g. x + 0, x ** 0), then do it.
    let const_folded: Option<(i64, RecExpr<ENodeOrVar<Expr>>)> = try_fold_constant(egraph, enode);
    if const_folded.is_some() {
        return Some((
            AstClassification::Constant {
                value: (const_folded.unwrap().0),
            },
            Some(const_folded.unwrap().1),
        ));
    }

    // Otherwise const folding has failed. Now we need to classify it.
    let result: AstClassification = match enode {
        Expr::Constant(def) => AstClassification::Constant { value: *def },
        Expr::Symbol(def) => AstClassification::Linear { is_variable: true },
        Expr::Pow([a, b]) => AstClassification::Nonlinear,
        Expr::Mul([a, b]) => {
            // At this point constant propagation has handled all cases where two constant children are used.
            // So now we only have to handle the other cases. First we start by checking the classification of the other(non constant) child.
            let other = get_non_constant_child_classification(op(a)?, op(b)?);

            // If neither operand is a constant then the expression is not linear.
            if other.is_none() {
                return Some((AstClassification::Nonlinear, None));
            }

            let result = match other.unwrap() {
                AstClassification::Unknown => panic!("Hopefully this shouldn't happen"),
                AstClassification::Constant { value } => panic!("Constant propagation should have already handled multiplication of two constants!"),
                // const * bitwise = mixed expression
                AstClassification::Bitwise => AstClassification::Mixed,
                // const * linear = linear
                AstClassification::Linear { is_variable } => AstClassification::Linear { is_variable: false },
                // const * nonlinear = nonlinear
                AstClassification::Nonlinear => AstClassification::Nonlinear,
                // const * mixed(bitwise and arithmetic) = mixed
                AstClassification::Mixed => AstClassification::Mixed,
            };

            return Some((result, None));
        }

        Expr::Add([a, b]) => {
            // Adding any operand (A) to any non linear operand (B) is always non linear.
            let children = [op(a)?, op(b)?];
            if children.into_iter().any(|x| match x {
                AstClassification::Nonlinear => true,
                _ => false,
            }) {
                return Some((AstClassification::Nonlinear, None));
            };

            // At this point we've established (above^) that there are no nonlinear children.
            // This leaves potentially constant, linear, bitwise, and mixed expressions left.
            // So now we check if either operand is mixed(bitwise + arithmetic) or bitwise.
            // In both cases, adding anything to a mixed or bitwise expression will be considered a mixed expression.
            if children.into_iter().any(|x| match x {
                AstClassification::Mixed => true,
                AstClassification::Bitwise => true,
                _ => false,
            }) {
                return Some((AstClassification::Mixed, None));
            };

            // Now an expression is either a constant or a linear child.
            // If any child is linear then we consider this to be a linear arithmetic expression.
            // Note that constant folding has already eliminated addition of constants.
            if children.into_iter().any(|x| match x {
                AstClassification::Linear { is_variable } => true,
                AstClassification::Bitwise => true,
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
            panic!()
        }
        Expr::Neg([a]) => classify_bitwise(op(a)?, None),
        Expr::And([a, b]) => classify_bitwise(op(a)?, Some(op(b)?)),
        Expr::Or([a, b]) => classify_bitwise(op(a)?, Some(op(b)?)),
        Expr::Xor([a, b]) => classify_bitwise(op(a)?, Some(op(b)?)),
    };

    return Some((result, None));
}

fn classify_bitwise(a: AstClassification, b: Option<AstClassification>) -> AstClassification {
    // TODO: Throw if we see negation with a constant, that should be fixed.
    let mut children = if b.is_some() {
        [a, b.unwrap()].iter()
    } else {
        [a].iter()
    };

    // Check if the expression contains constants or arithmetic expressions.
    let containsConstantOrArithmetic = children.any(|x| match x {
        AstClassification::Constant { value } => true,
        // We only want to match linear arithmetic expressions - variables are excluded here.
        AstClassification::Linear { is_variable } => {
            if *is_variable {
                false
            } else {
                true
            }
        }
        _ => false,
    });

    // Check if the expression contains constants or arithmetic expressions.
    let containsMixedOrNonLinear = children.any(|x| match x {
        AstClassification::Mixed => true,
        AstClassification::Nonlinear => true,
        _ => false,
    });

    // Bitwise expressions are considered to be nonlinear if they contain constants,
    // arithmetic(linear) expressions, or non linear subexpressions.
    if containsConstantOrArithmetic || containsMixedOrNonLinear {
        return AstClassification::Nonlinear;
    } else if children.any(|x: &AstClassification| match x {
        AstClassification::Linear { is_variable } => {
            if *is_variable {
                false
            } else {
                true
            }
        }
        AstClassification::Mixed => true,
        _ => false,
    }) {
        return AstClassification::Mixed;
    };

    // If none of the children are nonlinear or arithmetic then this is a pure bitwise expression.
    return AstClassification::Bitwise;
}

// If either one of the children is a constant, return the other one.
fn get_non_constant_child_classification(
    a: AstClassification,
    b: AstClassification,
) -> Option<(AstClassification)> {
    let mut constChild: Option<AstClassification> = None;
    let mut otherChild: Option<AstClassification> = None;
    match a {
        AstClassification::Constant { value } => {
            constChild = Some(a);
        }
        _ => {
            otherChild = Some(a);
        }
    }

    match b {
        AstClassification::Constant { value } => {
            constChild = Some(b);
        }
        _ => {
            otherChild = Some(b);
        }
    }

    if constChild.is_none() {
        return None;
    }

    return Some(otherChild.unwrap());
}

fn get_expr_pattern_ast(enode: &Expr) -> PatternAst<Expr> {
    return format!("{}", 0).parse().unwrap();
}

fn get_classification_cost(kind: AstClassification) -> Cost {
    match kind {
        AstClassification::Constant { value } => 0.1,
        AstClassification::Linear { is_variable } => {
            if is_variable {
                0.1
            } else {
                1.0
            }
        }
        AstClassification::Bitwise => 1.0,
        AstClassification::Mixed => 1.5,
        AstClassification::Nonlinear => 3.0,
        AstClassification::Unknown => panic!("Hopefully this never happens?"),
    }
}

fn get_cost(egraph: &EEGraph, enode: &Expr) -> Cost {
    let op = |i: &Id| get_classification_cost(egraph[*i].data.unwrap().0);
    match enode {
        Expr::Add([a, b]) => op(a) + op(b),
        Expr::Mul([a, b]) => op(a) + op(b),
        Expr::Pow([a, b]) => op(a) + op(b),
        Expr::And([a, b]) => op(a) + op(b),
        Expr::Or([a, b]) => op(a) + op(b),
        Expr::Xor([a, b]) => op(a) + op(b),
        Expr::Neg([a]) => op(a),
        Expr::Constant(_) => 0.1,
        Expr::Symbol(_) => 0.1,
    }
}

#[derive(Default)]
pub struct ConstantFold;
impl Analysis<Expr> for ConstantFold {
    type Data = Option<(AstClassification, Option<PatternAst<Expr>>, Cost)>;

    fn make(egraph: &EEGraph, enode: &Expr) -> Self::Data {
        // Classify the expression. Then throw if it doesn't have a classification - that should never happen.
        let maybe_classification = classify(egraph, enode);
        if (maybe_classification.is_none()) {
            panic!("Classifications cannot be none!");
        }

        // If we classified the AST and returned a new PatternAst<Expr>, that means constant
        // folding succeed. So now we return the newly detected classification and the pattern ast.
        let classification = maybe_classification.unwrap();
        if (classification.1.is_some()) {
            return Some((classification.0, classification.1, get_cost(egraph, enode)));
        }

        // Otherwise we've classified the AST but there was no constant folding to be performed.
        return Some((classification.0, classification.1, get_cost(egraph, enode)));
    }

    fn merge(&mut self, maybeA: &mut Self::Data, maybeB: Self::Data) -> DidMerge {
        return merge_option(maybeA, maybeB, |maybeA, maybeB| DidMerge(false, false));

        // TODO: Not sure how merging is supposed to work?
        let mut did_merge = DidMerge(false, false);
        let mut a = maybeA.unwrap();
        let mut b = maybeB.unwrap();

        let classification_a_cost = get_classification_cost(a.0);
        let classification_b_cost = get_classification_cost(b.0);

        return did_merge;
        /*
        merge_option(maybeA, maybeB, |maybeA, maybeB| {
            //assert_eq!(a., b.0, "Merged non-equal constants");
            //panic!("todo!");
            DidMerge(false, false)
        })
        */
    }

    fn modify(egraph: &mut EEGraph, id: Id) {
        panic!("todo!");

        if let Some(c) = egraph[id].data.clone() {
            egraph.union_instantiations(
                &c.1,
                &c.0.to_string().parse().unwrap(),
                &Default::default(),
                "analysis".to_string(),
            );
        }
    }
}

// Given an AND, XOR, or OR, of `2*x&2*y`, where a power of two can be factored out of it's children,
// transform it into `2*(x&y)`.
// TODO: As of right now this transform only works if the constant multiplier is a power of 2,
// but it should work if any power of two can be factored out of the immediate multipliers.
impl Applier<Expr, ConstantFold> for BitwisePowerOfTwoFactorApplier {
    fn apply_one(
        &self,
        egraph: &mut EEGraph,
        eclass: Id,
        subst: &Subst,
        searcher_ast: Option<&PatternAst<Expr>>,
        rule_name: Symbol,
    ) -> Vec<Id> {
        // println!("factors: {} {}", self.xFactor, self.yFactor);

        // Get the eclass, expression, and of the expressions relating to X.
        let x_id = subst["?x".parse().unwrap()];

        /*
        let x_factor_eclass = &egraph[subst[self.xFactor.parse().unwrap()]];
        assert_eq!(x_factor_eclass.nodes.len(), 1);
        let x_factor_expr = x_factor_eclass.nodes.first().unwrap();
        let x_factor_constant: i64 = match x_factor_expr {
            &Expr::Constant(def) => def,
            _ => panic!("factor must be constant!"),
        };
        */

        let x_factor_data = &egraph[subst[self.x_factor.parse().unwrap()]].data;
        let x_factor_constant: i64 = match x_factor_data {
            Some(c) => c.0,
            None => panic!("factor must be constant!"),
        };

        /*
        // Get the eclass, expression, and of the expressions relating to Y.
        let y_eclass = &egraph[subst["?y".parse().unwrap()]];
        assert_eq!(y_eclass.nodes.len(), 1);
        let y_id = y_eclass.id;
        let y_factor_eclass = &egraph[subst[self.yFactor.parse().unwrap()]];
        assert_eq!(y_factor_eclass.nodes.len(), 1);
        let y_factor_expr = y_factor_eclass.nodes.first().unwrap();
        let y_factor_constant: i64 = match y_factor_expr {
            &Expr::Constant(def) => def,
            _ => panic!("factor must be constant!"),
        };
        */

        // Get the eclass, expression, and of the expressions relating to X.
        let y_id = subst["?y".parse().unwrap()];

        /*
        let x_factor_eclass = &egraph[subst[self.xFactor.parse().unwrap()]];
        assert_eq!(x_factor_eclass.nodes.len(), 1);
        let x_factor_expr = x_factor_eclass.nodes.first().unwrap();
        let x_factor_constant: i64 = match x_factor_expr {
            &Expr::Constant(def) => def,
            _ => panic!("factor must be constant!"),
        };
        */

        let y_factor_data = &egraph[subst[self.y_factor.parse().unwrap()]].data;
        let y_factor_constant: i64 = match y_factor_data {
            Some(c) => c.0 .0,
            None => panic!("factor must be constant!"),
        };

        let min = x_factor_constant.min(y_factor_constant);
        let min_id = egraph.add(Expr::Constant(min));
        let max = x_factor_constant.max(y_factor_constant);

        // Here we're dealing with expressions like "4*x&4*y" and ""4*x&8*y",
        // where xFactor and yFactor are the constant multipliers.
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
            let remaining_factor = egraph.add(Expr::Constant((max / min)));

            // If x has the large factor then the RHS becomes ((max/min) * x) & y;
            let rhs: Id = if x_factor_constant == max {
                let x_times_remaining_factor = egraph.add(Expr::Mul(([remaining_factor, x_id])));
                let anded = egraph.add(Expr::And(([x_times_remaining_factor, y_id])));
                anded
            // If y has the large factor then the RHS becomes ((max/min) * y) & x;
            } else {
                let y_times_remaining_factor = egraph.add(Expr::Mul(([remaining_factor, y_id])));
                let anded = egraph.add(Expr::And(([y_times_remaining_factor, x_id])));
                anded
            };

            // Create the final expression of (min * factored_rhs);
            egraph.add(Expr::Mul([min_id, rhs]))
        };

        if egraph.union(eclass, factored) {
            return vec![factored];
        } else {
            return vec![];
        }
    }
}

impl Applier<Expr, ConstantFold> for DuplicateChildrenMulAddApplier {
    fn apply_one(
        &self,
        egraph: &mut EEGraph,
        eclass: Id,
        subst: &Subst,
        searcher_ast: Option<&PatternAst<Expr>>,
        rule_name: Symbol,
    ) -> Vec<Id> {
        let new_const_expr = &egraph[subst[self.const_factor.parse().unwrap()]].data;

        /*
        let constExpr = &egraph[subst[self.constFactor.parse().unwrap()]]
            .nodes
            .last()
            .unwrap();

        let constFactor: i64 = match constExpr {
            &&Expr::Constant(def) => def,
            _ => panic!("factor must be constant!"),
        };
        */

        let const_factor: i64 = match new_const_expr {
            Some(c) => c.0 .0,
            None => panic!("factor must be constant!"),
        };

        let original = &egraph[eclass];
        for pair in &original.nodes {
            // println!("original: {}", pair);
        }

        let x = subst[self.x_factor.parse().unwrap()];

        let new_const = egraph.add(Expr::Constant(const_factor + 1));
        let new_expr = egraph.add(Expr::Mul([new_const, x]));

        if egraph.union(eclass, new_expr) {
            return vec![new_expr];
        } else {
            return vec![];
        }

        //let b = subst[self.xFactor.parse().unwrap()];
        //let c = subst[self.constFactor.parse().unwrap()];
        panic!();

        println!(
            "before factoring: ({} * {}) + {}",
            const_factor, self.x_factor, self.x_factor
        );

        let factored = format!("(* {} {})", const_factor + 1, self.x_factor).replace("?", "");
        println!("factored: {}", factored);
        let parsed: RecExpr<Expr> = factored.parse().unwrap();
        println!("parsed: {}", parsed);
        println!("searcher ast: {}", searcher_ast.unwrap());
        let res = egraph.add_expr(&parsed);

        println!("equality: {} {}", res, eclass);
        let mut results: Vec<Id> = vec![];
        //egraph.union(eclass, res);
        results.push(res);
        results.push(eclass);
        return results;
        panic!("foobar!")
    }
}

fn var(s: &str) -> Symbol {
    s.parse().unwrap()
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
        rewrite!("mul-distributivity-expand"; "(* ?a (+ ?b ?c))" => "(+ (* ?a ?b) (* ?a ?c))"), // formally proved
        // Power rules
        rewrite!("power-zero"; "(** ?a 0)" => "1"),
        rewrite!("power-one"; "(** ?a 1)" => "?a"),
        // __check_duplicate_children
        rewrite!("expanded-add"; "(+ (* ?const ?x) ?x)" => {
            DuplicateChildrenMulAddApplier {
                const_factor : "?const".to_owned(),
                x_factor : "?x".to_owned(),
            }
        } if is_const("?const")),
        // ported rules:
        // __eliminate_nested_negations_advanced
        rewrite!("minus-twice"; "(* (* ?a -1) -1))" => "(?a)"), // formally proved
        rewrite!("negate-twice"; "(~ (~ ?a))" => "(?a)"),       // formally proved
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
                x_factor : "?factor1".to_owned(),
                y_factor : "?factor2".to_owned(),
            }
        } if (is_power_of_two("?factor1", "?factor2"))),
        // __check_beautify_constants_in_products: todo
        // __check_move_in_bitwise_negations
        rewrite!("and-move-bitwise-negation-in"; "(~ (& (~ ?a) ?b))" => "(| ?a (~ ?b))"), // formally proved
        rewrite!("or-move-bitwise-negation-in"; "(~ (| (~ ?a) ?b))" => "(& ?a (~ ?b))"), // formally proved
        rewrite!("xor-move-bitwise-negation-in"; "(~ (^ (~ ?a) ?b))" => "(^ ?a ?b)"), // formally proved
        // __check_bitwise_negations_in_excl_disjunctions
        rewrite!("xor-flip-negations"; "(^ (~ ?a) (~ ?b))" => "(^ ?a ?b)"), // formally proved
        // __check_rewrite_powers: todo
        // __check_resolve_product_of_powers
        // note: they say "Moreover merge factors that occur multiple times",
        // but I'm not sure what they mean
        rewrite!("merge-power-same-base"; "(* (** ?a ?b) (** ?a ?c))" => "(** ?a (+ ?b ?c))"),
        // __check_resolve_product_of_constant_and_sum
        //rewrite!("distribute-constant-to-sum"; "(* (+ ?a ?b) Constant)" => "(+ (* ?a Constant) (* ?b Constant))"),
        // __check_factor_out_of_sum
        rewrite!("factor"; "(+ (* ?a ?b) (* ?a ?c))" => "(* ?a (+ ?b ?c))"), // formally proved
        // __check_resolve_inverse_negations_in_sum
        rewrite!("invert-add-bitwise-not-self"; "(+ ?a (~ ?a))" => "-1"), // formally proved
        // formally proved
        rewrite!("invert-mul-bitwise-not-self"; "(+ (* ?a (~ ?b)) (* ?a ?b))" => "(* ?a -1)"), // formally proved
        // __insert_fixed_in_conj: todo
        // __insert_fixed_in_disj: todo
        // __check_trivial_xor: implemented above
        // __check_xor_same_mult_by_minus_one: todo
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
        // todo: see above
        // x|x-(x&y)
        rewrite!("disj_sub_disj_identity_rule_2"; "(| ?x (+ ?x (* (& ?x ?y) -1)))" => "?x"), // formally proved
        // __check_conj_add_conj_identity_rule
        // todo: see above
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
    ]
}

fn is_const(var: &str) -> impl Fn(&mut EEGraph, Id, &Subst) -> bool {
    let var = var.parse().unwrap();

    move |egraph, _, subst| {
        if let Some(c) = &egraph[subst[var]].data {
            //println!("CONST! {}", c.0);
            return true;
        } else {
            return false;
        };
    }
}

fn is_power_of_two(var: &str, var_to_str: &str) -> impl Fn(&mut EEGraph, Id, &Subst) -> bool {
    let var = var.parse().unwrap();
    let var2: Var = var_to_str.parse().unwrap();

    move |egraph, _, subst| {
        /* */
        let v1 = if let Some(c) = &egraph[subst[var]].data {
            c.0 .0 & (c.0 .0 - 1) == 0 && c.0 .0 != 0
        } else {
            false
        };

        let v2 = if let Some(c) = &egraph[subst[var2]].data {
            c.0 .0 & (c.0 .0 - 1) == 0 && c.0 .0 != 0
        } else {
            false
        };

        // println!("{}", &egraph[subst[var2]].nodes.len());
        let child = &egraph[subst[var]].nodes.first().unwrap();
        let child2 = &egraph[subst[var2]].nodes.first().unwrap();

        if let &&Expr::Constant(def) = child {
            //  println!("this is a constant!");
        } else {
            //  println!("this is not a constant! {}", child)
        }

        //println!("factor1: {}\nfactor2: {}", child, child2);

        //if let Some(c) = &egraph[subst[var2]] {
        //      println!("Somasde: {}", c.0);
        //  }

        //println!("is power of two! {} {}", var, var2);
        return v1 && v2;
    }
}

/// parse an expression, simplify it using egg, and pretty print it back out
fn simplify(s: &str) -> String {
    // parse the expression, the type annotation tells it which Language to use
    let expr: RecExpr<Expr> = s.parse().unwrap();

    // Create the runner. You can enable explain_equivalence to explain the equivalence,
    // but it comes at a severe performance penalty.
    let explain_equivalence = false;
    let mut runner: Runner<Expr, ConstantFold> = if !explain_equivalence {
        Runner::default()
            .with_time_limit(Duration::from_millis(1000))
            .with_expr(&expr)
    } else {
        Runner::default()
            .with_explanations_enabled()
            .with_expr(&expr)
    };

    let rules = make_rules();
    println!("made rules");

    let start = Instant::now();
    runner = runner.run(&rules);

    // the Runner knows which e-class the expression given with `with_expr` is in
    let root = runner.roots[0];

    // use an Extractor to pick the best element of the root eclass
    let extractor = Extractor::new(&runner.egraph, AstSize);
    let (best_cost, best) = extractor.find_best(root);
    let duration = start.elapsed();
    println!("Time elapsed in simplify() is: {:?}", duration);
    println!("Simplified {} to {} with  cost {}", expr, best, best_cost);

    if explain_equivalence {
        println!(
            "explained: {}",
            runner.explain_equivalence(&expr, &best).get_flat_string()
        );
    }

    best.to_string()
}

fn main() {
    // Get the program arguments.
    let mut args = std::env::args();

    // Skip the first argument since it's always the path to the current program.
    args.next();

    // Read the optional expression to simplify.
    let next = args.next();
    let expr = if next.is_some() {
        next.unwrap()
    } else {
        "(* y (+ (* (^ y x) -11) (+ (* (& x (~ y)) -19) (+ (* -36 (+ (& y x) (~ (| y x)))) (+ -31 (+ (* 6 (| y (~ x))) (* (& y (~ x)) -25)))))))".to_owned()
    };

    println!("Attempting to simplify expression: {}", expr);

    let mut simplified = simplify(expr.as_str());

    for _ in 0..0 {
        simplified = simplify(simplified.clone().as_ref());
    }

    println!("{}", simplified);
}
