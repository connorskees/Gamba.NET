use std::{
    collections::{BTreeMap, HashMap},
    fmt::{self, Write},
};

use lalrpop_util::lalrpop_mod;

lalrpop_mod!(pub s_expression);
lalrpop_mod!(pub gamba_expression);

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum BinOp {
    Add,
    Mul,
    Pow,

    Or,
    And,
    Xor,
}

impl fmt::Display for BinOp {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            BinOp::Add => f.write_char('+'),
            BinOp::Mul => f.write_char('*'),
            BinOp::Pow => f.write_str("**"),
            BinOp::Or => f.write_char('|'),
            BinOp::And => f.write_char('&'),
            BinOp::Xor => f.write_char('^'),
        }
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum Ast<'a> {
    BinOp {
        lhs: Box<Self>,
        op: BinOp,
        rhs: Box<Self>,
    },
    Not(Box<Self>),
    Variable(&'a str),
    Constant(i128),
}

#[derive(Debug)]
pub struct VariableNameInterner<'a> {
    to_index: BTreeMap<&'a str, usize>,
    names: Vec<&'a str>,
}

impl<'a> VariableNameInterner<'a> {
    pub fn new() -> Self {
        Self {
            to_index: BTreeMap::new(),
            names: Vec::new(),
        }
    }

    pub fn insert(&mut self, name: &'a str) {
        let id = self.names.len();
        if !self.to_index.contains_key(&name) {
            self.to_index.insert(name, id);
            self.names.push(name);
        }
    }

    pub fn len(&self) -> usize {
        self.names.len()
    }

    pub fn finish(&mut self) {
        self.names.sort_by_key(|name| *name);
    }

    pub fn rename_var(&self, var: &str) -> String {
        debug_assert!(var.starts_with("X["));
        debug_assert!(var.ends_with("]"));

        let idx = var.as_bytes()["X[".len()] - b'0';

        self.names[idx as usize].to_owned()
    }

    pub fn replace_vars(self, mut s: String) -> String {
        for (idx, name) in self.names.into_iter().enumerate() {
            s = s.replace(&format!("X[{idx}]"), name);
        }

        s
    }
}

impl<'a> Ast<'a> {
    pub fn new_binop(lhs: Self, op: BinOp, rhs: Self) -> Self {
        Self::BinOp {
            lhs: Box::new(lhs),
            op,
            rhs: Box::new(rhs),
        }
    }

    pub fn variables(&self) -> VariableNameInterner<'a> {
        let mut vars = VariableNameInterner::new();

        let mut queue = vec![self];

        while let Some(node) = queue.pop() {
            match node {
                Ast::BinOp { lhs, rhs, .. } => {
                    queue.push(&**lhs);
                    queue.push(&**rhs);
                }
                Ast::Not(node) => queue.push(&**node),
                Ast::Variable(v) => {
                    vars.insert(*v);
                }
                Ast::Constant(_) => {}
            }
        }

        vars
    }

    pub fn evaluate(&self, vars: &'a [i128]) -> i128 {
        Evaluator::new(vars).visit_ast(self)
    }
}

struct Evaluator<'a> {
    var_indices: HashMap<&'a str, usize>,
    vars: &'a [i128],
}

impl<'a> Evaluator<'a> {
    pub fn new(vars: &'a [i128]) -> Self {
        Self {
            vars,
            var_indices: HashMap::with_capacity(vars.len()),
        }
    }

