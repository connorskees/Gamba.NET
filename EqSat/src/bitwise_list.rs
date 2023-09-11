use expr_utils::{Ast, BinOp};

/// Hard-coded lookup table for simplifying linear MBAs. Eventually this should
/// be a lazy-static hashmap.
pub fn get_bitwise_list(num_vars: usize) -> Vec<Ast<'static>> {
    let not = |v| Ast::Not(Box::new(v));
    match num_vars {
        0 => Vec::new(),
        1 => vec![Ast::Constant(0), Ast::Variable("X[0]")],
        2 => {
            vec![
                Ast::Constant(0),
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    not(Ast::Variable("X[1]")),
                ),
                not(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    not(Ast::Variable("X[1]")),
                )),
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[1]")),
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
                Ast::Variable("X[0]"),
                Ast::Variable("X[1]"),
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[1]")),
            ]
        }
        3 => vec![
            Ast::Constant(0),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
            ))),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::Or,
                    Ast::Variable("X[2]"),
                ),
            ))),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[2]"))),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[1]")),
            ),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::Or,
                    Ast::Variable("X[2]"),
                ),
            ))),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::And,
                Ast::Not(Box::new(Ast::Variable("X[2]"))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::And,
                Ast::Not(Box::new(Ast::Variable("X[2]"))),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::And,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::And,
                    Ast::Variable("X[2]"),
                ),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[1]"))),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Variable("X[1]"),
                ))),
                BinOp::And,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::Variable("X[1]"),
                ))),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ))),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[1]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::And,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::And,
                    Ast::Variable("X[2]"),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::And,
                Ast::Not(Box::new(Ast::Variable("X[1]"))),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[1]")),
                BinOp::And,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::Variable("X[2]"),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
            ))),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[1]")),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::Or,
                        Ast::Variable("X[2]"),
                    ),
                ))),
            ),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Or,
                Ast::Not(Box::new(Ast::Variable("X[2]"))),
            ))),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Variable("X[1]"),
                ))),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[2]")),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::And,
                        Ast::Variable("X[2]"),
                    ),
                ),
            ),
            Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            Ast::new_binop(
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[1]")),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Or,
                Ast::Not(Box::new(Ast::Variable("X[1]"))),
            ))),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::Or,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::And,
                        Ast::Variable("X[2]"),
                    ),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::And,
                        Ast::Variable("X[2]"),
                    ),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::And,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Variable("X[2]"),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::Or,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ))),
            ),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Or,
                Ast::Not(Box::new(Ast::Variable("X[2]"))),
            ))),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::Or,
                        Ast::Variable("X[2]"),
                    ),
                ),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Xor,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[2]")),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[2]")),
                BinOp::Xor,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::And,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::Or,
                        Ast::Variable("X[2]"),
                    ),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[1]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::And,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::Or,
                        Ast::Variable("X[2]"),
                    ),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[1]")),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[1]")),
                BinOp::And,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Xor,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::Or,
                        Ast::Variable("X[2]"),
                    ),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::And,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Variable("X[1]"),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
                BinOp::Xor,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::And,
                        Ast::Variable("X[2]"),
                    ),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::Or,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::And,
                        Ast::Variable("X[2]"),
                    ),
                ))),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ))),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[1]")),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
            ),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ))),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::And,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::Variable("X[2]"),
                ))),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[1]")),
                BinOp::And,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::And,
                    Ast::Variable("X[2]"),
                ),
            ))),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::And,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ),
            ),
            Ast::new_binop(
                Ast::new_binop(
                    Ast::Variable("X[1]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ),
                BinOp::Or,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::Or,
                    Ast::new_binop(
                        Ast::Not(Box::new(Ast::Variable("X[1]"))),
                        BinOp::And,
                        Ast::Variable("X[2]"),
                    ),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::And,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::Variable("X[1]"),
                ))),
            ),
            Ast::new_binop(
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ),
                BinOp::And,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ))),
                BinOp::And,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Xor,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::And,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::And,
                    Ast::Variable("X[2]"),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ))),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[2]")),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::And,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::Or,
                    Ast::Variable("X[2]"),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::And,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
                ),
            ),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::Or,
                    Ast::Variable("X[2]"),
                ),
            ))),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
            ),
            Ast::Variable("X[0]"),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[2]")),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Variable("X[1]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ),
            ),
            Ast::Not(Box::new(Ast::new_binop(
                Ast::Not(Box::new(Ast::Variable("X[0]"))),
                BinOp::And,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::Or,
                    Ast::Variable("X[2]"),
                ),
            ))),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::And,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ),
            ),
            Ast::new_binop(
                Ast::new_binop(
                    Ast::Variable("X[1]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ))),
            ),
            Ast::new_binop(
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
                BinOp::Or,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[1]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                    BinOp::And,
                    Ast::Variable("X[2]"),
                ),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[1]")),
                BinOp::And,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[1]"),
                    BinOp::Xor,
                    Ast::Variable("X[2]"),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::And,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[2]")),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
                ),
            ),
            Ast::Variable("X[1]"),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::And,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::And,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ))),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::And,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::Or,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Or,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[2]"))),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::And,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[1]")),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Xor,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Xor, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Xor,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Not(Box::new(Ast::Variable("X[0]"))),
                    BinOp::Or,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
                ))),
            ),
            Ast::new_binop(
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::And, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[1]"),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[2]")),
            ),
            Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[1]")),
            Ast::Variable("X[2]"),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Or,
                Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::And,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Or,
                Ast::Not(Box::new(Ast::new_binop(
                    Ast::Variable("X[0]"),
                    BinOp::Or,
                    Ast::Not(Box::new(Ast::Variable("X[1]"))),
                ))),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::Xor, Ast::Variable("X[1]")),
            ),
            Ast::new_binop(
                Ast::Variable("X[2]"),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[0]"), BinOp::And, Ast::Variable("X[1]")),
            ),
            Ast::new_binop(Ast::Variable("X[0]"), BinOp::Or, Ast::Variable("X[2]")),
            Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
            Ast::new_binop(
                Ast::Variable("X[0]"),
                BinOp::Or,
                Ast::new_binop(Ast::Variable("X[1]"), BinOp::Or, Ast::Variable("X[2]")),
            ),
        ],
        _ => todo!(),
    }
}
