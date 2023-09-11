fn main() {
    println!("cargo:rerun-if-changed=src/s_expression.lalrpop");
    println!("cargo:rerun-if-changed=src/gamba_expression.lalrpop");
    lalrpop::process_root().unwrap();
}
