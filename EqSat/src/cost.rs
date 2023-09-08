use egg::*;

use crate::{
    const_fold::{ConstantFold, EEGraph},
    Expr,
};

pub type Cost = i64;

pub fn get_cost(egraph: &EEGraph, enode: &Expr) -> Cost {
    let cost = |i: &Id| egraph[*i].data.as_ref().unwrap().2;
    match enode {
        Expr::Add([a, b]) | Expr::Mul([a, b]) => cost(a) + cost(b),
        Expr::Pow([a, b]) | Expr::And([a, b]) | Expr::Or([a, b]) | Expr::Xor([a, b]) => {
            cost(a) + cost(b) + 1
        }
        Expr::Neg([a]) => cost(a) + 1,
        Expr::Constant(_) | Expr::Symbol(_) => 1,
    }
}

pub struct EGraphCostFn<'a> {
    pub egraph: &'a EGraph<Expr, ConstantFold>,
}

impl<'a> CostFunction<Expr> for EGraphCostFn<'a> {
    type Cost = usize;
    fn cost<C>(&mut self, enode: &Expr, _costs: C) -> Self::Cost
    where
        C: FnMut(Id) -> Self::Cost,
    {
        get_cost(self.egraph, enode) as usize
    }
}
