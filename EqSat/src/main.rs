use std::time::{Duration, Instant};

use egg::*;

use crate::{const_fold::ConstantFold, cost::EGraphCostFn, rules::make_rules};

mod const_fold;
mod cost;
mod rules;

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

fn main() {
    // Get the program arguments.
    let mut args = std::env::args();

    // Skip the first argument since it's always the path to the current program.
    args.next();

    // Read the optional expression to simplify.
    let expr = if let Some(next) = args.next() {
        next
    } else {
        std::fs::read_to_string("test-input.txt").unwrap()
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

    #[test]
    fn and_not_plus_doesnt_hang_regression_test() {
        validate_output!("(& (~ (+ a b)) a)", "(& a (~ (+ a b)))");
    }
}
