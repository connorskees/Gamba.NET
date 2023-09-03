use core::panic;

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
        "-" = UnaryMinus([Id; 1]), // (- a)
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

#[derive(Default)]
pub struct ConstantFold;
impl Analysis<Expr> for ConstantFold {
    type Data = Option<(i64, PatternAst<Expr>)>;

    fn make(egraph: &EEGraph, enode: &Expr) -> Self::Data {
        let x = |i: &Id| egraph[*i].data.as_ref().map(|d| d.0);
        Some(match enode {
            Expr::Constant(c) => (*c, format!("{}", c).parse().unwrap()),
            Expr::Add([a, b]) => (
                x(a)? + x(b)?,
                format!("(+ {} {})", x(a)?, x(b)?).parse().unwrap(),
            ),
            Expr::Mul([a, b]) => (
                x(a)? * x(b)?,
                format!("(* {} {})", x(a)?, x(b)?).parse().unwrap(),
            ),
            Expr::UnaryMinus([a]) => (0 - x(a)?, format!("(- {})", x(a)?).parse().unwrap()),
            Expr::Pow([a, b]) => (
                x(a)?.pow(x(b)?.try_into().unwrap()),
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
            Expr::Neg([a]) => (!x(a)?, format!("(~ {})", x(a)?).parse().unwrap()),
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
        let data = egraph[id].data.clone();
        if let Some((c, pat)) = data {
            if egraph.are_explanations_enabled() {
                egraph.union_instantiations(
                    &pat,
                    &format!("{}", c).parse().unwrap(),
                    &Default::default(),
                    "constant_fold".to_string(),
                );
            } else {
                let added = egraph.add(Expr::Constant(c));
                egraph.union(id, added);
            }
            // to not prune, comment this out
            egraph[id].nodes.retain(|n| n.is_leaf());

            #[cfg(debug_assertions)]
            egraph[id].assert_unique_leaves();
        }
    }
}

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

        let xExpr = &egraph[subst[self.xFactor.parse().unwrap()]]
            .nodes
            .first()
            .unwrap();

        let yExpr = &egraph[subst[self.yFactor.parse().unwrap()]]
            .nodes
            .first()
            .unwrap();

        let xFactor: i64 = match xExpr {
            &&Expr::Constant(def) => def,
            _ => panic!("factor must be constant!"),
        };

        let yFactor: i64 = match yExpr {
            &&Expr::Constant(def) => def,
            _ => panic!("factor must be constant!"),
        };

        let min = xFactor.min(yFactor);
        let max = xFactor.max(yFactor);

        // Here we're dealing with expressions like "4*x&4*y" and ""4*x&8*y",
        // where xFactor and yFactor are the constant multipliers.
        // If the constant multipliers are the same, then for example
        // we can factor `4*x&4*y` into `4*(x&y)`.
        let factored = if min == max {
            format!("(* {} (& ?x ?y))", min)
        }
        // If the factors are not equal(e.g. "4*x&8*y"), then we need to factor
        // out only the minimum factor, giving us something like "4*(x&2*y)".
        else {
            // If x has the larger factor then the expression becomes min * ((max/min) * x) & y)
            if xFactor == max {
                format!("(* {} (& (* {} ?x) ?y))", min, max / min)
            }
            // // If y has the larger factor then the expression becomes min * ((max/min) * y) & x)
            else {
                //println!("foobar: * {} (& (* {} ?y) ?x)", min, max / min);
                format!("(* {} (& (* {} ?y) ?x))", min, max / min)
            }
        };

        let parsed: RecExpr<Expr> = factored.parse().unwrap();
        let res = egraph.add_expr(&parsed);

        let mut results: Vec<Id> = vec![];
        egraph.union(eclass, res);
        results.push(res);
        results.push(eclass);
        return results;
        panic!()
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
        rewrite!("or-negated-itself"; "(| ?a (~ a))" => "-1"),
        rewrite!("or-commutativity"; "(| ?a ?b)" => "(| ?b ?a)"),
        rewrite!("or-associativity"; "(| ?a (| ?b ?c))" => "(| (| ?a ?b) ?c)"),
        // Xor rules
        rewrite!("xor-zero"; "(^ ?a 0)" => "?a"),
        rewrite!("xor-maxint"; "(^ ?a -1)" => "(~ ?a)"),
        rewrite!("xor-itself"; "(^ ?a ?a)" => "0"),
        rewrite!("xor-commutativity"; "(^ ?a ?b)" => "(^ ?b ?a)"),
        rewrite!("xor-associativity"; "(^ ?a (^ ?b ?c))" => "(^ (^ ?a ?b) ?c)"),
        // And rules
        rewrite!("and-zero"; "(& ?a 0)" => "0"),
        rewrite!("and-maxint"; "(& ?a -1)" => "?a"),
        rewrite!("and-itself"; "(& ?a ?a)" => "?a"),
        rewrite!("and-negated-itself"; "(& ?a (~ ?a))" => "0"),
        rewrite!("and-commutativity"; "(& ?a ?b)" => "(& ?b ?a)"),
        rewrite!("and-associativity"; "(& ?a (& ?b ?c))" => "(& (& ?a ?b) ?c)"),
        // Add rules
        rewrite!("add-itself"; "(+ ?a ?a)" => "(* ?a 2)"),
        rewrite!("add-zero"; "(+ ?a 0)" => "?a"),
        rewrite!("add-cancellation"; "(+ ?a (- ?a))" => "0"),
        rewrite!("add-commutativity"; "(+ ?a ?b)" => "(+ ?b ?a)"),
        rewrite!("add-associativity"; "(+ ?a (+ ?b ?c))" => "(+ (+ ?a ?b) ?c)"),
        // Mul rules
        rewrite!("mul-zero"; "(* ?a 0)" => "0"),
        rewrite!("mul-one"; "(* ?a 1)" => "?a"),
        rewrite!("mul-commutativity"; "(* ?a ?b)" => "(* ?b ?a)"),
        rewrite!("mul-associativity"; "(* ?a (* ?b ?c))" => "(* (* ?a ?b) ?c)"),
        //rewrite!("mul-distributivity-expand"; "(* ?a (+ ?b ?c))" => "+ (* ?a ?b) (* a ?c)"),
        // Power rules
        rewrite!("power-zero"; "(** ?a 0)" => "1"),
        rewrite!("power-one"; "(** ?a 1)" => "?a"),
        // ported rules:
        // __eliminate_nested_negations_advanced
        rewrite!("negate-twice"; "(- (- ?a))" => "(?a)"),
        rewrite!("not-twice"; "(~ (~ ?a))" => "(?a)"),
        // __check_bitwise_negations
        // bitwise -> arith
        //rewrite!("add-bitwise-negation"; "(+ (~ ?a) ?b)" => "(+ (- (- ?a) 1) ?b)"),
        rewrite!("add-bitwise-negation"; "(+ (~ ?a) ?b)" => "(+ (+ (- ?a) -1) ?b)"),
        //rewrite!("sub-bitwise-negation"; "(- (~ ?a) ?b)" => "(- (- (- ?a) 1) ?b)"),
        rewrite!("sub-bitwise-negation"; "(+ (~ ?a) (- ?b))" => "(+ (+ (- ?a) -1) (- ?b))"),
        //rewrite!("mul-bitwise-negation"; "(* (~ ?a) ?b)" => "(* (- (- ?a) 1) ?b)"),
        rewrite!("mul-bitwise-negation"; "(* (~ ?a) ?b)" => "(* (+ (- ?a) -1) ?b)"),
        //rewrite!("pow-bitwise-negation"; "(** (~ ?a) ?b)" => "(** (- (- ?a) 1) ?b)"),
        rewrite!("pow-bitwise-negation"; "(** (~ ?a) ?b)" => "(** (+ (- ?a) -11) ?b)"),
        // arith -> bitwise
        //rewrite!("and-bitwise-negation"; "(& (- (- ?a) 1) ?b)" => "(& (~ ?a) ?b)"),
        rewrite!("and-bitwise-negation"; "(& (+ (- ?a) -1) ?b)" => "(& (~ ?a) ?b)"),
        //rewrite!("or-bitwise-negation"; "(| (- (- ?a) 1) ?b)" => "(| (~ ?a) ?b)"),
        rewrite!("or-bitwise-negation"; "(| (+ (- ?a) -1) ?b)" => "(| (~ ?a) ?b)"),
        //rewrite!("xor-bitwise-negation"; "(^ (- (- ?a) 1) ?b)" => "(^ (~ ?a) ?b)"),
        rewrite!("xor-bitwise-negation"; "(^ (+ (- ?a) -11) ?b)" => "(^ (~ ?a) ?b)"),
        // __check_bitwise_powers_of_two
        rewrite!("bitwise_powers_of_two: "; "(& (* ?factor1 ?x) (* ?factor2 y))" => {
            BitwisePowerOfTwoFactorApplier {
                xFactor : "?factor1".to_owned(),
                yFactor : "?factor2".to_owned(),
            }
        } if (is_power_of_two("?factor1", "?factor2"))),
        // __check_beautify_constants_in_products: todo
        // __check_move_in_bitwise_negations
        rewrite!("and-move-bitwise-negation-in"; "(~ (& (~ ?a) ?b))" => "(& ?a (~ ?b))"),
        rewrite!("or-move-bitwise-negation-in"; "(~ (| (~ ?a) ?b))" => "(| ?a (~ ?b))"),
        rewrite!("xor-move-bitwise-negation-in"; "(~ (^ (~ ?a) ?b))" => "(^ ?a (~ ?b))"),
        // __check_bitwise_negations_in_excl_disjunctions
        rewrite!("xor-flip-negations"; "(^ (~ ?a) (~ ?b))" => "(^ ?a ?b)"),
        // __check_rewrite_powers: todo
        // __check_resolve_product_of_powers
        // note: they say "Moreover merge factors that occur multiple times",
        // but I'm not sure what they mean
        rewrite!("merge-power-same-base"; "(* (** ?a ?b) (** ?a ?c))" => "(** ?a (+ ?b ?c))"),
        // __check_resolve_product_of_constant_and_sum
        rewrite!("distribute-constant-to-sum"; "(* (+ ?a ?b) Constant)" => "(+ (* ?a Constant) (* ?b Constant))"),
        // __check_factor_out_of_sum
        rewrite!("factor"; "(+ (* ?a ?b) (* ?a ?c))" => "(* ?a (+ ?b ?c))"),
        // __check_resolve_inverse_negations_in_sum
        rewrite!("invert-add-bitwise-not-self"; "(+ ?a (~ ?a))" => "-1"),
        rewrite!("invert-mul-bitwise-not-self"; "(+ (* ?a (~ ?b)) (* ?a ?b))" => "(- ?a)"),
        // __insert_fixed_in_conj: todo
        // __insert_fixed_in_disj: todo
        // __check_trivial_xor: implemented above
    ]
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

        if let Some(c) = &egraph[subst[var]].data {
            println!("Some: {}", c.0);
        }

        // println!("{}", &egraph[subst[var2]].nodes.len());
        let child = &egraph[subst[var]].nodes.first().unwrap();
        let child2 = &egraph[subst[var2]].nodes.first().unwrap();

        match child {
            Expr::Add(_) => println!("add"),
            Expr::UnaryMinus(_) => println!("UnaryMinus"),
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

    // simplify the expression using a Runner, which creates an e-graph with
    // the given expression and runs the given rules over it
    let runner = Runner::default().with_expr(&expr).run(&make_rules());

    // the Runner knows which e-class the expression given with `with_expr` is in
    let root = runner.roots[0];

    // use an Extractor to pick the best element of the root eclass
    let extractor = Extractor::new(&runner.egraph, AstSize);
    let (best_cost, best) = extractor.find_best(root);
    println!("Simplified {} to {} with cost {}", expr, best, best_cost);
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

    println!("{}", simplify(expr.as_str()));
}
