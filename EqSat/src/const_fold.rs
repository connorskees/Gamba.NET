use crate::{
    cost::{get_cost, Cost, EGraphCostFn},
    AstClassification, Expr,
};

use egg::*;

pub type EEGraph = egg::EGraph<Expr, ConstantFold>;
pub type Rewrite = egg::Rewrite<Expr, ConstantFold>;

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
