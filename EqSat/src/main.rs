use egg::*;

define_language! {
    pub enum Expr {
        // arithmetic operations
        "+" = Add([Id; 2]),        // (+ a b)
        "-" = UnaryMinus([Id; 1]), // (- a)
        "*" = Mul([Id; 2]),        // (* a b)
        "//" = Div([Id; 2]),       // (| a b)
        "%" = Rem([Id; 2]),        // (% a b)
        "**" = Pow([Id; 2]),       // (** a b)
        // bitwise operations
        "&" = And([Id; 2]),        // (& a b)
        "|" = Or([Id; 2]),         // (| a b)
        "^" = Xor([Id; 2]),        // (^ a b)
        "~" = Not([Id; 1]),        // (~ a)

        // Values:
        Symbol(Symbol),            // (x)
        Constant(i64),             // (int)
    }
}

fn make_rules() -> Vec<Rewrite<Expr, ()>> {
    vec![
        // Or rules
        rewrite!("or-zero"; "(| ?a 0)" => "?a"),
        rewrite!("or-maxint"; "(| ?a -1)" => "-1"),
        rewrite!("or-itself"; "(| ?a ?a)" => "?a"),
        rewrite!("or-negated-itself"; "(| ?a (~ a))" => "-1"),
        rewrite!("or-commutativity"; "(| ?a ?b)" => "(| ?b ?a)"),
        rewrite!("or-associativity"; "(| ?a (| ?b ?c))" => "(| (| ?a ?b) ?c"),
        // Xor rules
        rewrite!("xor-zero"; "(^ ?a 0)" => "?a"),
        rewrite!("xor-maxint"; "(^ ?a -1)" => "(~ ?a)"),
        rewrite!("xor-itself"; "(^ ?a ?a)" => "0"),
        rewrite!("xor-commutativity"; "(^ ?a ?b)" => "(^ ?b ?a)"),
        rewrite!("xor-associativity"; "(^ ?a (^ ?b ?c))" => "(^ (^ ?a ?b) ?c"),
        // And rules
        rewrite!("and-zero"; "(& ?a 0)" => "0"),
        rewrite!("and-maxint"; "(& ?a -1)" => "?a"),
        rewrite!("and-itself"; "(& ?a ?a)" => "?a"),
        rewrite!("and-negated-itself"; "(& ?a (~ ?a))" => "0"),
        rewrite!("and-commutativity"; "(& ?a ?b)" => "(& ?b ?a)"),
        rewrite!("and-associativity"; "(& ?a (& ?b ?c))" => "(& (& ?a ?b) ?c"),
        // Add rules
        rewrite!("add-itself"; "(+ ?a ?a)" => "(* ?a 2)"),
        rewrite!("add-zero"; "(+ ?a 0)" => "?a"),
        rewrite!("add-cancellation"; "(+ ?a (- ?a))" => "0"),
        rewrite!("add-commutativity"; "(+ ?a ?b)" => "(+ ?b ?a)"),
        rewrite!("add-associativity"; "(+ ?a (+ ?b ?c))" => "(+ (+ ?a ?b) ?c"),
        // Mul rules
        rewrite!("mul-zero"; "(* ?a 0)" => "0"),
        rewrite!("mul-one"; "(* ?a 1)" => "?a"),
        rewrite!("mul-commutativity"; "(* ?a ?b)" => "(* ?b ?a)"),
        rewrite!("mul-associativity"; "(* ?a (* ?b ?c))" => "(* (* ?a ?b) ?c"),
        rewrite!("mul-distributivity-expand"; "(* ?a (+ ?b ?c))" => "+ (* ?a ?b) (* a ?c)"),
        // Power rules
        rewrite!("power-zero"; "(** ?a 0)" => "1"),
        rewrite!("power-one"; "(** ?a 1)" => "?a"),
        // Negation rules
        // afaict these are implemented below by __check_bitwise_negations but
        // with better heuristics
        // rewrite!("minus-to-neg-add"; "(- ?a)" => "(+ (~ ?a) 1)"),
        // rewrite!("to-neg"; "(+ (~ ?a) 1)" => "(~ ?a)"),
        // ported rules:
        // __eliminate_nested_negations_advanced
        rewrite!("negate-twice"; "(- (- ?a))" => "(& ?a)"),
        rewrite!("not-twice"; "(~ (~ ?a))" => "(& ?a)"),
        // __check_bitwise_negations
        // bitwise -> arith
        rewrite!("add-bitwise-negation"; "(+ (~ ?a) ?b)" => "(+ (- (- ?a) 1) ?b)"),
        rewrite!("sub-bitwise-negation"; "(- (~ ?a) ?b)" => "(- (- (- ?a) 1) ?b)"),
        rewrite!("mul-bitwise-negation"; "(* (~ ?a) ?b)" => "(* (- (- ?a) 1) ?b)"),
        rewrite!("div-bitwise-negation"; "(// (~ ?a) ?b)" => "(// (- (- ?a) 1) ?b)"),
        rewrite!("pow-bitwise-negation"; "(** (~ ?a) ?b)" => "(** (- (- ?a) 1) ?b)"),
        rewrite!("rem-bitwise-negation"; "(% (~ ?a) ?b)" => "(% (- (- ?a) 1) ?b)"),
        // arith -> bitwise
        rewrite!("and-bitwise-negation"; "(& (- (- ?a) 1) ?b)" => "(& (~ ?a) ?b)"),
        rewrite!("or-bitwise-negation"; "(| (- (- ?a) 1) ?b)" => "(| (~ ?a) ?b)"),
        rewrite!("xor-bitwise-negation"; "(^ (- (- ?a) 1) ?b)" => "(^ (~ ?a) ?b)"),
        // __check_bitwise_powers_of_two: todo
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
    println!("Hello, world!");

    println!("{}", simplify("(- ?x)"));
}
