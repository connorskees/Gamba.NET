use std::{
    collections::{HashMap, HashSet},
    mem,
};

use rustpython_parser::{
    ast::{self, BoolOp, CmpOp, Operator, StmtClassDef, StmtFunctionDef, UnaryOp},
    text_size::TextRange,
    Parse,
};

pub struct TranslationContext {
    /// A mapping of variables currently defined within a function.
    defined_variables: HashMap<TextRange, HashSet<String>>,
    /// Current indentation
    indentation: usize,
    /// The current class definition we are inside
    owning_class: Option<StmtClassDef>,
    /// The current function definition we are inside
    owning_function: Option<StmtFunctionDef>,
    /// The current C# output
    output: String,
}

impl TranslationContext {
    fn indent(&self) -> String {
        vec![" "; self.indentation * 4].join("")
    }

    fn self_to_this(s: String) -> String {
        if s == "self" {
            "this".to_owned()
        } else {
            s
        }
    }

    fn visit_body(&mut self, body: Vec<ast::Stmt>) {
        self.indentation += 1;
        for body_stmt in body {
            self.visit(body_stmt);
        }
        self.indentation -= 1;
    }

    fn visit(&mut self, stmt: ast::Stmt) {
        match stmt {
            ast::Stmt::ClassDef(def) => self.visit_class_def(def),
            ast::Stmt::FunctionDef(def) => self.visit_function_def(def),
            ast::Stmt::Assign(def) => self.visit_assign(def),
            ast::Stmt::AsyncFunctionDef(_) => todo!(),
            ast::Stmt::Return(return_stmt) => self.visit_return_stmt(return_stmt),
            ast::Stmt::Delete(_) => todo!(),
            ast::Stmt::TypeAlias(_) => todo!(),
            ast::Stmt::AugAssign(def) => self.visit_aug_assign(def),
            ast::Stmt::AnnAssign(_) => todo!(),
            ast::Stmt::For(for_stmt) => self.visit_for(for_stmt),
            ast::Stmt::AsyncFor(_) => todo!(),
            ast::Stmt::While(while_stmt) => self.visit_while_stmt(while_stmt),
            ast::Stmt::If(if_stmt) => self.visit_if_stmt(if_stmt),
            ast::Stmt::With(_) => todo!(),
            ast::Stmt::AsyncWith(_) => todo!(),
            ast::Stmt::Match(_) => todo!(),
            ast::Stmt::Raise(_) => todo!(),
            ast::Stmt::Try(try_stmt) => self.visit_try(try_stmt),
            ast::Stmt::TryStar(_) => todo!(),
            ast::Stmt::Assert(_) => todo!(),
            ast::Stmt::Import(_) => {}
            ast::Stmt::ImportFrom(_) => {}
            ast::Stmt::Global(_) => todo!(),
            ast::Stmt::Nonlocal(_) => todo!(),
            ast::Stmt::Expr(stmt_expr) => self.visit_stmt_expr(stmt_expr),
            ast::Stmt::Pass(_) => todo!(),
            ast::Stmt::Break(_) => todo!(),
            ast::Stmt::Continue(_) => todo!(),
        }
    }

    fn visit_for(&mut self, for_stmt: ast::StmtFor) {
        todo!("for loop")
    }

    fn visit_try(&mut self, try_stmt: ast::StmtTry) {
        println!("{}try {{", self.indent());
        self.visit_body(try_stmt.body);
        if !try_stmt.orelse.is_empty() {
            println!("{}}} catch {{", self.indent());
            self.visit_body(try_stmt.orelse);
        }

        if !try_stmt.finalbody.is_empty() {
            println!("{}}} finally {{", self.indent());
            self.visit_body(try_stmt.finalbody);
        }

        println!("{}}}", self.indent());
    }

    fn visit_return_stmt(&mut self, return_stmt: ast::StmtReturn) {
        if let Some(value) = return_stmt.value {
            println!("{}return {};", self.indent(), self.visit_expr(*value));
        }
    }

    fn visit_if_stmt(&mut self, if_stmt: ast::StmtIf) {
        println!(
            "{}if ({}) {{",
            self.indent(),
            self.visit_expr(*if_stmt.test)
        );
        self.visit_body(if_stmt.body);
        print!("{}}}", self.indent());
        if if_stmt.orelse.is_empty() {
            println!("")
        } else {
            println!(" else {{");
            self.visit_body(if_stmt.orelse);
            println!("{}}}", self.indent());
        }
    }

