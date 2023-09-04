use core::{panic, time};
use std::time::Duration;

use egg::*;

pub type ApplierEGraph = egg::EGraph<Expr, BitwiseAnalysis>;
pub type ApplierREwrite = egg::Rewrite<Expr, BitwiseAnalysis>;

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
    xFactor: String,
    yFactor: String,
}

#[derive(Debug)]
pub struct DuplicateChildrenMulAddApplier {
    constFactor: String,
    xFactor: String,
}

impl Expr {
    pub fn num(&self) -> Option<i64> {
        match self {
            Expr::Constant(n) => Some(*n),
            _ => None,
        }
    }
}

#[derive(Default)]
pub struct ConstantFold;
impl Analysis<Expr> for ConstantFold {
    type Data = Option<(i64, PatternAst<Expr>)>;

    fn make(egraph: &EEGraph, enode: &Expr) -> Self::Data {
        let x = |i: &Id| egraph[*i].data.as_ref().map(|c| c.0);
        //println!("applying const prop to: {}", enode);
        Some(match enode {
            Expr::Constant(c) => {
                let msg = format!("{}", c).parse().unwrap();
                println!("constant const prop: {}", msg);
                (*c, msg)
            }
            Expr::Add([a, b]) => {
                let msg = format!("(+ {} {})", x(a)?, x(b)?).parse().unwrap();
                println!("add const prop: {}", msg);
                (x(a)? + x(b)?, msg)
            }
            Expr::Mul([a, b]) => (
                x(a)? * x(b)?,
                format!("(* {} {})", x(a)?, x(b)?).parse().unwrap(),
            ),
            Expr::Pow([a, b]) => (
                x(a)?.pow((x(b)?).try_into().unwrap()),
                format!("(** {} {})", x(a)?, x(b)?).parse().unwrap(),
            ),
            Expr::And([a, b]) => (
                x(a)? & x(b)?,
                format!("(& {} {})", x(a)?, x(b)?).parse().unwrap(),
            ),
            Expr::Or([a, b]) => (
                x(a)? | x(b)?,
                format!("(| {} {})", x(a)?, x(b)?).parse().unwrap(),
            ),
            Expr::Xor([a, b]) => (
                x(a)? ^ x(b)?,
                format!("(^ {} {})", x(a)?, x(b)?).parse().unwrap(),
            ),
            Expr::Neg([a]) => {
                println!("NEGATION: ~ {}", x(a)?);
                let msg = format!("(~ {})", x(a)?).parse().unwrap();
                println!("{}", msg);
                let result = (!x(a)?, msg);
                result
            }
            Expr::Symbol(_) => return None,
        })
    }

    fn merge(&mut self, to: &mut Self::Data, from: Self::Data) -> DidMerge {
        merge_option(to, from, |a, b| {
            assert_eq!(a.0, b.0, "Merged non-equal constants");
            DidMerge(false, false)
        })
    }

