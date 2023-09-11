use std::collections::BTreeSet;

use expr_utils::{Ast, BinOp, VariableNameInterner};

use crate::bitwise_list::get_bitwise_list;

pub struct Solver<'a> {
    num_vars: usize,
    group_sizes: Vec<usize>,
    result_vector: Vec<i128>,
    modulus: i128,
    pub original_variables: VariableNameInterner<'a>,
    ast: Ast<'a>,
}

impl<'a> Solver<'a> {
    pub fn new(ast: Ast<'a>) -> Self {
        let original_variables = ast.variables();
        let num_vars = original_variables.len();
        let mut solver = Self {
            num_vars,
            result_vector: Vec::with_capacity(num_vars),
            original_variables,
            ast,
            modulus: 2_i128.pow(64), // u64::MAX as i128, //
            group_sizes: vec![1],
        };

        solver.init_group_sizes();
        solver.init_result_vector();

        solver
    }

    fn mod_red(&self, n: i128) -> i128 {
        return n as i64 as i128; // .rem_euclid(2_i128.pow(64)); // as i64 as i128; // ::MAX as i128; // self.modulus);
    }

    fn join_linear_combination(linear_combination: Vec<Ast<'a>>) -> Ast<'a> {
        let mut iter = linear_combination.into_iter();
        let mut ast = iter.next().unwrap();

        for elem in iter {
            ast = Ast::new_binop(ast, BinOp::Add, elem);
        }

        ast
    }

    pub fn solve(&mut self) -> Ast<'a> {
        let mut simpl;
        if self.num_vars > 3 {
            simpl = self.simplify_generic();
            simpl = self.try_simplify_fewer_variables(simpl)
        } else {
            let result_set = BTreeSet::<i128>::from_iter(self.result_vector.iter().copied());
            if result_set.len() == 1 {
                return self.simplify_one_value(result_set);
            }

            simpl = self.simplify_generic();
            simpl = self.try_refine(simpl);
        }

        Self::join_linear_combination(simpl)
    }