    fn visit_while_stmt(&mut self, while_stmt: ast::StmtWhile) {
        println!(
            "{}while ({}) {{",
            self.indent(),
            self.visit_expr(*while_stmt.test)
        );
        self.visit_body(while_stmt.body);
        println!("{}}}", self.indent());
    }

    fn visit_stmt_expr(&mut self, expr: ast::StmtExpr) {
        println!("{}{};", self.indent(), self.visit_expr(*expr.value));
    }

    fn visit_class_def(&mut self, def: ast::StmtClassDef) {
        println!("public class {}\n{}{{", def.clone().name, self.indent());

        mem::swap(&mut self.owning_class, &mut Some(def.clone()));
        self.visit_body(def.clone().body);
        mem::swap(&mut self.owning_class, &mut Some(def.clone()));

        println!("{}}}\n", self.indent())
    }

    fn visit_function_decl_arg(&mut self, arg: &ast::ArgWithDefault) -> String {
        let ty = self.expr_type(arg.default.clone());
        let name = arg.as_arg().arg.to_string();
        let default = arg
            .default
            .as_ref()
            .map(|default| format!("={}", self.visit_expr(*default.clone())));

        if let Some(default) = default {
            format!("{ty} {name}{default}")
        } else {
            format!("dynamic {name}")
        }
    }

    fn expr_type(&mut self, expr: Option<Box<ast::Expr>>) -> String {
        let expr = match expr {
            Some(e) => *e,
            None => return "void".to_owned(),
        };

        match expr {
            ast::Expr::BoolOp(_) => "bool",
            ast::Expr::Compare(_) => "bool",
            ast::Expr::Call(call_expr) => {
                let name = self.visit_expr(*call_expr.func);
                match name.as_str() {
                    "Node" => "Node",
                    _ => "dynamic",
                }
            }
            ast::Expr::Constant(constant) => match constant.value {
                ast::Constant::None => todo!(),
                ast::Constant::Bool(_) => "bool",
                ast::Constant::Str(_) => "string",
                ast::Constant::Bytes(_) => todo!(),
                ast::Constant::Int(_) => "ulong",
                ast::Constant::Tuple(_) => todo!(),
                ast::Constant::Float(_) => "float",
                ast::Constant::Complex { .. } => todo!(),
                ast::Constant::Ellipsis => todo!(),
            },
            ast::Expr::Name(name_expr) => match name_expr.id.as_str() {
                "node" | "prod" => "Node",
                _ => "dynamic",
            },
            _ => "dynamic",
        }
        .to_owned()
    }

    fn function_return_type(&mut self, def: &ast::StmtFunctionDef) -> String {
        let mut return_type = "void ".to_owned();
        for stmt in def.body.iter() {
            match stmt {
                ast::Stmt::Return(def) => {
                    return_type = self.expr_type(def.value.clone());
                    break;
                }
                _ => {}
            }
        }

        if def.name.as_str() == "__init__" {
            return_type = String::new();
        }

        return_type
    }

    fn visit_function_def(&mut self, def: ast::StmtFunctionDef) {
        let name = if def.name.as_str() == "__init__" {
            self.owning_class.clone().unwrap().name.to_string()
        } else {
            def.name.to_string()
        };

        let return_type = self.function_return_type(&def);
        println!(
            "{}public {} {}({})\n{}{{",
            self.indent(),
            return_type,
            name,
            def.args
                .args
                .iter()
                .map(|arg| self.visit_function_decl_arg(arg))
                .filter(|arg| arg.to_string() != "self")
                .collect::<Vec<_>>()
                .join(", "),
            self.indent(),
        );

        mem::swap(&mut self.owning_function, &mut Some(def.clone()));
        self.visit_body(def.clone().body);
        mem::swap(&mut self.owning_function, &mut Some(def.clone()));

        println!("{}}}\n", self.indent());
    }

