use std::time::{Duration, Instant};

use const_fold::EEGraph;
use egg::*;
use expr_utils::{Ast, GambaExpressionPrinter, SExpressionPrinter};

use crate::{
    const_fold::{simplify_expression_with_simba, ConstantFold},
    cost::{get_cost, EGraphCostFn},
    rules::make_rules,
};

mod bitwise_list;
mod classification;
mod const_fold;
mod cost;
mod rules;
mod simba;

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
        Constant(i128),             // (int)
        Symbol(Symbol),             // (x)
    }
}

impl Expr {
    pub fn to_ast<'a>(&'a self, egraph: &'a EEGraph) -> Ast<'a> {
        let f = |id| {
            let id = egraph.find(id);
            let nodes = &egraph[id].nodes;
            nodes
                .iter()
                .min_by_key(|node| get_cost(egraph, node))
                .unwrap()
        };

        match self {
            Expr::Add([a, b]) => Ast::new_binop(
                f(*a).to_ast(egraph),
                expr_utils::BinOp::Add,
                f(*b).to_ast(egraph),
            ),
            Expr::Mul([a, b]) => Ast::new_binop(
                f(*a).to_ast(egraph),
                expr_utils::BinOp::Mul,
                f(*b).to_ast(egraph),
            ),
            Expr::Pow([a, b]) => Ast::new_binop(
                f(*a).to_ast(egraph),
                expr_utils::BinOp::Pow,
                f(*b).to_ast(egraph),
            ),
            Expr::And([a, b]) => Ast::new_binop(
                f(*a).to_ast(egraph),
                expr_utils::BinOp::And,
                f(*b).to_ast(egraph),
            ),
            Expr::Or([a, b]) => Ast::new_binop(
                f(*a).to_ast(egraph),
                expr_utils::BinOp::Or,
                f(*b).to_ast(egraph),
            ),
            Expr::Xor([a, b]) => Ast::new_binop(
                f(*a).to_ast(egraph),
                expr_utils::BinOp::Xor,
                f(*b).to_ast(egraph),
            ),
            Expr::Neg([a]) => Ast::Not(Box::new(f(*a).to_ast(egraph))),
            Expr::Constant(a) => Ast::Constant(*a as i64 as i128),
            Expr::Symbol(a) => Ast::Variable(a.as_str()),
        }
    }

    pub fn num(&self) -> Option<i128> {
        match self {
            Expr::Constant(n) => Some(*n),
            _ => None,
        }
    }
}

/// parse an expression, simplify it using egg, and pretty print it back out
fn simplify(s: &str, optimize_for_linearity: bool, use_simba: bool) -> String {
    let expr: RecExpr<Expr> = s.parse().unwrap();

    // Create the runner. You can enable explain_equivalence to explain the equivalence,
    // but it comes at a severe performance penalty.
    let explain_equivalence = false;
    let mut runner: Runner<Expr, ConstantFold> = Runner::default()
        .with_time_limit(Duration::from_millis(5000))
        .with_node_limit(u32::MAX as usize)
        .with_iter_limit(u16::MAX as usize);

    if explain_equivalence {
        runner = runner.with_explanations_enabled();
    }

    runner = runner.with_expr(&expr);

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

    let best = if use_simba {
        let mut egraph = EEGraph::new(ConstantFold);

        for expr in best.as_ref() {
            egraph.add(expr.clone());
        }

        let mut simba_best = RecExpr::default();
        simplify_expression_with_simba(&egraph, best.as_ref().last().unwrap(), &mut simba_best);

        simba_best.to_string()
    } else {
        best.to_string()
    };

    let duration = start.elapsed();
    println!("Time elapsed in simplify() is: {:?}", duration);

    println!(
        "Simplified {} \n\nto:\n{}\n with cost {}\n\n",
        expr, best, best_cost
    );

    best
}

fn read_expr_from_args() -> String {
    let mut args = std::env::args().skip(1);

    if let Some(next) = args.next() {
        next
    } else {
        std::fs::read_to_string("test-input.txt").unwrap()
    }
}

fn main() {
    let expr = read_expr_from_args();
    println!("Attempting to simplify expression: {}", expr);

    let simplified = simplify(&expr, false, false);

    println!("{}", simplified);
}

/// Run SiMBA simplification on the given expression. It is the responsibility
/// of the caller to ensure the expression is linear.
#[allow(unused)]
fn run_simba(expr: &str) {
    let start = Instant::now();
    let ast = expr_utils::s_expression::AstParser::new()
        .parse(expr)
        .unwrap();
    let mut solver = simba::Solver::new(ast);
    let ast = solver.solve();
    let printed = GambaExpressionPrinter::print(&ast);

    let end = start.elapsed();

    println!(
        "{} in {end:?}",
        solver.original_variables.replace_vars(printed)
    );
}

/// Test the output of every expression in a given GAMBA or SiMBA data file
#[allow(unused)]
fn test_input_file(path: &str) {
    let input = std::fs::read_to_string(path).unwrap();

    let mut total = 0;
    let mut wrong = 0;
    for line in input.lines() {
        total += 1;
        let expr = line.split(',').next().unwrap();
        let ast = expr_utils::gamba_expression::AstParser::new()
            .parse(expr)
            .unwrap();
        let line = SExpressionPrinter::print(&ast);
        let printed = simplify(&line, false, false);

        if printed != "(49374 + 3735936685*(x^y))" {
            wrong += 1;
        }
        println!("{printed}");
    }
    println!("{wrong}/{total}");
}

#[cfg(test)]
mod test {
    use crate::simplify;

    macro_rules! validate_output {
        ($input:expr, $output:expr) => {
            assert_eq!(simplify($input, false, false), $output)
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

    #[test]
    fn and_not_plus_doesnt_hang_regression_test() {
        validate_output!("(& (~ (+ a b)) a)", "(& a (~ (+ a b)))");
    }

    #[test]
    fn distributes_not() {
        validate_output!("(~ (| x (~ y)))", "(& y (~ x))");
    }

    #[test]
    fn foo() {
        validate_output!("(+ x (& y (~ x)))", "(| x y)");
    }
}
