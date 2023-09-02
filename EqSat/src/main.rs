use egg::*;

define_language! {
    pub enum Expr {
        // operations
        "**" = Pow([Id; 2]),             // (+ a b)
        "~" = Neg([Id; 1]),              // (~ a)
        "-" = UnaryMinus([Id; 1]),       // (- a)
        "*" = Mul([Id; 2]),              // * a b)
        "+" = Add([Id; 2]),              // (+ a b)
        "&" = And([Id; 2]),              // (& a b)
        "^" = Xor([Id; 2]),              // (^ a b)
        "|" = Or([Id; 2]),               // (| a b)
        "//" = Div([Id; 2]),               // (| a b)
        "%" = Rem([Id; 2]),               // (| a b)

        // Values:
        Symbol(Symbol),                  // (x)
        Constant(i64),                   // (int)
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
        rewrite!("minus-to-neg-add"; "(- ?a)" => "(+ (~ ?a) 1)"),
        rewrite!("to-neg"; "(+ (~ ?a) 1)" => "(~ ?a)"),
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