    fn visit_assign(&mut self, def: ast::StmtAssign) {
        assert_eq!(def.targets.len(), 1, "too many assignment targets");

        let target = def.targets.first().unwrap();

        let var_name = match target {
            // Match self.instance_variable
            ast::Expr::Attribute(def) => {
                let value = def.clone().value.expect_name_expr().id.to_string();
                format!(
                    "{}.{}",
                    Self::self_to_this(value),
                    def.clone().attr.to_string()
                )
            }

            // Match "local_variable ="
            ast::Expr::Name(def) => {
                let mut is_first_definition = false;

                if let Some(owning_function) = self.owning_function.clone() {
                    if !self.defined_variables.contains_key(&owning_function.range) {
                        self.defined_variables
                            .insert(owning_function.range, HashSet::new());
                    }

                    let map = self
                        .defined_variables
                        .get_mut(&owning_function.range)
                        .unwrap();

                    is_first_definition = !map.contains(&def.id.to_string());

                    if is_first_definition {
                        map.insert(def.id.to_string());
                    }
                }

                format!(
                    "{}",
                    if is_first_definition {
                        format!("var {}", def.id.to_string())
                    } else {
                        def.id.to_string()
                    }
                )
            }
            _ => todo!(
                "Unexpected assignment destination type: {}",
                target.python_name()
            ),
        };

        // Print "var = "
        println!(
            "{}{} = {};",
            self.indent(),
            var_name,
            self.visit_expr(*def.value)
        );
    }

    fn visit_aug_assign(&mut self, def: ast::StmtAugAssign) {
        println!(
            "{}{} {}= {};",
            self.indent(),
            self.visit_expr(*def.target),
            op_to_string(def.op),
            self.visit_expr(*def.value)
        )
    }

    fn visit_expr(&mut self, expr: ast::Expr) -> String {
        match expr {
            ast::Expr::Name(def) => def.id.to_string(),
            ast::Expr::Constant(constant) => match constant.value {
                ast::Constant::None => "null".to_owned(),
                ast::Constant::Bool(v) => v.to_string(),
                ast::Constant::Str(v) => format!("\"{v}\""),
                ast::Constant::Bytes(_) => todo!(),
                ast::Constant::Int(v) => v.to_string(),
                ast::Constant::Tuple(_) => todo!(),
                ast::Constant::Float(v) => v.to_string(),
                ast::Constant::Complex { .. } => todo!(),
                ast::Constant::Ellipsis => todo!(),
            },
            ast::Expr::Call(call_expr) => {
                let mut name = self.visit_expr(*call_expr.func);
                name = name.replace("append", "Add");
                name = name.replace("insert", "Insert");
                name = name.replace("re.match", "reutil.match");
                if name == "len" && call_expr.args.len() == 1 {
                    format!(
                        "{}.Length",
                        call_expr
                            .args
                            .into_iter()
                            .map(|arg| self.visit_expr(arg))
                            .collect::<Vec<_>>()
                            .join(", ")
                    )
                } else {
                    let new = if name.as_bytes()[0].is_ascii_uppercase() && name == "Node" {
                        "new "
                    } else {
                        ""
                    };
                    format!(
                        "{new}{name}({})",
                        call_expr
                            .args
                            .into_iter()
                            .map(|arg| self.visit_expr(arg))
                            .collect::<Vec<_>>()
                            .join(", ")
                    )
                }
            }
            ast::Expr::Attribute(attr_expr) => {
                format!(
                    "{}.{}",
                    Self::self_to_this(self.visit_expr(*attr_expr.value)),
                    attr_expr.attr
                )
            }
            ast::Expr::BoolOp(def) => {
                format!(
                    "({})",
                    def.values
                        .into_iter()
                        .map(|expr| self.visit_expr(expr))
                        .collect::<Vec<_>>()
                        .join(&format!(" {} ", bool_op_to_string(def.op)))
                )

                /*
                format!(
                    "{} && {}",
                    self.visit_expr(def.values[0].clone()),
                    self.visit_expr(def.values[1].clone())
                )
                */
            }

            ast::Expr::Compare(def) => {
                assert_eq!(def.comparators.len(), 1, "too many or too few comparators");
                assert_eq!(
                    def.ops.len(),
                    1,
                    "too many or too few comparison operators (e.g. <=)"
                );

                match def.ops[0] {
                    CmpOp::NotIn => format!(
                        "!(({}).Contains({}))",
                        self.visit_expr(def.comparators[0].clone()),
                        self.visit_expr(*def.left)
                    ),
                    CmpOp::In => format!(
                        "(({}).Contains({}))",
                        self.visit_expr(def.comparators[0].clone()),
                        self.visit_expr(*def.left)
                    ),
                    op => format!(
                        "{} {} {}",
                        self.visit_expr(*def.left),
                        cmp_to_string(op),
                        self.visit_expr(def.comparators[0].clone())
                    ),
                }
            }
            ast::Expr::BinOp(def) => {
                format!(
                    "{} {} {}",
                    self.visit_expr(*def.left),
                    op_to_string(def.op),
                    self.visit_expr(*def.right)
                )
            }
            ast::Expr::UnaryOp(def) => {
                format!(
                    "{}({})",
                    unary_op_to_string(def.op),
                    self.visit_expr(*def.operand)
                )
            }
            // TODO: Handle generators?
            ast::Expr::List(def) => {
                format!(
                    "new List<dynamic>() {{ {} }}",
                    def.elts
                        .into_iter()
                        .map(|expr| self.visit_expr(expr))
                        .collect::<Vec<_>>()
                        .join(", ")
                )
            }

            ast::Expr::Subscript(def) => {
                if def.slice.is_slice_expr() {
                    format!(
                        "{}{}",
                        self.visit_expr(*def.value),
                        self.visit_expr(*def.slice)
                    )
                } else {
                    format!(
                        "{}[{}]",
                        self.visit_expr(*def.value),
                        self.visit_expr(*def.slice)
                    )
                }
            }
            ast::Expr::Slice(def) => {
                //assert_eq!(def.step.is_none(), false);

                format!(
                    ".Slice({}, {}, {})",
                    if def.lower.is_none() {
                        String::from("null")
                    } else {
                        self.visit_expr(*def.lower.clone().unwrap())
                    },
                    if def.upper.is_none() {
                        String::from("null")
                    } else {
                        self.visit_expr(*def.upper.clone().unwrap())
                    },
                    if def.step.is_none() {
                        String::from("null")
                    } else {
                        self.visit_expr(*def.step.clone().unwrap())
                    },
                )
            }
            ast::Expr::NamedExpr(_)
            | ast::Expr::Lambda(_)
            | ast::Expr::IfExp(_)
            | ast::Expr::Dict(_)
            | ast::Expr::Set(_)
            | ast::Expr::SetComp(_)
            | ast::Expr::DictComp(_)
            | ast::Expr::GeneratorExp(_)
            | ast::Expr::Await(_)
            | ast::Expr::Yield(_)
            | ast::Expr::YieldFrom(_)
            | ast::Expr::FormattedValue(_)
            | ast::Expr::JoinedStr(_)
            | ast::Expr::Starred(_)
            | ast::Expr::ListComp(_)
            | ast::Expr::Tuple(_) => todo!("Unimplemented expression type {:#?}", expr),
        }
    }
}

