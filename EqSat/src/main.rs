use core::panic;
use std::time::{Duration, Instant};

use egg::*;

type Cost = i64;

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
            (x(a)??.wrapping_add(x(b)??), msg)
        }
        Expr::Mul([a, b]) => (
            x(a)??.wrapping_mul(x(b)??),
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
        let unwrapped = const_folded.unwrap();
        return Some((
            AstClassification::Constant {
                value: (unwrapped.0),
            },
            Some(unwrapped.1),
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
            panic!()
        }
        Expr::Neg([a]) => {
            let result = classify_bitwise(egraph, op(a)?, None);
            result
        }
        Expr::And([a, b]) => classify_bitwise(egraph, op(a)?, Some(op(b)?)),
        Expr::Or([a, b]) => {
            /*
            println!("here comes an OR!");
            print(egraph, enode);

            let a = op(a)?;
            println!("Classification of a: {:#?}", a);

            let b = op(b)?;
            println!("Classification of b: {:#?}", b);

            let result = classify_bitwise(egraph, a, Some(b));
            println!("Classification of or^: {:#?}", result);
            result
            */
            classify_bitwise(egraph, op(a)?, Some(op(b)?))
        }
        Expr::Xor([a, b]) => classify_bitwise(egraph, op(a)?, Some(op(b)?)),
    };

    return Some((result, None));
}

