use crate::{
    classification::{classify, AstClassification, ClassificationResult},
    cost::{get_cost, EGraphCostFn},
    simba, Expr,
};

use egg::*;
use expr_utils::{Ast, VariableNameInterner};

pub type EEGraph = egg::EGraph<Expr, ConstantFold>;
pub type Rewrite = egg::Rewrite<Expr, ConstantFold>;

fn build_expr(
    ast: Ast<'_>,
    expr: &mut RecExpr<ENodeOrVar<Expr>>,
    original_vars: &VariableNameInterner<'_>,
) -> Id {
    let node = ENodeOrVar::ENode(match ast {
        Ast::BinOp { lhs, op, rhs } => {
            let lhs = build_expr(*lhs, expr, original_vars);
            let rhs = build_expr(*rhs, expr, original_vars);

            match op {
                expr_utils::BinOp::Add => Expr::Add([lhs, rhs]),
                expr_utils::BinOp::Mul => Expr::Mul([lhs, rhs]),
                expr_utils::BinOp::Pow => Expr::Pow([lhs, rhs]),
                expr_utils::BinOp::Or => Expr::Or([lhs, rhs]),
                expr_utils::BinOp::And => Expr::And([lhs, rhs]),
                expr_utils::BinOp::Xor => Expr::Xor([lhs, rhs]),
            }
        }
        Ast::Not(node) => Expr::Neg([build_expr(*node, expr, original_vars)]),
        Ast::Variable(var) => Expr::Symbol(Symbol::new(original_vars.rename_var(var))),
        Ast::Constant(c) => Expr::Constant(wrap(c)),
    });

    expr.add(node)
}

pub fn simplify_expression_with_simba(
    egraph: &EEGraph,
    enode: &Expr,
    builder: &mut RecExpr<ENodeOrVar<Expr>>,
) -> Id {
    let classification = classify(egraph, enode).unwrap();

    if !matches!(
        classification.0,
        AstClassification::Nonlinear | AstClassification::Unknown
    ) {
        return simplify_linear_expression(egraph, enode, builder);
    }

    let f = |id| {
        let id = egraph.find(id);
        let nodes = &egraph[id].nodes;
        nodes
            .iter()
            .min_by_key(|node| get_cost(egraph, node))
            .unwrap()
    };

    match enode {
        Expr::Add([a, b])
        | Expr::Mul([a, b])
        | Expr::Pow([a, b])
        | Expr::And([a, b])
        | Expr::Or([a, b])
        | Expr::Xor([a, b]) => {
            let lhs = simplify_expression_with_simba(egraph, f(*a), builder);
            let rhs = simplify_expression_with_simba(egraph, f(*b), builder);

            let expr = match enode {
                Expr::Add(_) => Expr::Add([lhs, rhs]),
                Expr::Mul(_) => Expr::Mul([lhs, rhs]),
                Expr::Pow(_) => Expr::Pow([lhs, rhs]),
                Expr::And(_) => Expr::And([lhs, rhs]),
                Expr::Or(_) => Expr::Or([lhs, rhs]),
                Expr::Xor(_) => Expr::Xor([lhs, rhs]),
                _ => unreachable!(),
            };

            builder.add(ENodeOrVar::ENode(expr))
        }
        Expr::Neg([a]) => {
            let node = simplify_expression_with_simba(egraph, f(*a), builder);
            builder.add(ENodeOrVar::ENode(Expr::Neg([node])))
        }
        Expr::Constant(..) => unreachable!(),
        Expr::Symbol(..) => unreachable!(),
    }
}

fn simplify_linear_expression(
    egraph: &EEGraph,
    enode: &Expr,
    builder: &mut RecExpr<ENodeOrVar<Expr>>,
) -> Id {
    match enode {
        Expr::Constant(c) => return builder.add(ENodeOrVar::ENode(Expr::Constant(*c))),
        Expr::Symbol(c) => return builder.add(ENodeOrVar::ENode(Expr::Symbol(*c))),
        _ => {}
    }

    let original_ast = enode.to_ast(egraph);

    let mut solver = simba::Solver::new(original_ast.clone());
    let ast = solver.solve();

    let mut original_vars = solver.original_variables;
    original_vars.finish();

    build_expr(ast, builder, &original_vars)
}

#[derive(Default)]
pub struct ConstantFold;
impl Analysis<Expr> for ConstantFold {
    type Data = Option<ClassificationResult>;

    fn make(egraph: &EEGraph, enode: &Expr) -> Self::Data {
        let classification = classify(egraph, enode).unwrap();

        let cost = get_cost(egraph, enode);

        // If we classified the AST and returned a new PatternAst<Expr>, that means
        // constant folding succeeded. So now we return the newly detected
        // classification and the pattern ast
        if classification.1.is_some() {
            return Some(ClassificationResult::new(
                classification.0,
                classification.1,
                cost,
            ));
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
        Some(ClassificationResult::new(
            classification.0,
            classification.1,
            cost,
        ))
    }

    fn merge(&mut self, maybe_a: &mut Self::Data, maybe_b: Self::Data) -> DidMerge {
        merge_option(maybe_a, maybe_b, |_, _| DidMerge(false, false))
    }

    fn modify(egraph: &mut EEGraph, id: Id) {
        // TODO: Call egraph.union_instanations when with_explanations_enabled is set?
        if let Some(c) = &egraph[id].data {
            if let Some(new) = c.const_fold.clone() {
                let instantiation = egraph.add_instantiation(&new, &Default::default());
                egraph.union(id, instantiation);
            }
        }
    }
}

pub fn wrap(n: i128) -> i128 {
    n as i64 as i128
}

pub fn try_fold_constant(egraph: &EEGraph, enode: &Expr) -> Option<(i128, PatternAst<Expr>)> {
    let x = |i: &Id| {
        egraph[*i].data.as_ref().map(|c| match c.classification {
            AstClassification::Constant { value } => Some(wrap(value)),
            _ => None,
        })
    };

    Some(match enode {
        Expr::Constant(c) => {
            let msg = c.to_string().parse().unwrap();
            (wrap(*c), msg)
        }
        Expr::Add([a, b]) => {
            let val = x(a)??.wrapping_add(x(b)??);
            (wrap(val), val.to_string().parse().unwrap())
        }
        Expr::Mul([a, b]) => {
            let val = x(a)??.wrapping_mul(x(b)??);
            (wrap(val), val.to_string().parse().unwrap())
        }
        Expr::Pow([a, b]) => {
            let val = x(a)??.wrapping_pow(x(b)??.try_into().unwrap());
            (wrap(val), val.to_string().parse().unwrap())
        }
        Expr::And([a, b]) => {
            let val = x(a)?? & x(b)??;
            (wrap(val), val.to_string().parse().unwrap())
        }
        Expr::Or([a, b]) => {
            let val = x(a)?? | x(b)??;
            (wrap(val), val.to_string().parse().unwrap())
        }
        Expr::Xor([a, b]) => {
            let val = x(a)?? ^ x(b)??;
            (wrap(val), val.to_string().parse().unwrap())
        }
        Expr::Neg([a]) => {
            let val = !x(a)??;
            (wrap(val), val.to_string().parse().unwrap())
        }
        Expr::Symbol(_) => return None,
    })
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