    fn modify(egraph: &mut EEGraph, id: Id) {
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
        println!("factors: {} {}", self.xFactor, self.yFactor);

        // Get the eclass, expression, and of the expressions relating to X.
        let x_eclass = &egraph[subst["?x".parse().unwrap()]];
        assert_eq!(x_eclass.nodes.len(), 1);
        let x_id = x_eclass.id;
        let x_expr = x_eclass.nodes.first().unwrap();

        let x_factor_eclass = &egraph[subst[self.xFactor.parse().unwrap()]];
        assert_eq!(x_factor_eclass.nodes.len(), 1);
        let x_factor_expr = x_factor_eclass.nodes.first().unwrap();
        let x_factor_constant: i64 = match x_factor_expr {
            &Expr::Constant(def) => def,
            _ => panic!("factor must be constant!"),
        };

        // Get the eclass, expression, and of the expressions relating to Y.
        let y_eclass = &egraph[subst["?y".parse().unwrap()]];
        assert_eq!(y_eclass.nodes.len(), 1);
        let y_id = y_eclass.id;
        let y_expr = x_eclass.nodes.first().unwrap();
        let y_factor_eclass = &egraph[subst[self.yFactor.parse().unwrap()]];
        assert_eq!(y_factor_eclass.nodes.len(), 1);
        let y_factor_expr = y_factor_eclass.nodes.first().unwrap();
        let y_factor_constant: i64 = match y_factor_expr {
            &Expr::Constant(def) => def,
            _ => panic!("factor must be constant!"),
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

        if (egraph.union(eclass, factored)) {
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
        let nodes = &egraph[subst[self.constFactor.parse().unwrap()]].nodes;
        let nodes2 = &egraph[subst[self.xFactor.parse().unwrap()]].nodes;

        println!("node lengths: {} {}", nodes.len(), nodes2.len());

        let newConstExpr = &egraph[subst[self.constFactor.parse().unwrap()]].data;

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

        let constFactor: i64 = match newConstExpr {
            Some(c) => c.0,
            None => panic!("factor must be constant!"),
        };

        let original = &egraph[eclass];
        for pair in &original.nodes {
            println!("original: {}", pair);
        }

        let x = subst[self.xFactor.parse().unwrap()];

        let newConst = egraph.add(Expr::Constant(constFactor + 1));
        let newExpr = egraph.add(Expr::Mul([newConst, x]));

        if (egraph.union(eclass, newExpr)) {
            return vec![newExpr];
        } else {
            return vec![];
        }

        //let b = subst[self.xFactor.parse().unwrap()];
        //let c = subst[self.constFactor.parse().unwrap()];
        panic!();

        println!(
            "before factoring: ({} * {}) + {}",
            constFactor, self.xFactor, self.xFactor
        );

        let factored = format!("(* {} {})", constFactor + 1, self.xFactor).replace("?", "");
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
                constFactor : "?const".to_owned(),
                xFactor : "?x".to_owned(),
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
            xFactor : "?factor1".to_owned(),
            yFactor : "?factor2".to_owned(),
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
        rewrite!("invert-mul-bitwise-not-self"; "(+ (* ?a (~ ?b)) (* ?a ?b))" => "(* ?a -1)"), // formally proved
                                                                                               // __insert_fixed_in_conj: todo
                                                                                               // __insert_fixed_in_disj: todo
                                                                                               // __check_trivial_xor: implemented above
    ]
}

fn is_const(var: &str) -> impl Fn(&mut EEGraph, Id, &Subst) -> bool {
    let var = var.parse().unwrap();

    move |egraph, _, subst| {
        if let Some(c) = &egraph[subst[var]].data {
            println!("CONST! {}", c.0);
            return true;
        } else {
            return false;
        };
    }
}

fn is_power_of_two(var: &str, var2Str: &str) -> impl Fn(&mut EEGraph, Id, &Subst) -> bool {
    let var = var.parse().unwrap();
    let var2: Var = var2Str.parse().unwrap();

    move |egraph, _, subst| {
        /* */
        let v1 = if let Some(c) = &egraph[subst[var]].data {
            c.0 & (c.0 - 1) == 0 && c.0 != 0
        } else {
            false
        };

        let v2 = if let Some(c) = &egraph[subst[var2]].data {
            c.0 & (c.0 - 1) == 0 && c.0 != 0
        } else {
            false
        };

        // println!("{}", &egraph[subst[var2]].nodes.len());
        let child = &egraph[subst[var]].nodes.first().unwrap();
        let child2 = &egraph[subst[var2]].nodes.first().unwrap();

        match child {
            Expr::Add(_) => println!("add"),
            Expr::Mul(_) => println!("Mul"),
            Expr::Pow(_) => println!("Pow"),
            Expr::And(_) => println!("And"),
            Expr::Or(_) => println!("Or"),
            Expr::Xor(_) => println!("Xor"),
            Expr::Neg(_) => println!("Not"),
            Expr::Symbol(_) => println!("Symbol"),
            Expr::Constant(_) => println!("Constant"),
        }

        if let &&Expr::Constant(def) = child {
            println!("this is a constant!");
        } else {
            println!("this is not a constant! {}", child)
        }

        println!("factor1: {}\nfactor2: {}", child, child2);

        //if let Some(c) = &egraph[subst[var2]] {
        //      println!("Somasde: {}", c.0);
        //  }

        println!("is power of two! {} {}", var, var2);
        return v1 && v2;
    }
}

/// parse an expression, simplify it using egg, and pretty print it back out
fn simplify(s: &str) -> String {
    // parse the expression, the type annotation tells it which Language to use
    let expr: RecExpr<Expr> = s.parse().unwrap();

    // Create the runner. You can enable explain_equivalence to explain the equivalence,
    // but it comes at a severe performance penalty.
    let explain_equivalence = true;
    let mut runner: Runner<Expr, ConstantFold> = if !explain_equivalence {
        Runner::default()
            .with_time_limit(Duration::from_millis(100))
            .with_expr(&expr)
    } else {
        Runner::default()
            .with_explanations_enabled()
            .with_expr(&expr)
    };

    let rules = make_rules();
    println!("made rules");
    runner = runner.run(&rules);

    // the Runner knows which e-class the expression given with `with_expr` is in
    let root = runner.roots[0];

    // use an Extractor to pick the best element of the root eclass
    let extractor = Extractor::new(&runner.egraph, AstSize);
    let (best_cost, best) = extractor.find_best(root);
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
        "(^ (^ (~ x) (~ y)) z)".to_owned()
    };

    println!("Attempting to simplify expression: {}", expr);

    let simplified = simplify(expr.as_str());
    println!("{}", simplified);
}