fn classify_bitwise(
    egraph: &EEGraph,
    a: AstClassification,
    b: Option<AstClassification>,
) -> AstClassification {
    // TODO: Throw if we see negation with a constant, that should be fixed.

    let mut maybe_b: AstClassification = a;

    /*
    let mut children = if b.is_some() {
        maybe_b = b.unwrap().clone();
        [a, maybe_b]
    } else {
        [a]
    };
    */

    let mut childrens = if b.is_some() {
        maybe_b = b.unwrap().clone();
        vec![a, maybe_b]
    } else {
        vec![a]
    };

    let mut children = childrens;
    //println!("child count: {}", children.len());

    // Check if the expression contains constants or arithmetic expressions.
    let containsConstantOrArithmetic = children.iter().any(|x| match x {
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
    let containsMixedOrNonLinear = children.iter().any(|x| match x {
        AstClassification::Mixed => true,
        AstClassification::Nonlinear => true,
        _ => false,
    });

    // Bitwise expressions are considered to be nonlinear if they contain constants,
    // arithmetic(linear) expressions, or non linear subexpressions.
    if containsConstantOrArithmetic || containsMixedOrNonLinear {
        return AstClassification::Nonlinear;
    } else if children.iter().any(|x: &AstClassification| match x {
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
    /*
    println!(
        "{} {}",
        containsConstantOrArithmetic, containsMixedOrNonLinear
    );
    */

    for child in children {
        //println!("children: {:#?}", child);
    }

    //panic!("oh no!");
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
        AstClassification::Constant { value } => 10,
        AstClassification::Linear { is_variable } => {
            if is_variable {
                10
            } else {
                100
            }
        }
        AstClassification::Bitwise => 125,
        AstClassification::Mixed => 150,
        AstClassification::Nonlinear => 300,
        AstClassification::Unknown => panic!("Hopefully this never happens?"),
    }
}

fn get_cost(egraph: &EEGraph, enode: &Expr) -> Cost {
    // TODO: Remove clone
    let op = |i: &Id| egraph[*i].data.clone().unwrap().2;
    match enode {
        Expr::Add([a, b]) => op(a) + op(b),
        Expr::Mul([a, b]) => op(a) + op(b),
        Expr::Pow([a, b]) => op(a) + op(b),
        Expr::And([a, b]) => op(a) + op(b),
        Expr::Or([a, b]) => op(a) + op(b),
        Expr::Xor([a, b]) => op(a) + op(b),
        Expr::Neg([a]) => op(a),
        Expr::Constant(_) => 1,
        Expr::Symbol(_) => 1,
    }
}

fn print(egraph: &EEGraph, expr: &Expr) {
    let cost_func = EGraphCostFn { egraph: egraph };

    let extractor = Extractor::new(&egraph, cost_func);

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
        // Classify the expression. Then throw if it doesn't have a classification - that should never happen.
        let maybe_classification = classify(egraph, enode);
        if (maybe_classification.is_none()) {
            panic!("Classifications cannot be none!");
        }

        let mut cost = get_cost(egraph, enode);

        // If we classified the AST and returned a new PatternAst<Expr>, that means constant
        // folding succeed. So now we return the newly detected classification and the pattern ast.
        let classification = maybe_classification.unwrap();
        if (classification.1.is_some()) {
            return Some((classification.0, classification.1, cost));
        }

        let str = enode.to_string();
        if (cost > 30000) {
            match classification.0 {
                AstClassification::Unknown => panic!(),
                AstClassification::Constant { value } => (),
                AstClassification::Bitwise => {
                    print!("bitwse subexpression: ");
                    match enode {
                        Expr::Add([a, b]) => print(egraph, enode),
                        Expr::Mul(_) => print(egraph, enode),
                        Expr::Pow(_) => print(egraph, enode),
                        Expr::And(_) => print(egraph, enode),
                        Expr::Or(_) => print(egraph, enode),
                        Expr::Xor(_) => print(egraph, enode),
                        Expr::Neg(_) => print(egraph, enode),
                        Expr::Constant(_) => (),
                        Expr::Symbol(_) => (),
                    }
                }
                AstClassification::Linear { is_variable } => {
                    print!("linear subexpression: ");
                    match enode {
                        Expr::Add([a, b]) => print(egraph, enode),
                        Expr::Mul(_) => print(egraph, enode),
                        Expr::Pow(_) => print(egraph, enode),
                        Expr::And(_) => print(egraph, enode),
                        Expr::Or(_) => print(egraph, enode),
                        Expr::Xor(_) => print(egraph, enode),
                        Expr::Neg(_) => print(egraph, enode),
                        Expr::Constant(_) => (),
                        Expr::Symbol(_) => (),
                    }
                }
                AstClassification::Nonlinear => {
                    //print!("nonlinear subexpression: ");
                    /*
                    match enode {
                        Expr::Add([a, b]) => print(egraph, enode),
                        Expr::Mul(_) => print(egraph, enode),
                        Expr::Pow(_) => print(egraph, enode),
                        Expr::And(_) => print(egraph, enode),
                        Expr::Or(_) => print(egraph, enode),
                        Expr::Xor(_) => print(egraph, enode),
                        Expr::Neg(_) => print(egraph, enode),
                        Expr::Constant(_) => (),
                        Expr::Symbol(_) => (),
                    }
                    */
                }
                AstClassification::Mixed => {
                    print!("mixed subexpression: ");
                    match enode {
                        Expr::Add([a, b]) => print(egraph, enode),
                        Expr::Mul(_) => print(egraph, enode),
                        Expr::Pow(_) => print(egraph, enode),
                        Expr::And(_) => print(egraph, enode),
                        Expr::Or(_) => print(egraph, enode),
                        Expr::Xor(_) => print(egraph, enode),
                        Expr::Neg(_) => print(egraph, enode),
                        Expr::Constant(_) => (),
                        Expr::Symbol(_) => (),
                    }

                    //let best = extractor.find_best(node)
                    match enode {
                        Expr::Add([a, b]) => print(egraph, enode),
                        Expr::Mul(_) => print(egraph, enode),
                        Expr::Pow(_) => print(egraph, enode),
                        Expr::And(_) => print(egraph, enode),
                        Expr::Or(_) => print(egraph, enode),
                        Expr::Xor(_) => print(egraph, enode),
                        Expr::Neg(_) => print(egraph, enode),
                        Expr::Constant(_) => (),
                        Expr::Symbol(_) => (),
                    }
                    //panic!();
                }
            }
        }

        // Otherwise we've classified the AST but there was no constant folding to be performed.
        return Some((classification.0, classification.1, get_cost(egraph, enode)));
    }

    fn merge(&mut self, maybeA: &mut Self::Data, maybeB: Self::Data) -> DidMerge {
        /*
        let mut a = maybeA.to_owned().unwrap();
        let mut b = maybeB.to_owned().unwrap();

        let classification_a_cost = get_classification_cost(a.0);
        let classification_b_cost = get_classification_cost(b.0);

        let mut changedA = false;
        let mut changedB = false;
        if (classification_a_cost > classification_b_cost) {
            a.0 = b.0;
            changedA = true;
        } else if (classification_b_cost > classification_a_cost) {
            b.0 = a.0;
            changedB = true;
        }
        */

        return merge_option(maybeA, maybeB, |maybeA, maybeB| DidMerge(false, false));

        // TODO: Not sure how merging is supposed to work?
        let mut did_merge = DidMerge(false, false);

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
        // TODO: Call egraph.union_instanations when with_explanations_enabled is set?
        if let Some(c) = egraph[id].data.clone() {
            if let Some(new) = &c.1 {
                /*
                egraph.un(
                    &c.1,
                    &c.0.to_string().parse().unwrap(),
                    &Default::default(),
                    "analysis".to_string(),
                );
                */

                let instantiation = egraph.add_instantiation(new, &Default::default());
                egraph.union(id, instantiation);
            }
        }
    }
}

fn read_constant(
    data: &Option<(AstClassification, Option<PatternAst<Expr>>, Cost)>,
) -> Option<i64> {
    if (data.is_none()) {
        return None;
    }

    let classification = data.as_ref().unwrap().0;
    match classification {
        AstClassification::Constant { value } => return Some(value),
        _ => return None,
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
        let x_factor_constant: i64 = match read_constant(x_factor_data) {
            Some(c) => c,
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
        let y_factor_constant: i64 = read_constant(y_factor_data).unwrap();

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

        let const_factor: i64 = read_constant(new_const_expr).unwrap();
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
        // Additional rules:
        rewrite!("mba-1"; "(+ ?d (* (* 1 -1) (& ?d ?a)))" => "(& (~ ?a) ?d)"),
        rewrite!("mba-2"; "(+ (* (* 1 -1) (& ?d ?a)) ?d)" => "(& (~ ?a) ?d)"),
        rewrite!("mba-3"; "(+ (+ ?a (* 2 -1)) (* (* 2 -1) ?d))" => "(+ (+ (* 2 -1) ?a) (* (* 2 ?d) -1))"),
        rewrite!("mba-4"; "(+ (| ?d ?a) (* (* 1 -1) (& ?a (~ ?d))))" => "?d"),
        rewrite!("mba-5"; "(+ (* (* 1 -1) (& ?a (~ ?d))) (| ?d ?a))" => "?d"),
        rewrite!("mba-6"; "(+ (& ?d ?a) (& ?a (~ ?d)))" => "?a"),
        rewrite!("mba-7"; "(+ (& ?a (~ ?d)) (& ?d ?a))" => "?a"),
        rewrite!("mba-8"; "(+ (* (& ?d (* ?a ?d)) (| ?d (* ?a ?d))) (* (& (~ ?d) (* ?a ?d)) (& ?d (~ (* ?a ?d)))))" => "(* (** ?d 2) ?a)"),
        rewrite!("mba-9"; "(+ (+ ?a (* -2 ?d)) (* 2 (& (~ ?a) (* 2 ?d))))" => "(^ ?a (* 2 ?d))"),
        rewrite!("mba-10"; "(~ (* ?x ?y))" => "(+ (* (~ ?x) ?y) (+ ?y (* 1 -1)))"),
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
        rewrite!("mba-33"; "(+ (& ?a48 (| (~ ?a21) (~ ?a46))) (+ (& (~ ?a48) (~ (^ ?a21 ?a46))) (| (~ ?a48) (| ?a21 ?a46))))" => "(+ (* 2 -1) (* (* 1 -1) (^ ?a21 (^ ?a46 ?a48))))"),
        rewrite!("mba-34"; "(+ (^ ?a48 (^ ?a21 ?a46)) (+ (& ?a48 (| (~ ?a21) (~ ?a46))) (+ (& (~ ?a48) (~ (^ ?a21 ?a46))) (| (~ ?a48) (| ?a21 ?a46)))))" => "(* 2 -1)"),
    ]
}

fn is_const(var: &str) -> impl Fn(&mut EEGraph, Id, &Subst) -> bool {
    let var = var.parse().unwrap();

    move |egraph, _, subst| {
        if let Some(c) = read_constant(&egraph[subst[var]].data) {
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

fn print_recexpr(
    egraph: &mut EEGraph,
    expr: RecExpr<Expr>,
    is_first: bool,
    runner: &Extractor<'_, EGraphCostFn<'_>, Expr, ConstantFold>,
) {
    let nodes = expr.as_ref();
    let last = nodes.last();
    println!("{}", nodes.len());
    visit_all(egraph, last.unwrap(), runner);
    todo!()
}

fn visit_all(
    egraph: &mut EEGraph,
    expr: &Expr,
    runner: &Extractor<'_, EGraphCostFn<'_>, Expr, ConstantFold>,
) {
    let added = egraph.add(expr.clone());
    let classification = egraph[added].data.as_ref().unwrap().0;
    println!("{:#?}: {}", classification, expr);

    let mut x = |id: &Id| print_recexpr(egraph, runner.find_best(*id).1, true, runner);
    match expr {
        Expr::Add([a, b]) => {
            x(a);
            x(b)
        }
        Expr::Mul([a, b]) => {
            x(a);
            x(b)
        }
        Expr::Pow([a, b]) => {
            x(a);
            x(b)
        }
        Expr::And([a, b]) => {
            x(a);
            x(b)
        }
        Expr::Or([a, b]) => {
            x(a);
            x(b)
        }
        Expr::Xor([a, b]) => {
            x(a);
            x(b)
        }
        Expr::Neg([a]) => {
            x(a);
        }
        Expr::Constant(def) => {}
        Expr::Symbol(_) => {}
    }
}

/// parse an expression, simplify it using egg, and pretty print it back out
fn simplify(s: &str, optimize_for_linearity: bool) -> String {
    // parse the expression, the type annotation tells it which Language to use
    let expr: RecExpr<Expr> = s.parse().unwrap();

    // Create the runner. You can enable explain_equivalence to explain the equivalence,
    // but it comes at a severe performance penalty.
    let explain_equivalence = false;
    let mut runner: Runner<Expr, ConstantFold> = if !explain_equivalence {
        Runner::default()
            .with_time_limit(Duration::from_millis(5000))
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

    if optimize_for_linearity {
        // use an Extractor to pick the best element of the root eclass
        let cost_func = EGraphCostFn {
            egraph: &runner.egraph,
        };

        let extractor = Extractor::new(&runner.egraph, cost_func);
        let (best_cost, best) = extractor.find_best(root);
        let classification = &runner.egraph[root].data.clone().unwrap().0;

        let duration = start.elapsed();
        println!("Time elapsed in simplify() is: {:?}", duration);

        println!(
            "Simplified {} \n\nto:\n{}\n with cost {}\n\n",
            expr, best, best_cost
        );

        dbg!(classification);

        //print_recexpr(&mut runner.egraph, best.clone(), true, &extractor);

        best.to_string()
    } else {
        // use an Extractor to pick the best element of the root eclass
        let cost_func = AstSize;

        let extractor = Extractor::new(&runner.egraph, AstSize);
        let (best_cost, best) = extractor.find_best(root);
        let classification = &runner.egraph[root].data.clone().unwrap().0;

        let duration = start.elapsed();
        println!("Time elapsed in simplify() is: {:?}", duration);

        println!("Simplified {} to {} with cost {}", expr, best, best_cost);

        dbg!(classification);

        best.to_string()
    }

    /*
    if explain_equivalence {
        println!(
            "explained: {}",
            runner.explain_equivalence(&expr, &best).get_flat_string()
        );
    }
    */
}

struct EGraphCostFn<'a> {
    egraph: &'a EGraph<Expr, ConstantFold>,
}

impl<'a> CostFunction<Expr> for EGraphCostFn<'a> {
    type Cost = usize;
    fn cost<C>(&mut self, enode: &Expr, mut costs: C) -> Self::Cost
    where
        C: FnMut(Id) -> Self::Cost,
    {
        return get_cost(self.egraph, enode) as usize;
    }
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
        if i % 2 == 0 {
            simplified = simplify(simplified.clone().as_ref(), true);
        } else {
            simplified = simplify(simplified.clone().as_ref(), false);
        }
    }

    simplified = simplify(simplified.clone().as_ref(), false);
    simplified = simplify(simplified.clone().as_ref(), false);
    println!("{}", simplified);
}