fn cmp_to_string(cmp_op: CmpOp) -> &'static str {
    match cmp_op {
        ast::CmpOp::Eq => "==",
        ast::CmpOp::NotEq => "!=",
        ast::CmpOp::Lt => "<",
        ast::CmpOp::LtE => "<=",
        ast::CmpOp::Gt => ">",
        ast::CmpOp::GtE => ">=",
        ast::CmpOp::Is => "is",
        ast::CmpOp::IsNot => "is not",
        ast::CmpOp::In => "in",
        ast::CmpOp::NotIn => "not in",
    }
}

fn op_to_string(op: Operator) -> &'static str {
    match op {
        Operator::Add => "+",
        Operator::Sub => "-",
        Operator::Mult => "*",
        Operator::MatMult => todo!(),
        Operator::Div => "/",
        Operator::Mod => "%",
        Operator::Pow => "**",
        Operator::LShift => "<<",
        Operator::RShift => ">>",
        Operator::BitOr => "|",
        Operator::BitXor => "^",
        Operator::BitAnd => "&",
        Operator::FloorDiv => todo!(),
    }
}

fn bool_op_to_string(op: BoolOp) -> &'static str {
    match op {
        BoolOp::And => "&&",
        BoolOp::Or => "||",
    }
}

fn unary_op_to_string(op: UnaryOp) -> &'static str {
    match op {
        UnaryOp::Invert => "~",
        UnaryOp::Not => "!",
        UnaryOp::UAdd => "+",
        UnaryOp::USub => "-",
    }
}

fn main() {
    let file_name = std::env::args().skip(1).next().unwrap();
    let python_source = std::fs::read_to_string(file_name).unwrap();

    let ast = ast::Suite::parse(&python_source, "<embedded>").unwrap();

    let mut context = TranslationContext {
        defined_variables: HashMap::new(),
        indentation: 0,
        owning_class: None,
        owning_function: None,
        output: String::new(),
    };

    for stmt in ast {
        context.visit(stmt);
    }

    println!("{}", context.output);
}