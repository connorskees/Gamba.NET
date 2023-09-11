use expr_utils::{gamba_expression, s_expression, GambaExpressionPrinter, SExpressionPrinter};

fn main() {
    let input = std::env::args().skip(1).next().unwrap();
    let gamba_expr = gamba_expression::AstParser::new().parse(&input);
    let s_expr = s_expression::AstParser::new().parse(&input);

    assert!(gamba_expr.is_ok() || s_expr.is_ok());
    assert!(gamba_expr.is_ok() ^ s_expr.is_ok());

    let serialized = if let Ok(expr) = gamba_expr {
        SExpressionPrinter::print(&expr)
    } else if let Ok(expr) = s_expr {
        GambaExpressionPrinter::print(&expr)
    } else {
        unreachable!()
    };

    println!("{}", serialized);
}