    fn simplify_one_value(&mut self, mut result_set: BTreeSet<i128>) -> Ast<'a> {
        let coefficient = result_set.pop_first().unwrap();
        Ast::Constant(self.mod_red(coefficient))
    }

    fn try_simplify_fewer_variables(&mut self, linear_combination: Vec<Ast<'a>>) -> Vec<Ast<'a>> {
        let mut occuring = Vec::with_capacity(self.num_vars);

        // todo: this is also just `Ast::variables`?
        let mut queue = linear_combination.iter().collect::<Vec<_>>();

        while let Some(node) = queue.pop() {
            match node {
                Ast::BinOp { lhs, rhs, .. } => {
                    queue.push(&**lhs);
                    queue.push(&**rhs);
                }
                Ast::Not(node) => queue.push(&**node),
                Ast::Variable(v) => {
                    occuring.push((v.as_bytes()["X[".len()] - b'0') as i128);
                }
                Ast::Constant(_) => {}
            }
        }

        let vnumber = occuring.len();

        // No function available for more than 3.
        if vnumber > 3 {
            return linear_combination;
        }

        for _ in 0..self.num_vars {
            // todo: variable replacing
            //     expr = expr.replace(self.get_vname(occuring[i]), "v" + str(i))
        }

        let mut inner_simplifier = Self::new(Self::join_linear_combination(linear_combination));
        let linear_combination = inner_simplifier.solve();

        for _ in 0..self.num_vars {
            // todo: variable replacing
            //     expr = expr.replace("v" + str(i), self.originalVariables[occuring[i]])
        }

        vec![linear_combination]
    }

    fn init_group_sizes(&mut self) {
        for _ in 1..self.num_vars {
            self.group_sizes.push(2 * *self.group_sizes.last().unwrap());
        }
    }

    fn init_result_vector(&mut self) {
        self.result_vector.clear();

        let mut par = Vec::with_capacity(self.num_vars);
        for i in 0..2_usize.pow(self.num_vars as u32) {
            let mut n = i as i128;
            par.clear();
            for _ in 0..self.num_vars {
                par.push(n & 1);
                n = n >> 1;
            }
            self.result_vector
                .push(self.mod_red(self.evaluate_original_expression(&par)));
        }
    }

    fn evaluate_original_expression(&self, vars: &[i128]) -> i128 {
        self.ast.evaluate(vars)
    }

    fn simplify_generic(&mut self) -> Vec<Ast<'a>> {
        let mut linear_combination = Vec::new();

        let constant = self.mod_red(self.result_vector[0]);

        if constant != 0 {
            for elem in &mut self.result_vector[1..] {
                *elem -= constant;
            }

            linear_combination.push(Ast::Constant(constant));
        }

        let vars = (0..self.num_vars as i128).collect::<Vec<_>>();
        for combo in sublists(&vars) {
            let index = combo
                .iter()
                .map(|&v| self.group_sizes[v as usize])
                .sum::<usize>();

            let coefficient = self.mod_red(self.result_vector[index]);

            if coefficient == 0 {
                continue;
            }

            self.subtract_coefficient(coefficient, index, combo);
            self.append_conjunction(&mut linear_combination, coefficient, combo);
        }

        if linear_combination.is_empty() {
            linear_combination.push(Ast::Constant(0));
        }

        linear_combination
    }

    fn subtract_coefficient(&mut self, coefficient: i128, first_start: usize, variables: &[i128]) {
        let groupsize1 = self.group_sizes[variables[0] as usize];
        let period1 = 2 * groupsize1;
        for start in (first_start..self.result_vector.len()).step_by(period1) {
            for i in start..start + groupsize1 {
                // The first variable is true by design of the for loops.
                if i != first_start
                    && (variables.len() == 1 || self.are_variables_true(i as i128, &variables[1..]))
                {
                    self.result_vector[i] -= coefficient as i128;
                }
            }
        }
    }

    fn are_variables_true(&self, mut n: i128, variables: &[i128]) -> bool {
        let mut prev = 0;
        for &v in variables {
            n >>= v - prev;
            prev = v;

            if (n & 1) == 0 {
                return false;
            }
        }
        return true;
    }

    fn try_refine(&mut self, linear_combination: Vec<Ast<'a>>) -> Vec<Ast<'a>> {
        self.init_result_vector();

        let num_terms = linear_combination.len();

        if num_terms <= 1 {
            return linear_combination;
        }

        let result_set = BTreeSet::<i128>::from_iter(self.result_vector.iter().copied());
        let l = result_set.len();
        debug_assert!(l > 1);

        let bitwise_list = get_bitwise_list(self.num_vars);

        if l == 2 {
            // (2) If only one nonzero value occurs and the result for all
            // variables being zero is zero, we can find a single expression.
            if self.result_vector[0] == 0 {
                return self.expression_for_each_unique_value(result_set, &bitwise_list);
            }

            // (3) Check whether we can find one single negated term.
            let simpler = self.try_find_negated_single_expression(result_set, &bitwise_list);

            if !simpler.is_empty() {
                return simpler;
            }
        }

        if num_terms <= 2 {
            return linear_combination;
        }

        let constant = self.result_vector[0];
        if constant != 0 {
            for elem in &mut self.result_vector {
                *elem -= constant;
            }
        }

        let result_set = BTreeSet::<i128>::from_iter(self.result_vector.iter().copied());
        let l = result_set.len();

        if l == 2 {
            // (4) In this case we know that the constant is nonzero since we
            // would have run into the case above otherwise.
            let mut expr = self.expression_for_each_unique_value(result_set, &bitwise_list);
            expr.insert(0, Ast::Constant(constant));
            return expr;
        }

        if l == 3 && constant == 0 {
            return self.expression_for_each_unique_value(result_set, &bitwise_list);
        }

        let unique_values = result_set
            .iter()
            .copied()
            .filter(|&r| r != 0)
            .collect::<Vec<_>>();

        if l == 4 && constant == 0 {
            // (6) We can still come down to 2 expressions if we can express one
            // value as a sum of the others.
            let simpler = self.try_eliminate_unique_value(&unique_values, &bitwise_list);
            if !simpler.is_empty() {
                return simpler;
            }
        }

        // NOTE: One may additionally want to try to find a sum of two negated
        // expressions, or one negated and one unnegated...

        // We cannot simplify the expression better.
        if num_terms == 3 {
            return linear_combination;
        }

        if constant == 0 {
            // (7) Since the previous attempts failed, the best we can do is find
            // three expressions corresponding to the three unique values.
            return self.expression_for_each_unique_value(result_set, &bitwise_list);
        }

        // (8) Try to reduce the number of unique values by expressing one as a
        // combination of the others.
        let mut simpler = self.try_eliminate_unique_value(&unique_values, &bitwise_list);

        if !simpler.is_empty() {
            if constant == 0 {
                return simpler;
            }

            simpler.insert(0, Ast::Constant(constant));

            return simpler;
        }

        return linear_combination;
    }

    fn try_find_negated_single_expression(
        &mut self,
        result_set: BTreeSet<i128>,
        bitwise_list: &[Ast<'static>],
    ) -> Vec<Ast<'a>> {
        // We can only find a negated expression if we have 2 distinct values.
        debug_assert_eq!(result_set.len(), 2);

        // Check whether we have a bitwise negation of a term in the lookup table.
        // This is the only chance for reducing the expression to one term.

        let mut a = *result_set.first().unwrap();
        let mut b = *result_set.last().unwrap();

        let a_double = self.is_double_modulo(a, b);
        let b_double = self.is_double_modulo(b, a);

        if !a_double && !b_double {
            return Vec::new();
        }

        // Make sure that b is double a.
        if a_double {
            (a, b) = (b, a);
        }
        if self.result_vector[0] == b {
            return Vec::new();
        }

        let coeff = self.mod_red(-a);

        let mut t = Vec::with_capacity(self.result_vector.len());
        for &r in &self.result_vector {
            t.push((r == b) as i128);
        }

        let index = self.get_bitwise_index_for_vector(&t, 0);
        let mut e = bitwise_list[index].clone();

        match e {
            Ast::Not(node) => e = *node,
            node => e = Ast::Not(Box::new(node)),
        }

        if coeff == 1 {
            return vec![e];
        }

        vec![Ast::new_binop(Ast::Constant(coeff), BinOp::Mul, e)]
    }

    fn is_double_modulo(&self, a: i128, b: i128) -> bool {
        2 * b == a || 2 * b == a.wrapping_add(self.modulus)
    }

    fn try_eliminate_unique_value(
        &mut self,
        unique_values: &[i128],
        bitwise_list: &[Ast<'static>],
    ) -> Vec<Ast<'a>> {
        let l = unique_values.len();
        // NOTE: Would be possible also for higher l, implementation is generic.
        if l > 4 {
            return Vec::new();
        }

        // Try to get rid of a value by representing it as a sum of the others.
        for i in 0..(l - 1) {
            for j in (i + 1)..l {
                for k in 0..l {
                    if k == i || k == j {
                        continue;
                    }

                    if self.is_sum_modulo(unique_values[i], unique_values[j], unique_values[k]) {
                        let mut simpler = Vec::with_capacity(unique_values.len());
                        for i1 in [i, j] {
                            self.append_term_refinement(
                                &mut simpler,
                                bitwise_list,
                                unique_values[i1],
                                Some(unique_values[k]),
                            );
                        }

                        if l > 3 {
                            let mut result_set = BTreeSet::from_iter(unique_values.iter().copied());
                            result_set.remove(&unique_values[i]);
                            result_set.remove(&unique_values[j]);
                            result_set.remove(&unique_values[k]);

                            while let Some(r1) = result_set.pop_last() {
                                self.append_term_refinement(&mut simpler, bitwise_list, r1, None);
                            }
                        }

                        return simpler;
                    }
                }
            }
        }

        if l < 4 {
            return Vec::new();
        }

        // Finally, if we have more than 3 values, try to express one of them as
        // a sum of all others.
        for i in 0..l {
            if 2 * unique_values[i] != unique_values.iter().sum() {
                continue;
            }

            let mut simpler = Vec::with_capacity(unique_values.len());
            for j in 0..l {
                if i == j {
                    continue;
                }

                self.append_term_refinement(
                    &mut simpler,
                    bitwise_list,
                    unique_values[j],
                    Some(unique_values[i]),
                );
            }

            return simpler;
        }

        Vec::new()
    }

    fn is_sum_modulo(&self, s1: i128, s2: i128, a: i128) -> bool {
        return s1 + s2 == a || s1 + s2 == a.wrapping_add(self.modulus);
    }

    fn get_bitwise_index_for_vector(&mut self, vector: &[i128], offset: i128) -> usize {
        let mut index = 0;
        let mut add = 1;
        for i in 0..(vector.len() - 1) {
            if vector[i + 1] != offset {
                index += add;
            }
            add <<= 1;
        }
        return index;
    }

    fn append_term_refinement(
        &mut self,
        linear_combination: &mut Vec<Ast<'a>>,
        bitwise_list: &[Ast<'static>],
        r1: i128,
        r_alt: Option<i128>,
    ) {
        let mut t = Vec::with_capacity(self.result_vector.len());

        for &r2 in &self.result_vector {
            t.push((r1 == r2 || Some(r2) == r_alt) as i128);
        }

        let index = self.get_bitwise_index_for_vector(&t, 0);

        if r1 == 1 {
            linear_combination.push(bitwise_list[index].clone());
            return;
        }

        let expr = Ast::new_binop(Ast::Constant(r1), BinOp::Mul, bitwise_list[index].clone());

        linear_combination.push(expr);
    }

    fn expression_for_each_unique_value(
        &mut self,
        result_set: BTreeSet<i128>,
        bitwise_list: &[Ast<'static>],
    ) -> Vec<Ast<'a>> {
        let mut out = Vec::with_capacity(result_set.len());
        for r in result_set {
            self.append_term_refinement(&mut out, bitwise_list, r, None);
        }

        out
    }

    fn get_v_name(n: i128) -> &'static str {
        match n {
            0 => "X[0]",
            1 => "X[1]",
            2 => "X[2]",
            3 => "X[3]",
            4 => "X[4]",
            5 => "X[5]",
            6 => "X[6]",
            _ => todo!("vars over 6"),
        }
    }

    fn append_conjunction(
        &mut self,
        linear_combination: &mut Vec<Ast<'a>>,
        coefficient: i128,
        variables: &[i128],
    ) {
        debug_assert!(!variables.is_empty());
        if coefficient == 0 {
            return;
        }

        let mut var = Ast::Variable(Self::get_v_name(variables[0]));

        for &v in &variables[1..] {
            let rhs = Ast::Variable(Self::get_v_name(v));

            var = Ast::new_binop(var, BinOp::And, rhs);
        }

        if coefficient != 1 {
            let rhs = Ast::Constant(coefficient);
            var = Ast::new_binop(var, BinOp::Mul, rhs);
        }

        linear_combination.push(var);
    }
}

fn sublists(s: &[i128]) -> Vec<&[i128]> {
    let mut lists = Vec::new();

    for start in 0..s.len() {
        for end in start..s.len() {
            lists.push(&s[start..=end]);
        }
    }

    lists.sort_by_key(|list| list.len());

    lists
}
