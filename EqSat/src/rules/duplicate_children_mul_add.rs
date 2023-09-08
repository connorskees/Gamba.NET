use egg::*;

use crate::{
    const_fold::{ConstantFold, EEGraph},
    Expr,
};

use super::read_constant;

#[derive(Debug)]
pub struct DuplicateChildrenMulAddApplier {
    pub const_factor: &'static str,
    pub x_factor: &'static str,
}

impl Applier<Expr, ConstantFold> for DuplicateChildrenMulAddApplier {
    fn apply_one(
        &self,
        egraph: &mut EEGraph,
        eclass: Id,
        subst: &Subst,
        _searcher_ast: Option<&PatternAst<Expr>>,
        _rule_name: Symbol,
    ) -> Vec<Id> {
        let new_const_expr = &egraph[subst[self.const_factor.parse().unwrap()]].data;

        let const_factor = read_constant(new_const_expr).unwrap();

        let x = subst[self.x_factor.parse().unwrap()];

        let new_const = egraph.add(Expr::Constant(const_factor + 1));
        let new_expr = egraph.add(Expr::Mul([new_const, x]));

        if egraph.union(eclass, new_expr) {
            vec![new_expr]
        } else {
            vec![]
        }
    }
}

pub fn is_const(var: &str) -> impl Fn(&mut EEGraph, Id, &Subst) -> bool {
    let var = var.parse().unwrap();

    move |egraph, _, subst| read_constant(&egraph[subst[var]].data).is_some()
}
