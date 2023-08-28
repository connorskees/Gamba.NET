use egg::*;

define_language! {
    pub enum Expr {
        // operations
        "**" = Pow([Id; 2]),             // (+ a b)
        "~" = Neg([Id; 1]),              // (~ a)
        "*" = Mul([Id; 2]),              // * a b)
        "+" = Add([Id; 2]),              // (+ a b)
        "&" = And([Id; 2]),              // (& a b)
        "^" = Xor([Id; 2]),              // (^ a b)
        "|" = Or([Id; 2]),               // (| a b)

        // Values:
        Symbol(Symbol),                  // (x) 
        Constant(i64),                   // (int)
    }
}

fn make_rules() -> Vec<Rewrite<Expr, ()>> {
    vec![
        rewrite!("commute-add"; "(+ ?a ?b)" => "(+ ?b ?a)"),
        rewrite!("commute-mul"; "(* ?a ?b)" => "(* ?b ?a)"),
        rewrite!("add-0"; "(+ ?a 0)" => "?a"),
        rewrite!("mul-0"; "(* ?a 0)" => "0"),
        rewrite!("mul-1"; "(* ?a 1)" => "?a"),
        rewrite!("and-0"; "(& ?a ?a)" => "?a"),
        rewrite!("and-1"; "(& ?a (& ?b ?a))" => "(& ?a ?b)"),
        rewrite!("xor-1"; "(^ ?a -1)" => "(~ ?a)"),
        rewrite!("const-1"; "(& ?a Constant)" => "(& Constant ?a)"),
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

    println!("{}", simplify("(& a 11111111)"));
}
