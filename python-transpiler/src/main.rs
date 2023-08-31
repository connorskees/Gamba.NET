use core::panic;
use std::{
    collections::{HashMap, HashSet},
    hash::Hash,
    mem,
};

use rustpython_parser::{
    ast::{
        self, BoolOp, CmpOp, Comprehension, Expr, Operator, StmtClassDef, StmtFunctionDef, UnaryOp,
    },
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
        if s == "self" || s == "this" {
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
            ast::Stmt::Delete(def) => self.visit_delete_statement(def),
            ast::Stmt::TypeAlias(_) => todo!(),
            ast::Stmt::AugAssign(def) => self.visit_aug_assign(def),
            ast::Stmt::AnnAssign(def) => self.visit_ann_assign(def),
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
            ast::Stmt::Assert(def) => self.visit_assert(def),
            ast::Stmt::Import(_) => {}
            ast::Stmt::ImportFrom(_) => {}
            ast::Stmt::Global(_) => todo!(),
            ast::Stmt::Nonlocal(_) => todo!(),
            ast::Stmt::Expr(stmt_expr) => self.visit_stmt_expr(stmt_expr),
            ast::Stmt::Pass(_) => todo!(),
            ast::Stmt::Break(_) => println!("{}break;", self.indent()),
            ast::Stmt::Continue(_) => println!("{}continue;", self.indent()),
        }
    }

    fn visit_for(&mut self, for_stmt: ast::StmtFor) {
        assert_eq!(for_stmt.orelse.len(), 0);

        println!(
            "{}foreach (var {} in {}) {{",
            self.indent(),
            self.visit_expr(*for_stmt.target),
            self.visit_expr(*for_stmt.iter)
        );

        self.visit_body(for_stmt.body);
        println!("{}}}", self.indent());
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

    fn visit_assert(&mut self, assert_stmt: ast::StmtAssert) {
        println!(
            "{}Assert.True({});",
            self.indent(),
            self.visit_expr(*assert_stmt.test)
        );
    }

    fn visit_return_stmt(&mut self, return_stmt: ast::StmtReturn) {
        if let Some(value) = return_stmt.value {
            println!("{}return {};", self.indent(), self.visit_expr(*value));
        } else {
            println!("{}return;", self.indent());
        }
    }

    fn visit_delete_statement(&mut self, def: ast::StmtDelete) {
        assert_eq!(def.targets.len(), 1);
        assert!(def.targets[0].is_subscript_expr() == true);

        let subscript = def.targets[0].as_subscript_expr().unwrap();
        println!(
            "{}{}.RemoveAt({});",
            self.indent(),
            self.visit_expr(*subscript.clone().value),
            self.visit_expr(*subscript.clone().slice)
        );
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
        let name = arg.as_arg().arg.to_string();
        let default = arg
            .default
            .as_ref()
            .map(|default| format!("={}", self.visit_expr(*default.clone())));

        if (name == "this" || name == "self") {
            return format!("{}", "this");
        }

        if arg.clone().def.annotation.is_none() {
            dbg!(arg.clone().def);
            panic!("Function argument {} does not have type annotation!", name);
        }

        let annotation = arg.clone().def.annotation.unwrap();
        let type_name = self.visit_expr(*annotation);
        let ty = self.type_to_csharp(type_name.to_string());
        if let Some(default) = default {
            format!("{ty} {name}{default}")
        } else {
            format!("{ty} {name}")
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
                ast::Constant::None => "void",
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
                .filter(|arg| arg.to_string() != "this")
                .collect::<Vec<_>>()
                .join(", "),
            self.indent(),
        );

        if !self.defined_variables.contains_key(&def.clone().range) {
            self.defined_variables
                .insert(def.clone().range, HashSet::new());
        }

        let mapping = self.defined_variables.get_mut(&def.range).unwrap();
        for arg in def.clone().args.args {
            let arg_name = arg.def.arg.to_string();
            if (!mapping.contains(&arg_name)) {
                mapping.insert(arg_name.to_string());
            }
        }

        mem::swap(&mut self.owning_function, &mut Some(def.clone()));
        self.visit_body(def.clone().body);
        mem::swap(&mut self.owning_function, &mut Some(def.clone()));

        println!("{}}}\n", self.indent());
    }

    fn function_return_type(&mut self, def: &ast::StmtFunctionDef) -> String {
        if def.name.as_str() == "__init__" {
            return String::new();
        }

        //println!("def:\n");
        //dbg!(def.clone());
        let return_type_str = self.visit_expr(*def.clone().returns.unwrap());

        return self.type_to_csharp(return_type_str);
    }

    fn type_to_csharp(&mut self, type_name: String) -> String {
        let mut result = match type_name.clone().as_str() {
            "None" => "void".to_owned(),
            "null" => "void".to_owned(),
            "int" => "int".to_owned(),
            "i64" => "long".to_owned(),
            "bool" => "bool".to_owned(),
            "str" => "string".to_owned(),
            "Node" => "Node".to_owned(),
            "NodeType" => "NodeType".to_owned(),
            "Batch" => "Batch".to_owned(),
            "IndexWithMultitude" => "IndexWithMultitude".to_owned(),
            "list[str]" => "List<string>".to_owned(),
            "list[bool]" => "List<bool>".to_owned(),
            "list[int]" => "List<int>".to_owned(),
            "list[i64]" => "List<long>".to_owned(),
            "list[Node]" => "List<Node>".to_owned(),
            "list[NodeType]" => "List<NodeType>".to_owned(),
            "list[Any]" => "List<object>".to_owned(),
            "list[int]" => "List<int>".to_owned(),
            "list[list[int]]" => "List<List<int>>".to_owned(),
            "list[tuple[(int, int)]]" => "List<(int, int)>".to_owned(),
            "list[set[IndexWithMultitude]]" => "List<HashSet<IndexWithMultitude>>".to_owned(),
            "set[IndexWithMultitude]" => "HashSet<IndexWithMultitude>".to_owned(),
            "Optional[int]" => "int?".to_owned(),
            "Optional[bool]" => "bool?".to_owned(),
            "Optional[i64]" => "long?".to_owned(),
            "Optional[NodeType]" => "NodeType?".to_owned(),
            "tuple[(Optional[int], Optional[int])]" => "(int?, int?)".to_owned(),
            "tuple[(Optional[i64], Optional[i64])]" => "(long?, long?)".to_owned(),
            "tuple[(Optional[int], Node)]" => "(int?, Node)".to_owned(),
            "tuple[(Optional[i64], Node)]" => "(long?, Node)".to_owned(),
            "tuple[(Node, Optional[i64])]" => "(Node, long?)".to_owned(),
            "Optional[tuple[(int, int)]]" => "(int, int)?".to_owned(),
            "tuple[(bool, bool, tuple[(int, int, int)])]" => {
                "(bool, bool, (int, int, int))".to_owned()
            }
            "tuple[(bool, bool, tuple[(i64, i64, i64)])]" => {
                "(bool, bool, (long, long, long))".to_owned()
            }
            "tuple[(bool, bool)]" => "(bool, bool)".to_owned(),
            "tuple[(Node, Optional[int])]" => "(Node, int?)".to_owned(),
            "tuple[(Node, Optional[int])]" => "(Node, int?)".to_owned(),
            "tuple[(Node, Node)]" => "(Node, Node)".to_owned(),
            "tuple[(Node, int)]" => "(Node, int)".to_owned(),
            "tuple[(int, Node)]" => "(int, Node)".to_owned(),
            "tuple[(int, int, bool)]" => "(int, int, bool)".to_owned(),
            "tuple[(int, int)]" => "(int, int)".to_owned(),
            "tuple[(i64, i64)]" => "(long, long)".to_owned(),
            "tuple[(int, int, int)]" => "(int, int, int)".to_owned(),
            "tuple[(i64, i64, i64)]" => "(long, long, long)".to_owned(),
            "tuple[(int, bool, int)]" => "(int, bool, int)".to_owned(),
            "tuple[(i64, bool, i64)]" => "(i64, bool, i64)".to_owned(),
            "tuple[(list[Node], list[Any], list[IndexWithMultitude])]" => {
                "(List<Node>, List<object>, List<IndexWithMultitude>)".to_owned()
            }
            "tuple[(list[Node], list[Any], list[set[IndexWithMultitude]])]" => {
                "(List<Node>, List<object>, List<HashSet<IndexWithMultitude>>)".to_owned()
            }
            "list[tuple[(int, list[int])]]" => "List<(int, List<int>)>".to_owned(),
            _ => {
                dbg!(type_name);
                todo!()
            }
        };

        // Wrap nullable integers in our own C# wrapper struct.
        // This is necessary
        result = result.replace("long?", "NullableI64");
        result = result.replace("int?", "NullableI32");

        return result;
    }

    fn visit_assign(&mut self, def: ast::StmtAssign) {
        assert_eq!(def.targets.len(), 1, "too many assignment targets");

        let target = def.targets.first().unwrap();

        let value_str = self.visit_expr(*def.clone().value);

        self.process_assign(target.clone(), Option::None, value_str)
    }

    fn visit_ann_assign(&mut self, def: ast::StmtAnnAssign) {
        let value_str = self.visit_expr(*def.clone().value.unwrap());

        let ty_str = self.visit_expr(*def.clone().annotation);

        let csharp_ty_str = self.type_to_csharp(ty_str);

        self.process_assign(
            *def.clone().target.clone(),
            Option::from(csharp_ty_str),
            value_str,
        )
    }

    fn process_assign(&mut self, target: Expr, assign_type: Option<String>, value: String) {
        let ty = if assign_type.is_some() {
            assign_type.unwrap()
        } else {
            "var".to_owned()
        };

        let var_name = match target.clone() {
            // Match self.instance_variable
            ast::Expr::Attribute(def) => {
                let value = self.visit_expr(*def.clone().value).replace("self", "this");
                format!(
                    "{}.{}",
                    Self::self_to_this(value),
                    def.clone().attr.to_string()
                )
            }

            // Match "local_variable ="
            ast::Expr::Name(def) => {
                let is_first_definition = self.is_first_time_seeing_var(def.id.to_string());

                format!(
                    "{}",
                    if is_first_definition {
                        format!("{} {}", ty, def.id.to_string())
                    } else {
                        def.id.to_string()
                    }
                )
            }

            ast::Expr::Subscript(def) => {
                format!("{}", self.visit_expr(target.clone()))
            }

            ast::Expr::Tuple(def) => {
                // If a tuple is assigned to then it's always considered an 'ExprContextStore'.
                let context: ast::ExprContextStore = def.ctx.store().unwrap();

                format!(
                    "{} ({})",
                    ty,
                    def.clone()
                        .elts
                        .into_iter()
                        .map(|var| self.visit_expr(var))
                        .collect::<Vec<_>>()
                        .join(", ")
                )
            }

            _ => {
                dbg!(target.clone());

                todo!(
                    "Unexpected assignment destination type: {}",
                    target.python_name()
                )
            }
        };

        // Print "var = "
        println!("{}{} = {};", self.indent(), var_name, value);
    }

    fn is_first_time_seeing_var(&mut self, name: String) -> bool {
        let mut is_first_definition = false;

        if let Some(owning_function) = self.owning_function.clone() {
            // Add a new Hashset of variable names corresponding to the current owning function.
            if !self.defined_variables.contains_key(&owning_function.range) {
                self.defined_variables
                    .insert(owning_function.range, HashSet::new());
            }

            // Get the hashset.
            let map = self
                .defined_variables
                .get_mut(&owning_function.range)
                .unwrap();

            // Check if the variable has any definition.
            // A variable is 'defined' if it's an argument or if an assignment to a variable with the current name
            // has already been processed.
            is_first_definition = !map.contains(&name);

            if is_first_definition {
                map.insert(name.to_string());
            }
        }

        return is_first_definition;
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
                name = name.replace("re.match", "reutil.match");

                // TODO: Refactor into helper method.
                name = if name.ends_with(".equals") {
                    name.replace(".equals", ".Equals")
                } else {
                    name
                };

                name = if name.ends_with(".append") {
                    name.replace(".append", ".Add")
                } else {
                    name
                };

                name = if name.ends_with(".insert") {
                    name.replace(".insert", ".Insert")
                } else {
                    name
                };

                name = if name.ends_with(".index") {
                    name.replace(".index", ".IndexOf")
                } else {
                    name
                };

                name = if name == "int" {
                    name.replace("int", "Convert.ToInt32")
                } else {
                    name
                };

                name = if name == "list" {
                    name.replace("list", "new")
                } else {
                    name
                };

                name = if name == "min" {
                    name.replace("min", "Math.Min")
                } else {
                    name
                };

                name = if name == "reversed" {
                    name.replace("reversed", "ListUtil.Reversed")
                } else {
                    name
                };

                name = if name.ends_with(".sort") {
                    name.replace(".sort", ".Sort")
                } else {
                    name
                };

                name = if name.ends_with(".remove") {
                    name.replace(".remove", ".Remove")
                } else {
                    name
                };

                name = if name.ends_with(".extend") {
                    name.replace(".extend", ".AddRange")
                } else {
                    name
                };

                name = if name.ends_with(".pop") {
                    name.replace(".pop", ".Pop")
                } else {
                    name
                };

                name = if name.ends_with("range") {
                    name.replace("range", "Range.Get")
                } else {
                    name
                };

                name = if name.ends_with("sys.exit") {
                    name.replace("sys.exit", "throw new InvalidOperationException")
                } else {
                    name
                };

                if name == "len" && call_expr.args.len() == 1 {
                    format!(
                        "{}.Count()",
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
                        .map(|expr| format!("({})", self.visit_expr(expr)))
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
                        "({}) {} ({})",
                        self.visit_expr(*def.left),
                        cmp_to_string(op),
                        self.visit_expr(def.comparators[0].clone())
                    ),
                }
            }
            ast::Expr::BinOp(def) => {
                // C# does not have a power operator, so we must embed
                // ** uses into a .Pow() helper method.
                let op = def.op;
                if op == Operator::Pow {
                    return format!(
                        "LongPower({}, {})",
                        self.visit_expr(*def.left),
                        self.visit_expr(*def.right)
                    );
                }

                format!(
                    "(({}) {} ({}))",
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
                    "new () {{ {} }}",
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
            ast::Expr::IfExp(def) => {
                format!(
                    "({}) ? {} : {}",
                    self.visit_expr(*def.test),
                    self.visit_expr(*def.body),
                    self.visit_expr(*def.orelse)
                )
            }
            ast::Expr::Tuple(def) => {
                format!(
                    "({})",
                    def.clone()
                        .elts
                        .into_iter()
                        .map(|var| self.visit_expr(var))
                        .collect::<Vec<_>>()
                        .join(", ")
                )
            }
            ast::Expr::ListComp(def) => {
                assert!(def.generators.len() == 1);

                // Convert the generator into a LINQ 'While(x => predicate1 && predicate2 && predicate ... )' query.
                let evaluated_generator = self.visit_generator(def.generators[0].clone());

                // LINQ queries in C# follow the format of collection.Query(x => lambda_expression),
                // where "x" is a variable name used to represent the input variable to the lambda.
                // So first we must pick the variable name to use. If the generator supplies a name then we use it,
                // if not we default to 'x';
                let select_var_name = if def.elt.is_name_expr() {
                    def.clone().elt.expect_name_expr().id.to_string()
                } else {
                    "x".to_owned()
                };

                // Here 'elt' is equivalent to the lamba on the right hand side of a linq query.
                // E.g. if you have expression_list.Where(x => x.IsLinear()), this is equivalent to "x.IsLinear()".
                let lambda = self.visit_expr(*def.elt.clone());

                format!(
                    "{}.Select({} => {})",
                    evaluated_generator, select_var_name, lambda
                )
            }
            ast::Expr::NamedExpr(_)
            | ast::Expr::Lambda(_)
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
            | ast::Expr::Starred(_) => todo!("Unimplemented expression type {:#?}", expr),
        }
    }

    fn visit_generator(&mut self, comp: Comprehension) -> String {
        // A comprehension in Python can be modeled as a series of LINQ queries.
        // First you have the collection being queried on - which in this case is the 'iter' field of the comprehension.
        let iterable_collection = self.visit_expr(comp.iter);

        // Then the collection can be filtered using a series of if statements / predicates.
        // We then filter the collection using .Where(if1 && if2 && ... ).
        let linq_where_predicate = comp
            .ifs
            .iter()
            .map(|predicate| self.visit_expr(predicate.clone()))
            .collect::<Vec<_>>()
            .join(" && ");

        // Model the comprehe
        let comprehension = format!(
            "{}.Where({} => {})",
            iterable_collection,
            self.visit_expr(comp.target),
            linq_where_predicate
        );

        return comprehension.to_owned();
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
        Operator::FloorDiv => "/",
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