    pub fn visit_ast(&mut self, ast: &Ast<'a>) -> i128 {
        match ast {
            Ast::BinOp { lhs, op, rhs } => {
                let lhs = self.visit_ast(&**lhs);
                let rhs = self.visit_ast(&**rhs);

                match *op {
                    BinOp::Add => lhs + rhs,
                    BinOp::Mul => (lhs as i64).wrapping_mul(rhs as i64) as i128,
                    BinOp::Pow => lhs.pow(rhs.try_into().unwrap()),
                    BinOp::Or => lhs | rhs,
                    BinOp::And => lhs & rhs,
                    BinOp::Xor => lhs ^ rhs,
                }
            }
            Ast::Not(expr) => !self.visit_ast(&**expr),
            Ast::Variable(v) => {
                let maybe_idx = self.var_indices.len();
                let idx = *self.var_indices.entry(*v).or_insert(maybe_idx);

                self.vars[idx]
            }
            Ast::Constant(v) => *v,
        }
    }
}

pub struct SExpressionPrinter {
    output: String,
}

impl SExpressionPrinter {
    pub fn print(ast: &Ast) -> String {
        let mut printer = Self::new();
        printer.print_node(ast);
        printer.output
    }

    fn new() -> Self {
        Self {
            output: String::new(),
        }
    }

    fn print_node(&mut self, node: &Ast) {
        match node {
            Ast::BinOp { lhs, op, rhs } => {
                self.output.push_str(&format!("({} ", op));
                self.print_node(&**lhs);
                self.output.push(' ');
                self.print_node(&**rhs);
                self.output.push(')');
            }
            Ast::Not(expr) => {
                self.output.push_str("(~ ");
                self.print_node(&**expr);
                self.output.push(')');
            }
            Ast::Variable(var) => self.output.push_str(var),
            Ast::Constant(c) => self.output.push_str(&(*c as i64).to_string()),
        }
    }
}

pub struct GambaExpressionPrinter {
    output: String,
}

impl GambaExpressionPrinter {
    pub fn print(ast: &Ast) -> String {
        let mut printer = Self::new();
        printer.print_node(ast);
        printer.output
    }

    fn new() -> Self {
        Self {
            output: String::new(),
        }
    }

    fn print_node(&mut self, node: &Ast) {
        match node {
            Ast::BinOp { lhs, op, rhs } => {
                if *op != BinOp::Mul {
                    self.output.push('(');
                }
                self.print_node(&**lhs);
                if *op != BinOp::Mul {
                    self.output.push(' ');
                }
                self.output.push_str(&op.to_string());
                if *op != BinOp::Mul {
                    self.output.push(' ');
                }
                self.print_node(&**rhs);
                if *op != BinOp::Mul {
                    self.output.push(')');
                }
            }
            Ast::Not(expr) => {
                self.output.push('~');
                self.print_node(&**expr);
            }
            Ast::Variable(v) => self.output.push_str(v),
            Ast::Constant(v) => self.output.push_str(&v.to_string()),
        }
    }
}

pub struct AstPrinter {
    output: String,
}

impl AstPrinter {
    pub fn print(ast: &Ast) -> String {
        let mut printer = Self::new();
        printer.print_node(ast);
        printer.output
    }

    fn new() -> Self {
        Self {
            output: String::new(),
        }
    }

    fn print_node(&mut self, node: &Ast) {
        match node {
            Ast::BinOp { lhs, op, rhs } => {
                self.output.push_str("Ast::new_binop(");
                self.print_node(&**lhs);
                self.output.push_str(", ");
                self.output.push_str(match &op {
                    BinOp::Add => "BinOp::Add",
                    BinOp::Mul => "BinOp::Mul",
                    BinOp::Pow => "BinOp::Pow",
                    BinOp::Or => "BinOp::Or",
                    BinOp::And => "BinOp::And",
                    BinOp::Xor => "BinOp::Xor",
                });
                self.output.push_str(", ");
                self.print_node(&**rhs);
                self.output.push(')');
            }
            Ast::Not(expr) => {
                self.output.push_str("Ast::Not(Box::new(");
                self.print_node(&**expr);
                self.output.push_str("))");
            }
            Ast::Variable(v) => self.output.push_str(&format!("Ast::Variable(\"{v}\")")),
            Ast::Constant(v) => self.output.push_str(&format!("Ast::Constant({v})")),
        }
    }
}
