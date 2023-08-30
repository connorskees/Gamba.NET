using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiled
{
    public static class Assert
    {
        public static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("Assertio failure.");
        }
    }

    public class Node
    {
        public enum NodeType
        {
            CONSTANT = 0,
            VARIABLE = 1,
            POWER = 2,
            NEGATION = 3,
            PRODUCT = 4,
            SUM = 5,
            CONJUNCTION = 6,
            EXCL_DISJUNCTION = 7,
            INCL_DISJUNCTION = 8,
        }

        public enum NodeState
        {
            UNKNOWN = 0,
            BITWISE = 1,
            LINEAR = 2,
            NONLINEAR = 3,
            MIXED = 4
        }

        public NodeType type;

        public ulong __modulus;

        public bool __modRed;

        public List<dynamic> children;

        public string vname = "";

        public long __vidx = -1;

        public ulong constant = 0;

        public NodeState state = NodeState.UNKNOWN;

        public ulong linearEnd = 0;

        public ulong __MAX_IT = 10;

        public Node(dynamic nodeType, dynamic modulus, bool modRed = false)
        {
            this.type = nodeType;
            this.children = new List<dynamic>() { };
            this.vname = "";
            this.__vidx = -(1);
            this.constant = 0;
            this.state = NodeState.UNKNOWN;
            this.__modulus = modulus;
            this.__modRed = modRed;
            this.linearEnd = 0;
            this.__MAX_IT = 10;
        }

        public dynamic __str__()
        {
            return this.to_string();
        }

        public void to_string(bool withParentheses = false, dynamic end = -(1), void varNames = null)
        {
            if (end == -(1))
            {
                var end = this.children.Count();
            }
            else
            {
                Assert.True(end <= this.children.Count());
            }
            if (this.type == NodeType.CONSTANT)
            {
                return str(this.constant);
            }
            if (this.type == NodeType.VARIABLE)
            {
                return (varNames == null) ? this.vname : varNames[this.__vidx];
            }
            if (this.type == NodeType.POWER)
            {
                Assert.True(this.children.Count() == 2);
                var child1 = this.children[0];
                var child2 = this.children[1];
                var ret = child1.to_string(child1.type > NodeType.VARIABLE, -(1), varNames) + "**" + child2.to_string(child2.type > NodeType.VARIABLE, -(1), varNames);
                if (withParentheses)
                {
                    ret = "(" + ret + ")";
                }
                return ret;
            }
            if (this.type == NodeType.NEGATION)
            {
                Assert.True(this.children.Count() == 1);
                var child = this.children[0];
                ret = "~" + child.to_string(child.type > NodeType.NEGATION, -(1), varNames);
                if (withParentheses)
                {
                    ret = "(" + ret + ")";
                }
                return ret;
            }
            if (this.type == NodeType.PRODUCT)
            {
                Assert.True(this.children.Count() > 0);
                child1 = this.children[0];
                var ret1 = child1.to_string(child1.type > NodeType.PRODUCT, -(1), varNames);
                ret = ret1;
                foreach (var child in this.children.Slice(1, end, null))
                {
                    ret += "*" + child.to_string(child.type > NodeType.PRODUCT, -(1), varNames);
                }
                if ((ret1 == "-1" && this.children.Count() > 1 && end > 1))
                {
                    ret = "-" + ret.Slice(3, null, null);
                }
                if (withParentheses)
                {
                    ret = "(" + ret + ")";
                }
                return ret;
            }
            if (this.type == NodeType.SUM)
            {
                Assert.True(this.children.Count() > 0);
                child1 = this.children[0];
                ret = child1.to_string(child1.type > NodeType.SUM, -(1), varNames);
                foreach (var child in this.children.Slice(1, end, null))
                {
                    var s = child.to_string(child.type > NodeType.SUM, -(1), varNames);
                    if (s[0] != "-")
                    {
                        ret += "+";
                    }
                    ret += s;
                }
                if (withParentheses)
                {
                    ret = "(" + ret + ")";
                }
                return ret;
            }
            if (this.type == NodeType.CONJUNCTION)
            {
                Assert.True(this.children.Count() > 0);
                child1 = this.children[0];
                ret = child1.to_string(child1.type > NodeType.CONJUNCTION, -(1), varNames);
                foreach (var child in this.children.Slice(1, end, null))
                {
                    ret += "&" + child.to_string(child.type > NodeType.CONJUNCTION, -(1), varNames);
                }
                if (withParentheses)
                {
                    ret = "(" + ret + ")";
                }
                return ret;
            }
            else
            {
                if (this.type == NodeType.EXCL_DISJUNCTION)
                {
                    Assert.True(this.children.Count() > 0);
                    child1 = this.children[0];
                    ret = child1.to_string(child1.type > NodeType.EXCL_DISJUNCTION, -(1), varNames);
                    foreach (var child in this.children.Slice(1, end, null))
                    {
                        ret += "^" + child.to_string(child.type > NodeType.EXCL_DISJUNCTION, -(1), varNames);
                    }
                    if (withParentheses)
                    {
                        ret = "(" + ret + ")";
                    }
                    return ret;
                }
            }
            if (this.type == NodeType.INCL_DISJUNCTION)
            {
                Assert.True(this.children.Count() > 0);
                child1 = this.children[0];
                ret = child1.to_string(child1.type > NodeType.INCL_DISJUNCTION, -(1), varNames);
                foreach (var child in this.children.Slice(1, end, null))
                {
                    ret += "|" + child.to_string(child.type > NodeType.INCL_DISJUNCTION, -(1), varNames);
                }
                if (withParentheses)
                {
                    ret = "(" + ret + ")";
                }
                return ret;
            }
            Assert.True(false);
        }

        public dynamic part_to_string(dynamic end)
        {
            return this.to_string(false, end);
        }

        public dynamic __power(dynamic b, dynamic e)
        {
            return power(b, e, this.__modulus);
        }

        public dynamic __get_reduced_constant(dynamic c)
        {
            if (this.__modRed)
            {
                return mod_red(c, this.__modulus);
            }
            return this.__get_reduced_constant_closer_to_zero(c);
        }

        public dynamic __get_reduced_constant_closer_to_zero(dynamic c)
        {
            var c = mod_red(c, this.__modulus);
            if (2 * c > this.__modulus)
            {
                c -= this.__modulus;
            }
            return c;
        }

        public void __reduce_constant()
        {
            this.constant = this.__get_reduced_constant(this.constant);
        }

        public void __set_and_reduce_constant(dynamic c)
        {
            this.constant = this.__get_reduced_constant(c);
        }

        public void collect_and_enumerate_variables(dynamic variables)
        {
            this.collect_variables(variables);
            variables.sort();
            this.enumerate_variables(variables);
        }

        public void collect_variables(dynamic variables)
        {
            if (this.type == NodeType.VARIABLE)
            {
                if (!(((variables).Contains(this.vname))))
                {
                    variables.Add(this.vname);
                }
            }
            else
            {
                foreach (var child in this.children)
                {
                    child.collect_variables(variables);
                }
            }
        }

        public void enumerate_variables(dynamic variables)
        {
            if (this.type == NodeType.VARIABLE)
            {
                Assert.True(((variables).Contains(this.vname)));
                this.__vidx = variables.index(this.vname);
            }
            else
            {
                foreach (var child in this.children)
                {
                    child.enumerate_variables(variables);
                }
            }
        }

        public void get_max_vname(dynamic start, dynamic end)
        {
            if (this.type == NodeType.VARIABLE)
            {
                if (this.vname.Slice(null, start.Count(), null) != start)
                {
                    return null;
                }
                if (this.vname.Slice(-(end.Count()), null, null) != end)
                {
                    return null;
                }
                var n = this.vname.Slice(start.Count(), -(end.Count()), null);
                if (!(n.isnumeric()))
                {
                    return null;
                }
                return int(n);
            }
            else
            {
                var maxn = null;
                foreach (var child in this.children)
                {
                    n = child.get_max_vname(start, end);
                    if ((n != null && (maxn == null || n > maxn)))
                    {
                        maxn = n;
                    }
                }
                return maxn;
            }
        }

        public dynamic eval(dynamic X)
        {
            if (this.type == NodeType.CONSTANT)
            {
                return this.constant % this.__modulus;
            }
            if (this.type == NodeType.VARIABLE)
            {
                if (this.__vidx < 0)
                {
                    sys.exit("ERROR: Variable index not set prior to evaluation!");
                }
                return X[this.__vidx] % this.__modulus;
            }
            Assert.True(this.children.Count() > 0);
            if (this.type == NodeType.NEGATION)
            {
                return ~(this.children[0].eval(X)) % this.__modulus;
            }
            var val = this.children[0].eval(X);
            foreach (var i in range(1, this.children.Count()))
            {
                val = this.__apply_binop(val, this.children[i].eval(X)) % this.__modulus;
            }
            return val;
        }

        public dynamic __apply_binop(dynamic x, dynamic y)
        {
            if (this.type == NodeType.POWER)
            {
                return this.__power(x, y);
            }
            if (this.type == NodeType.PRODUCT)
            {
                return x * y;
            }
            if (this.type == NodeType.SUM)
            {
                return x + y;
            }
            return this.__apply_bitwise_binop(x, y);
        }

        public ulong __apply_bitwise_binop(dynamic x, dynamic y)
        {
            if (this.type == NodeType.CONJUNCTION)
            {
                return x & y;
            }
            if (this.type == NodeType.EXCL_DISJUNCTION)
            {
                return x ^ y;
            }
            if (this.type == NodeType.INCL_DISJUNCTION)
            {
                return x | y;
            }
            Assert.True(false);
            return 0;
        }

        public bool has_nonlinear_child()
        {
            foreach (var child in this.children)
            {
                if ((child.state == NodeState.NONLINEAR || child.state == NodeState.MIXED))
                {
                    return true;
                }
            }
            return false;
        }

        public dynamic __are_all_children_contained(dynamic other)
        {
            Assert.True(other.type == this.type);
            return are_all_children_contained(this.children, other.children);
        }

        public dynamic equals(dynamic other)
        {
            if (this.type != other.type)
            {
                return this.__equals_rewriting_bitwise(other);
            }
            if (this.type == NodeType.CONSTANT)
            {
                return this.constant == other.constant;
            }
            if (this.type == NodeType.VARIABLE)
            {
                return this.vname == other.vname;
            }
            if (this.children.Count() != other.children.Count())
            {
                return false;
            }
            return this.__are_all_children_contained(other);
        }

        public bool equals_negated(dynamic other)
        {
            if ((this.type == NodeType.NEGATION && this.children[0].Equals(other)))
            {
                return true;
            }
            if ((other.type == NodeType.NEGATION && other.children[0].Equals(self)))
            {
                return true;
            }
            return false;
        }

        public bool __equals_rewriting_bitwise(dynamic other)
        {
            return (this.__equals_rewriting_bitwise_asymm(other) || other.__equals_rewriting_bitwise_asymm(self));
        }

        public bool __equals_rewriting_bitwise_asymm(dynamic other)
        {
            if (this.type == NodeType.NEGATION)
            {
                var node = other.__get_opt_transformed_negated();
                return (node != null && node.Equals(this.children[0]));
            }
            if (this.type == NodeType.PRODUCT)
            {
                if (this.children.Count() != 2)
                {
                    return false;
                }
                if (!(this.children[0].__is_constant(-(1))))
                {
                    return false;
                }
                if (this.children[1].type != NodeType.NEGATION)
                {
                    return false;
                }
                node = other.__get_opt_negative_transformed_negated();
                return (node != null && node.Equals(this.children[1].children[0]));
            }
            return false;
        }

        public dynamic __get_opt_negative_transformed_negated()
        {
            if (this.type != NodeType.SUM)
            {
                return null;
            }
            if (this.children.Count() < 2)
            {
                return null;
            }
            if (!(this.children[0].__is_constant(1)))
            {
                return null;
            }
            var res = this.__new_node(NodeType.SUM);
            foreach (var i in range(1, this.children.Count()))
            {
                res.children.Add(this.children[i].get_copy());
            }
            Assert.True(res.children.Count() > 0);
            if (res.children.Count() == 1)
            {
                return res.children[0];
            }
            return res;
        }

        public void __remove_children_of_node(dynamic other)
        {
            foreach (var ochild in other.children)
            {
                foreach (var i in range(this.children.Count()))
                {
                    if (this.children[i].Equals(ochild))
                    {
                        this.children.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public Node __new_node(dynamic t)
        {
            return new Node(t, this.__modulus, this.__modRed);
        }

        public Node __new_constant_node(dynamic constant)
        {
            var node = this.__new_node(NodeType.CONSTANT);
            node.constant = constant;
            node.__reduce_constant();
            return node;
        }

        public Node __new_variable_node(dynamic vname)
        {
            var node = this.__new_node(NodeType.VARIABLE);
            node.vname = vname;
            return node;
        }

        public Node __new_node_with_children(dynamic t, dynamic children)
        {
            var node = this.__new_node(t);
            node.children = children;
            return node;
        }

        public void replace_variable_by_constant(dynamic vname, dynamic constant)
        {
            this.replace_variable(vname, this.__new_constant_node(constant));
        }

        public void replace_variable(dynamic vname, dynamic node)
        {
            if (this.type == NodeType.VARIABLE)
            {
                if (this.vname == vname)
                {
                    this.__copy_all(node);
                }
                return;
            }
            foreach (var child in this.children)
            {
                child.replace_variable(vname, node);
            }
        }

        public void refine(void parent = null, bool restrictedScope = false)
        {
            foreach (var i in range(this.__MAX_IT))
            {
                this.__refine_step_1(restrictedScope);
                if (!(this.__refine_step_2(parent, restrictedScope)))
                {
                    return;
                }
            }
        }

        public void __refine_step_1(bool restrictedScope = false)
        {
            if (!(restrictedScope))
            {
                foreach (var c in this.children)
                {
                    c.__refine_step_1();
                }
            }
            this.__inspect_constants();
            this.__flatten();
            this.__check_duplicate_children();
            this.__resolve_inverse_nodes();
            this.__remove_trivial_nodes();
        }

        public void __inspect_constants()
        {
            if (this.type == NodeType.INCL_DISJUNCTION)
            {
                this.__inspect_constants_incl_disjunction();
            }
            else
            {
                if (this.type == NodeType.EXCL_DISJUNCTION)
                {
                    this.__inspect_constants_excl_disjunction();
                }
                else
                {
                    if (this.type == NodeType.CONJUNCTION)
                    {
                        this.__inspect_constants_conjunction();
                    }
                    else
                    {
                        if (this.type == NodeType.SUM)
                        {
                            this.__inspect_constants_sum();
                        }
                        else
                        {
                            if (this.type == NodeType.PRODUCT)
                            {
                                this.__inspect_constants_product();
                            }
                            else
                            {
                                if (this.type == NodeType.NEGATION)
                                {
                                    this.__inspect_constants_negation();
                                }
                                else
                                {
                                    if (this.type == NodeType.POWER)
                                    {
                                        this.__inspect_constants_power();
                                    }
                                    else
                                    {
                                        if (this.type == NodeType.CONSTANT)
                                        {
                                            this.__inspect_constants_constant();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void __inspect_constants_incl_disjunction()
        {
            var first = this.children[0];
            var isMinusOne = first.__is_constant(-(1));
            var toRemove = new List<dynamic>() { };
            if (!(isMinusOne))
            {
                foreach (var child in this.children.Slice(1, null, null))
                {
                    if (child.type == NodeType.CONSTANT)
                    {
                        if (child.constant == 0)
                        {
                            toRemove.Add(child);
                            continue;
                        }
                        if (child.__is_constant(-(1)))
                        {
                            isMinusOne = true;
                            break;
                        }
                        first = this.children[0];
                        if (first.type == NodeType.CONSTANT)
                        {
                            first.__set_and_reduce_constant(first.constant | child.constant);
                            toRemove.Add(child);
                        }
                        else
                        {
                            this.children.remove(child);
                            this.children.Insert(0, child);
                        }
                    }
                }
            }
            if (isMinusOne)
            {
                this.children = new List<dynamic>() { };
                this.type = NodeType.CONSTANT;
                this.__set_and_reduce_constant(-(1));
                return;
            }
            foreach (var child in toRemove)
            {
                this.children.remove(child);
            }
            first = this.children[0];
            if ((this.children.Count() > 1 && first.__is_constant(0)))
            {
                this.children.pop(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
        }

        public bool __is_constant(dynamic value)
        {
            return (this.type == NodeType.CONSTANT && mod_red(this.constant - value, this.__modulus) == 0);
        }

        public void __inspect_constants_excl_disjunction()
        {
            var toRemove = new List<dynamic>() { };
            foreach (var child in this.children.Slice(1, null, null))
            {
                if (child.type == NodeType.CONSTANT)
                {
                    if (child.constant == 0)
                    {
                        toRemove.Add(child);
                        continue;
                    }
                    var first = this.children[0];
                    if (first.type == NodeType.CONSTANT)
                    {
                        first.__set_and_reduce_constant(first.constant ^ child.constant);
                        toRemove.Add(child);
                    }
                    else
                    {
                        this.children.remove(child);
                        this.children.Insert(0, child);
                    }
                }
            }
            foreach (var child in toRemove)
            {
                this.children.remove(child);
            }
            first = this.children[0];
            if ((this.children.Count() > 1 && first.__is_constant(0)))
            {
                this.children.pop(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
        }

        public void __inspect_constants_conjunction()
        {
            var first = this.children[0];
            var isZero = first.__is_constant(0);
            var toRemove = new List<dynamic>() { };
            if (!(isZero))
            {
                foreach (var child in this.children.Slice(1, null, null))
                {
                    if (child.type == NodeType.CONSTANT)
                    {
                        if (child.__is_constant(-(1)))
                        {
                            toRemove.Add(child);
                            continue;
                        }
                        if (child.constant == 0)
                        {
                            isZero = true;
                            break;
                        }
                        first = this.children[0];
                        if (first.type == NodeType.CONSTANT)
                        {
                            first.__set_and_reduce_constant(first.constant & child.constant);
                            toRemove.Add(child);
                        }
                        else
                        {
                            this.children.remove(child);
                            this.children.Insert(0, child);
                        }
                    }
                }
            }
            if (isZero)
            {
                this.children = new List<dynamic>() { };
                this.type = NodeType.CONSTANT;
                this.constant = 0;
                return;
            }
            foreach (var child in toRemove)
            {
                this.children.remove(child);
            }
            first = this.children[0];
            if ((this.children.Count() > 1 && first.__is_constant(-(1))))
            {
                this.children.pop(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
        }

        public void __inspect_constants_sum()
        {
            var first = this.children[0];
            var toRemove = new List<dynamic>() { };
            foreach (var child in this.children.Slice(1, null, null))
            {
                if (child.type == NodeType.CONSTANT)
                {
                    if (child.constant == 0)
                    {
                        toRemove.Add(child);
                        continue;
                    }
                    first = this.children[0];
                    if (first.type == NodeType.CONSTANT)
                    {
                        first.__set_and_reduce_constant(first.constant + child.constant);
                        toRemove.Add(child);
                    }
                    else
                    {
                        this.children.remove(child);
                        this.children.Insert(0, child);
                    }
                }
            }
            foreach (var child in toRemove)
            {
                this.children.remove(child);
            }
            first = this.children[0];
            if ((this.children.Count() > 1 && first.__is_constant(0)))
            {
                this.children.pop(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
        }

        public void __inspect_constants_product()
        {
            var first = this.children[0];
            var isZero = first.__is_constant(0);
            var toRemove = new List<dynamic>() { };
            if (!(isZero))
            {
                foreach (var child in this.children.Slice(1, null, null))
                {
                    if (child.type == NodeType.CONSTANT)
                    {
                        if (child.constant == 1)
                        {
                            toRemove.Add(child);
                            continue;
                        }
                        if (child.constant == 0)
                        {
                            isZero = true;
                            break;
                        }
                        first = this.children[0];
                        if (first.type == NodeType.CONSTANT)
                        {
                            first.__set_and_reduce_constant(first.constant * child.constant);
                            toRemove.Add(child);
                        }
                        else
                        {
                            this.children.remove(child);
                            this.children.Insert(0, child);
                        }
                    }
                }
            }
            if (isZero)
            {
                this.children = new List<dynamic>() { };
                this.type = NodeType.CONSTANT;
                this.constant = 0;
                return;
            }
            foreach (var child in toRemove)
            {
                this.children.remove(child);
            }
            first = this.children[0];
            if ((this.children.Count() > 1 && first.__is_constant(1)))
            {
                this.children.pop(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
        }

        public void __inspect_constants_negation()
        {
            Assert.True(this.children.Count() == 1);
            var child = this.children[0];
            if (child.type == NodeType.NEGATION)
            {
                this.copy(child.children[0]);
            }
            else
            {
                if (child.type == NodeType.CONSTANT)
                {
                    child.__set_and_reduce_constant(-(child.constant) - 1);
                    this.copy(child);
                }
            }
        }

        public void __inspect_constants_power()
        {
            Assert.True(this.children.Count() == 2);
            var base = this.children[0];
            var exp = this.children[1];
            if ((base.type == NodeType.CONSTANT && exp.type == NodeType.CONSTANT))
            {
                base.__set_and_reduce_constant(this.__power(base.constant, exp.constant));
                this.copy(base);
                return;
            }
            if (exp.type == NodeType.CONSTANT)
            {
                if (exp.constant == 0)
                {
                    this.type = NodeType.CONSTANT;
                    this.constant = 1;
                    this.children = new List<dynamic>() { };
                }
                else
                {
                    if (exp.constant == 1)
                    {
                        this.copy(base);
                    }
                }
            }
        }

        public void __inspect_constants_constant()
        {
            this.__reduce_constant();
        }

        public void copy(dynamic node)
        {
            this.type = node.type;
            this.state = node.state;
            this.children = node.children;
            this.vname = node.vname;
            this.__vidx = node.__vidx;
            this.constant = node.constant;
        }

        public void __copy_all(dynamic node)
        {
            this.type = node.type;
            this.state = node.state;
            this.children = new List<dynamic>() { };
            this.vname = node.vname;
            this.__vidx = node.__vidx;
            this.constant = node.constant;
            foreach (var child in node.children)
            {
                this.children.Add(child.get_copy());
            }
        }

        public void __copy_children()
        {
            foreach (var i in range(this.children.Count()))
            {
                this.children[i] = this.children[i].get_copy();
                this.children[i].__copy_children();
            }
        }

        public dynamic get_copy()
        {
            var n = this.__new_node(this.type);
            n.state = this.state;
            n.children = new List<dynamic>() { };
            n.vname = this.vname;
            n.__vidx = this.__vidx;
            n.constant = this.constant;
            foreach (var child in this.children)
            {
                n.children.Add(child.get_copy());
            }
            return n;
        }

        public dynamic __get_shallow_copy()
        {
            var n = this.__new_node(this.type);
            n.state = this.state;
            n.children = new List<dynamic>() { };
            n.vname = this.vname;
            n.__vidx = this.__vidx;
            n.constant = this.constant;
            n.children = list(this.children);
            return n;
        }

        public void __flatten()
        {
            if (this.type == NodeType.INCL_DISJUNCTION)
            {
                this.__flatten_binary_generic();
            }
            else
            {
                if (this.type == NodeType.EXCL_DISJUNCTION)
                {
                    this.__flatten_binary_generic();
                }
                else
                {
                    if (this.type == NodeType.CONJUNCTION)
                    {
                        this.__flatten_binary_generic();
                    }
                    else
                    {
                        if (this.type == NodeType.SUM)
                        {
                            this.__flatten_binary_generic();
                        }
                        else
                        {
                            if (this.type == NodeType.PRODUCT)
                            {
                                this.__flatten_product();
                            }
                        }
                    }
                }
            }
        }

        public void __flatten_binary_generic()
        {
            var changed = false;
            foreach (var i in range(this.children.Count() - 1, -(1), -(1)))
            {
                var child = this.children[i];
                if (child.type != this.type)
                {
                    continue;
                }
                this.children.RemoveAt(i);
                this.children.extend(child.children);
                changed = true;
            }
            if (changed)
            {
                this.__inspect_constants();
            }
        }

        public void __flatten_product()
        {
            var changed = false;
            var i = 0;
            while (i < this.children.Count())
            {
                var child = this.children[i];
                if (child.type != this.type)
                {
                    i += 1;
                    continue;
                }
                changed = true;
                this.children.RemoveAt(i);
                if (child.children[0].type == NodeType.CONSTANT)
                {
                    if ((i > 0 && this.children[0].type == NodeType.CONSTANT))
                    {
                        var prod = this.children[0].constant * child.children[0].constant;
                        this.children[0].__set_and_reduce_constant(prod);
                    }
                    else
                    {
                        this.children.Insert(0, child.children[0]);
                        i += 1;
                    }
                    child.children.RemoveAt(0);
                }
                this.children.Slice(i, i, null) = child.children;
                i += child.children.Count();
            }
            if (!(changed))
            {
                return;
            }
            if (this.children.Count() > 1)
            {
                var first = this.children[0];
                if (first.type != NodeType.CONSTANT)
                {
                    return;
                }
                if (!(first.__is_constant(1)))
                {
                    return;
                }
                this.children.RemoveAt(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
        }

        public void __check_duplicate_children()
        {
            if (this.type == NodeType.INCL_DISJUNCTION)
            {
                this.__remove_duplicate_children();
            }
            else
            {
                if (this.type == NodeType.EXCL_DISJUNCTION)
                {
                    this.__remove_pairs_of_children();
                }
                else
                {
                    if (this.type == NodeType.CONJUNCTION)
                    {
                        this.__remove_duplicate_children();
                    }
                    else
                    {
                        if (this.type == NodeType.SUM)
                        {
                            this.__merge_similar_nodes_sum();
                        }
                    }
                }
            }
        }

        public void __remove_duplicate_children()
        {
            var i = 0;
            while (i < this.children.Count())
            {
                foreach (var j in range(this.children.Count() - 1, i, -(1)))
                {
                    if (this.children[i].Equals(this.children[j]))
                    {
                        this.children.RemoveAt(j);
                    }
                }
                i += 1;
            }
        }

        public void __remove_pairs_of_children()
        {
            var i = 0;
            while (i < this.children.Count())
            {
                foreach (var j in range(this.children.Count() - 1, i, -(1)))
                {
                    if (this.children[i].Equals(this.children[j]))
                    {
                        this.children.RemoveAt(j);
                        this.children.RemoveAt(i);
                        i -= 1;
                        break;
                    }
                }
                i += 1;
            }
            if (this.children.Count() == 0)
            {
                this.children = new List<dynamic>() { this.__new_constant_node(0) };
            }
        }

        public void __merge_similar_nodes_sum()
        {
            Assert.True(this.type == NodeType.SUM);
            var i = 0;
            while (i < this.children.Count() - 1)
            {
                var j = i + 1;
                while (j < this.children.Count())
                {
                    if (this.__try_merge_sum_children(i, j))
                    {
                        this.children.RemoveAt(j);
                    }
                    else
                    {
                        j += 1;
                    }
                }
                if (this.children[i].__is_zero_product())
                {
                    this.children.RemoveAt(i);
                }
                else
                {
                    if (this.children[i].__has_factor_one())
                    {
                        this.children[i].children.pop(0);
                        if (this.children[i].children.Count() == 1)
                        {
                            this.children[i] = this.children[i].children[0];
                        }
                    }
                    i += 1;
                }
            }
            if (this.children.Count() > 1)
            {
                return;
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
                return;
            }
            this.type = NodeType.CONSTANT;
            this.children = new List<dynamic>() { };
            this.constant = 0;
        }

        public bool __is_zero_product()
        {
            return (this.type == NodeType.PRODUCT && this.children[0].__is_constant(0));
        }

        public bool __has_factor_one()
        {
            return (this.type == NodeType.PRODUCT && this.children[0].__is_constant(1));
        }

        public void __get_opt_const_factor()
        {
            if ((this.type == NodeType.PRODUCT && this.children[0].type == NodeType.CONSTANT))
            {
                return this.children[0].constant;
            }
            return null;
        }

        public bool __try_merge_sum_children(dynamic i, dynamic j)
        {
            var child1 = this.children[i];
            var const1 = child1.__get_opt_const_factor();
            var child2 = this.children[j];
            var const2 = child2.__get_opt_const_factor();
            if (!(child1.__equals_neglecting_constants(child2, const1 != null, const2 != null)))
            {
                return false;
            }
            if (const2 == null)
            {
                const2 = 1;
            }
            if (const1 == null)
            {
                if (child1.type == NodeType.PRODUCT)
                {
                    child1.children.Insert(0, this.__new_constant_node(1 + const2));
                }
                else
                {
                    var c = this.__new_constant_node(1 + const2);
                    this.children[i] = this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { c, child1 });
                }
            }
            else
            {
                child1.children[0].constant += const2;
                child1.children[0].__reduce_constant();
            }
            return true;
        }

        public dynamic __equals_neglecting_constants(dynamic other, dynamic hasConst, dynamic hasConstOther)
        {
            Assert.True((!(hasConst) || this.type == NodeType.PRODUCT));
            Assert.True((!(hasConstOther) || other.type == NodeType.PRODUCT));
            Assert.True((!(hasConst) || this.children[0].type == NodeType.CONSTANT));
            Assert.True((!(hasConstOther) || other.children[0].type == NodeType.CONSTANT));
            if (hasConst)
            {
                if (hasConstOther)
                {
                    return this.__equals_neglecting_constants_both_const(other);
                }
                return other.__equals_neglecting_constants_other_const(self);
            }
            if (hasConstOther)
            {
                return this.__equals_neglecting_constants_other_const(other);
            }
            return this.Equals(other);
        }

        public bool __equals_neglecting_constants_other_const(dynamic other)
        {
            Assert.True(other.type == NodeType.PRODUCT);
            Assert.True(other.children[0].type == NodeType.CONSTANT);
            Assert.True((this.type != NodeType.PRODUCT || this.children[0].type != NodeType.CONSTANT));
            if (other.children.Count() == 2)
            {
                return this.Equals(other.children[1]);
            }
            if (this.type != NodeType.PRODUCT)
            {
                return false;
            }
            if (this.children.Count() != other.children.Count() - 1)
            {
                return false;
            }
            var oIndices = list(range(1, other.children.Count()));
            foreach (var child in this.children)
            {
                var found = false;
                foreach (var i in oIndices)
                {
                    if (child.Equals(other.children[i]))
                    {
                        oIndices.remove(i);
                        found = true;
                    }
                }
                if (!(found))
                {
                    return false;
                }
            }
            return true;
        }

        public bool __equals_neglecting_constants_both_const(dynamic other)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            Assert.True(this.children[0].type == NodeType.CONSTANT);
            Assert.True(other.type == NodeType.PRODUCT);
            Assert.True(other.children[0].type == NodeType.CONSTANT);
            if (this.children.Count() != other.children.Count())
            {
                return false;
            }
            if (this.children.Count() == 2)
            {
                return this.children[1].Equals(other.children[1]);
            }
            var oIndices = list(range(1, other.children.Count()));
            foreach (var child in this.children.Slice(1, null, null))
            {
                var found = false;
                foreach (var i in oIndices)
                {
                    if (child.Equals(other.children[i]))
                    {
                        oIndices.remove(i);
                        found = true;
                    }
                }
                if (!(found))
                {
                    return false;
                }
            }
            return true;
        }

        public void __resolve_inverse_nodes()
        {
            if (((new List<dynamic>() { NodeType.INCL_DISJUNCTION, NodeType.EXCL_DISJUNCTION, NodeType.CONJUNCTION }).Contains(this.type)))
            {
                this.__resolve_inverse_nodes_bitwise();
            }
        }

        public void __resolve_inverse_nodes_bitwise()
        {
            var i = 0;
            while (true)
            {
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var neg1 = child1.type == NodeType.NEGATION;
                foreach (var j in range(i + 1, this.children.Count()))
                {
                    var child2 = this.children[j];
                    if (!(child1.__is_bitwise_inverse(child2)))
                    {
                        continue;
                    }
                    if ((this.type != NodeType.EXCL_DISJUNCTION || this.children.Count() == 2))
                    {
                        this.copy(this.__new_constant_node((this.type == NodeType.CONJUNCTION) ? 0 : -(1)));
                        return;
                    }
                    this.children.RemoveAt(j);
                    this.children.RemoveAt(i);
                    if (this.children[0].type == NodeType.CONSTANT)
                    {
                        this.children[0].__set_and_reduce_constant(-(1) ^ this.children[0].constant);
                        i -= 1;
                    }
                    else
                    {
                        this.children.Insert(0, this.__new_constant_node(-(1)));
                    }
                    if (this.children.Count() == 1)
                    {
                        this.copy(this.children[0]);
                        return;
                    }
                    break;
                }
                i += 1;
            }
        }

        public dynamic __is_bitwise_inverse(dynamic other)
        {
            if (this.type == NodeType.NEGATION)
            {
                if (other.type == NodeType.NEGATION)
                {
                    return this.children[0].__is_bitwise_inverse(other.children[0]);
                }
                return this.children[0].Equals(other);
            }
            if (other.type == NodeType.NEGATION)
            {
                return this.Equals(other.children[0]);
            }
            var node = this.get_copy();
            if ((node.type == NodeType.PRODUCT && node.children.Count() == 2 && node.children[0].type == NodeType.CONSTANT))
            {
                if (node.children[1].type == NodeType.SUM)
                {
                    foreach (var n in node.children[1].children)
                    {
                        n.__multiply(node.children[0].constant);
                    }
                    node.copy(node.children[1]);
                }
            }
            var onode = other.get_copy();
            if ((onode.type == NodeType.PRODUCT && onode.children.Count() == 2 && onode.children[0].type == NodeType.CONSTANT))
            {
                if (onode.children[1].type == NodeType.SUM)
                {
                    foreach (var n in onode.children[1].children)
                    {
                        n.__multiply(onode.children[0].constant);
                    }
                    onode.copy(onode.children[1]);
                }
            }
            if (node.type == NodeType.SUM)
            {
                if (onode.type != NodeType.SUM)
                {
                    if ((node.children.Count() > 2 || !(node.children[0].__is_constant(-(1)))))
                    {
                        return false;
                    }
                    node.children[1].__multiply_by_minus_one();
                    return node.children[1].Equals(onode);
                }
                foreach (var child in node.children)
                {
                    child.__multiply_by_minus_one();
                }
                if (node.children[0].type == NodeType.CONSTANT)
                {
                    node.children[0].constant -= 1;
                    node.children[0].__reduce_constant();
                    if (node.children[0].__is_constant(0))
                    {
                        node.children.RemoveAt(0);
                        Assert.True(node.children.Count() >= 1);
                        if (this.children.Count() == 1)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    node.children.Insert(0, this.__new_constant_node(-(1)));
                }
                return node.Equals(onode);
            }
            if (onode.type != NodeType.SUM)
            {
                return false;
            }
            if ((onode.children.Count() > 2 || !(onode.children[0].__is_constant(-(1)))))
            {
                return false;
            }
            onode.children[1].__multiply_by_minus_one();
            return onode.children[1].Equals(node);
        }

        public void __remove_trivial_nodes()
        {
            if (this.type < NodeType.PRODUCT)
            {
                return;
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
        }

        public dynamic __refine_step_2(void parent = null, bool restrictedScope = false)
        {
            var changed = false;
            if (!(restrictedScope))
            {
                foreach (var c in this.children)
                {
                    if (c.__refine_step_2(self))
                    {
                        changed = true;
                    }
                }
            }
            if (this.__eliminate_nested_negations_advanced())
            {
                changed = true;
            }
            if (this.__check_bitwise_negations(parent))
            {
                changed = true;
            }
            if (this.__check_bitwise_powers_of_two())
            {
                changed = true;
            }
            if (this.__check_beautify_constants_in_products())
            {
                changed = true;
            }
            if (this.__check_move_in_bitwise_negations())
            {
                changed = true;
            }
            if (this.__check_bitwise_negations_in_excl_disjunctions())
            {
                changed = true;
            }
            if (this.__check_rewrite_powers(parent))
            {
                changed = true;
            }
            if (this.__check_resolve_product_of_powers())
            {
                changed = true;
            }
            if (this.__check_resolve_product_of_constant_and_sum())
            {
                changed = true;
            }
            if (this.__check_factor_out_of_sum())
            {
                changed = true;
            }
            if (this.__check_resolve_inverse_negations_in_sum())
            {
                changed = true;
            }
            if (this.__insert_fixed_in_conj())
            {
                changed = true;
            }
            if (this.__insert_fixed_in_disj())
            {
                changed = true;
            }
            if (this.__check_trivial_xor())
            {
                changed = true;
            }
            if (this.__check_xor_same_mult_by_minus_one())
            {
                changed = true;
            }
            if (this.__check_conj_zero_rule())
            {
                changed = true;
            }
            if (this.__check_conj_neg_xor_zero_rule())
            {
                changed = true;
            }
            if (this.__check_conj_neg_xor_minus_one_rule())
            {
                changed = true;
            }
            if (this.__check_conj_negated_xor_zero_rule())
            {
                changed = true;
            }
            if (this.__check_conj_xor_identity_rule())
            {
                changed = true;
            }
            if (this.__check_disj_xor_identity_rule())
            {
                changed = true;
            }
            if (this.__check_conj_neg_conj_identity_rule())
            {
                changed = true;
            }
            if (this.__check_disj_disj_identity_rule())
            {
                changed = true;
            }
            if (this.__check_conj_conj_identity_rule())
            {
                changed = true;
            }
            if (this.__check_disj_conj_identity_rule())
            {
                changed = true;
            }
            if (this.__check_disj_conj_identity_rule_2())
            {
                changed = true;
            }
            if (this.__check_conj_disj_identity_rule())
            {
                changed = true;
            }
            if (this.__check_disj_neg_disj_identity_rule())
            {
                changed = true;
            }
            if (this.__check_disj_sub_disj_identity_rule())
            {
                changed = true;
            }
            if (this.__check_disj_sub_conj_identity_rule())
            {
                changed = true;
            }
            if (this.__check_conj_add_conj_identity_rule())
            {
                changed = true;
            }
            if (this.__check_disj_disj_conj_rule())
            {
                changed = true;
            }
            if (this.__check_conj_conj_disj_rule())
            {
                changed = true;
            }
            if (this.__check_disj_disj_conj_rule_2())
            {
                changed = true;
            }
            return changed;
        }

        public bool __eliminate_nested_negations_advanced()
        {
            if (this.type == NodeType.NEGATION)
            {
                var child = this.children[0];
                if (child.type == NodeType.NEGATION)
                {
                    this.copy(child.children[0]);
                    return true;
                }
                var node = child.__get_opt_transformed_negated();
                if (node != null)
                {
                    this.copy(node);
                    return true;
                }
                if ((child.type == NodeType.SUM && child.children[0].__is_constant(-(1))))
                {
                    this.type = NodeType.SUM;
                    this.children = child.children.Slice(1, null, null);
                    foreach (var child in this.children)
                    {
                        child.__multiply_by_minus_one();
                    }
                    Assert.True(this.children.Count() >= 1);
                    if (this.children.Count() == 1)
                    {
                        this.copy(this.children[0]);
                    }
                    return true;
                }
                return false;
            }
            child = this.__get_opt_transformed_negated();
            if (child == null)
            {
                return false;
            }
            if (child.type == NodeType.NEGATION)
            {
                this.copy(child.children[0]);
                return true;
            }
            node = child.__get_opt_transformed_negated();
            if (node != null)
            {
                this.copy(node);
                return true;
            }
            return false;
        }

        public void __multiply(dynamic factor)
        {
            if (factor - 1 % this.__modulus == 0)
            {
                return;
            }
            if (this.type == NodeType.CONSTANT)
            {
                this.__set_and_reduce_constant(this.constant * factor);
                return;
            }
            if (this.type == NodeType.SUM)
            {
                foreach (var child in this.children)
                {
                    child.__multiply(factor);
                }
                return;
            }
            if (this.type == NodeType.PRODUCT)
            {
                if (this.children[0].type == NodeType.PRODUCT)
                {
                    this.children.Slice(1, 1, null) = this.children[0].children.Slice(1, null, null);
                    this.children[0] = this.children[0].children[0];
                }
                if (this.children[0].type == NodeType.CONSTANT)
                {
                    this.children[0].__multiply(factor);
                    if (this.children[0].__is_constant(1))
                    {
                        this.children.RemoveAt(0);
                        if (this.children.Count() == 1)
                        {
                            this.copy(this.children[0]);
                        }
                    }
                    else
                    {
                        if (this.children[0].__is_constant(0))
                        {
                            this.copy(this.children[0]);
                        }
                    }
                    return;
                }
                this.children.Insert(0, this.__new_constant_node(factor));
                return;
            }
            var fac = this.__new_constant_node(factor);
            var node = this.__new_node(NodeType.CONSTANT);
            node.copy(self);
            var prod = this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { fac, node });
            this.copy(prod);
        }

        public void __multiply_by_minus_one()
        {
            this.__multiply(-(1));
        }

        public void __get_opt_transformed_negated()
        {
            if (this.type == NodeType.SUM)
            {
                return this.__get_opt_transformed_negated_sum();
            }
            if (this.type == NodeType.PRODUCT)
            {
                return this.__get_opt_transformed_negated_product();
            }
            return null;
        }

        public dynamic __get_opt_transformed_negated_sum()
        {
            Assert.True(this.type == NodeType.SUM);
            if (this.children.Count() < 2)
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var res = this.__new_node(NodeType.SUM);
            foreach (var i in range(1, this.children.Count()))
            {
                var child = this.children[i];
                var hasMinusOne = (child.type == NodeType.PRODUCT && child.children[0].__is_constant(-(1)));
                if (hasMinusOne)
                {
                    Assert.True(child.children.Count() > 1);
                    if (child.children.Count() == 2)
                    {
                        res.children.Add(child.children[1]);
                        continue;
                    }
                    res.children.Add(this.__new_node_with_children(NodeType.PRODUCT, child.children.Slice(1, null, null)));
                }
                else
                {
                    var node = child.get_copy();
                    node.__multiply_by_minus_one();
                    res.children.Add(node);
                }
            }
            Assert.True(res.children.Count() > 0);
            if (res.children.Count() == 1)
            {
                return res.children[0];
            }
            return res;
        }

        public dynamic __get_opt_transformed_negated_product()
        {
            Assert.True(this.type == NodeType.PRODUCT);
            if (this.children.Count() != 2)
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var child1 = this.children[1];
            if (child1.type != NodeType.SUM)
            {
                return null;
            }
            if (!(child1.children[0].__is_constant(1)))
            {
                return null;
            }
            if (child1.children.Count() < 2)
            {
                return null;
            }
            if (child1.children.Count() == 2)
            {
                return child1.children[1];
            }
            return this.__new_node_with_children(NodeType.SUM, child1.children.Slice(1, null, null));
        }

        public bool __check_bitwise_negations(dynamic parent)
        {
            if (this.type == NodeType.NEGATION)
            {
                if ((parent != null && parent.__is_bitwise_op()))
                {
                    return false;
                }
                if (this.children[0].type == NodeType.PRODUCT)
                {
                    this.__substitute_bitwise_negation_product();
                    return true;
                }
                if (this.children[0].type == NodeType.SUM)
                {
                    this.__substitute_bitwise_negation_sum();
                    return true;
                }
                return this.__substitute_bitwise_negation_generic(parent);
            }
            if ((parent == null || !(parent.__is_bitwise_op())))
            {
                return false;
            }
            var child = this.__get_opt_transformed_negated();
            if (child == null)
            {
                return false;
            }
            this.type = NodeType.NEGATION;
            this.children = new List<dynamic>() { child };
            return true;
        }

        public bool __is_bitwise_op()
        {
            return (this.type == NodeType.NEGATION || this.__is_bitwise_binop());
        }

        public bool __is_bitwise_binop()
        {
            return ((new List<dynamic>() { NodeType.CONJUNCTION, NodeType.EXCL_DISJUNCTION, NodeType.INCL_DISJUNCTION }).Contains(this.type));
        }

        public bool __is_arithm_op()
        {
            return ((new List<dynamic>() { NodeType.SUM, NodeType.PRODUCT, NodeType.POWER }).Contains(this.type));
        }

        public void __substitute_bitwise_negation_product()
        {
            this.type = NodeType.SUM;
            this.children.Insert(0, this.__new_constant_node(-(1)));
            var child = this.children[1];
            if (child.children[0].type == NodeType.CONSTANT)
            {
                child.children[0].__set_and_reduce_constant(-(child.children[0].constant));
            }
            else
            {
                child.children.Insert(0, this.__new_constant_node(-(1)));
            }
        }

        public void __substitute_bitwise_negation_sum()
        {
            this.type = NodeType.PRODUCT;
            this.children.Insert(0, this.__new_constant_node(-(1)));
            var child = this.children[1];
            if (child.children[0].type == NodeType.CONSTANT)
            {
                child.children[0].__set_and_reduce_constant(child.children[0].constant + 1);
            }
            else
            {
                child.children.Insert(0, this.__new_constant_node(1));
            }
        }

        public bool __substitute_bitwise_negation_generic(dynamic parent)
        {
            Assert.True(this.type == NodeType.NEGATION);
            if (parent == null)
            {
                return false;
            }
            if ((parent.type != NodeType.SUM && parent.type != NodeType.PRODUCT))
            {
                return false;
            }
            if (parent.type == NodeType.PRODUCT)
            {
                if ((parent.children.Count() > 2 || parent.children[0].type != NodeType.CONSTANT))
                {
                    return false;
                }
            }
            var prod = this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { this.__new_constant_node(-(1)), this.children[0] });
            this.type = NodeType.SUM;
            this.children = new List<dynamic>() { this.__new_constant_node(-(1)), prod };
            return true;
        }

        public bool __check_bitwise_powers_of_two()
        {
            if (!(this.__is_bitwise_binop()))
            {
                return false;
            }
            var e = this.__get_max_factor_power_of_two_in_children();
            if (e <= 0)
            {
                return false;
            }
            var c = null;
            if (this.children[0].type == NodeType.CONSTANT)
            {
                c = this.children[0].constant;
            }
            var add = null;
            foreach (var child in this.children)
            {
                var rem = child.__divide_by_power_of_two(e);
                if (add == null)
                {
                    add = rem;
                }
                else
                {
                    if (this.type == NodeType.CONJUNCTION)
                    {
                        add &= rem;
                    }
                    else
                    {
                        if (this.type == NodeType.INCL_DISJUNCTION)
                        {
                            add |= rem;
                        }
                        else
                        {
                            add ^= rem;
                        }
                    }
                }
            }
            var prod = this.__new_node(NodeType.PRODUCT);
            prod.children = new List<dynamic>() { this.__new_constant_node(2 * *e), this.__get_shallow_copy() };
            this.copy(prod);
            if (add % this.__modulus != 0)
            {
                var constNode = this.__new_constant_node(add);
                var sumNode = this.__new_node_with_children(NodeType.SUM, new List<dynamic>() { constNode, this.__get_shallow_copy() });
                this.copy(sumNode);
            }
            return true;
        }

        public dynamic __get_max_factor_power_of_two_in_children(bool allowRem = true)
        {
            Assert.True(this.children.Count() > 1);
            Assert.True((this.__is_bitwise_binop() || this.type == NodeType.SUM));
            var withNeg = (allowRem && this.__is_bitwise_binop());
            var maxe = this.children[0].__get_max_factor_power_of_two(withNeg);
            if ((allowRem && this.children[0].type == NodeType.CONSTANT))
            {
                maxe = -(1);
            }
            if (maxe == 0)
            {
                return 0;
            }
            foreach (var child in this.children.Slice(1, null, null))
            {
                var e = child.__get_max_factor_power_of_two(withNeg);
                if (e == 0)
                {
                    return 0;
                }
                if (e == -(1))
                {
                    continue;
                }
                maxe = (maxe == -(1)) ? e : min(maxe, e);
            }
            return maxe;
        }

        public ulong __get_max_factor_power_of_two(dynamic allowRem)
        {
            if (this.type == NodeType.CONSTANT)
            {
                return trailing_zeros(this.constant);
            }
            if (this.type == NodeType.PRODUCT)
            {
                return this.children[0].__get_max_factor_power_of_two(false);
            }
            if (this.type == NodeType.SUM)
            {
                return this.__get_max_factor_power_of_two_in_children(allowRem);
            }
            if ((allowRem && this.type == NodeType.NEGATION))
            {
                return this.children[0].__get_max_factor_power_of_two(false);
            }
            return 0;
        }

        public dynamic __divide_by_power_of_two(dynamic e)
        {
            if (this.type == NodeType.CONSTANT)
            {
                var orig = this.constant;
                this.constant >>= e;
                return orig - this.constant << e;
            }
            if (this.type == NodeType.PRODUCT)
            {
                var rem = this.children[0].__divide_by_power_of_two(e);
                Assert.True(rem == 0);
                if (this.children[0].__is_constant(1))
                {
                    this.children.RemoveAt(0);
                    if (this.children.Count() == 1)
                    {
                        this.copy(this.children[0]);
                    }
                }
                return 0;
            }
            if (this.type == NodeType.SUM)
            {
                var add = 0;
                foreach (var child in this.children)
                {
                    rem = child.__divide_by_power_of_two(e);
                    if (rem != 0)
                    {
                        Assert.True(add == 0);
                        add = rem;
                    }
                }
                if (this.children[0].__is_constant(0))
                {
                    this.children.RemoveAt(0);
                    if (this.children.Count() == 1)
                    {
                        this.copy(this.children[0]);
                    }
                }
                return add;
            }
            Assert.True(this.type == NodeType.NEGATION);
            rem = this.children[0].__divide_by_power_of_two(e);
            Assert.True(rem == 0);
            return 1 << e - 1;
        }

        public dynamic __check_beautify_constants_in_products()
        {
            if (this.type != NodeType.PRODUCT)
            {
                return false;
            }
            if (this.children[0].type != NodeType.CONSTANT)
            {
                return false;
            }
            var e = trailing_zeros(this.children[0].constant);
            if (e <= 0)
            {
                return false;
            }
            var changed = false;
            foreach (var child in this.children.Slice(1, null, null))
            {
                var ch = child.__check_beautify_constants(e);
                if (ch)
                {
                    changed = true;
                }
            }
            return changed;
        }

        public bool __check_beautify_constants(dynamic e)
        {
            if ((this.__is_bitwise_op() || ((new List<dynamic>() { NodeType.SUM, NodeType.PRODUCT }).Contains(this.type))))
            {
                var changed = false;
                foreach (var child in this.children)
                {
                    var ch = child.__check_beautify_constants(e);
                    if (ch)
                    {
                        changed = true;
                    }
                }
                return changed;
            }
            if (this.type != NodeType.CONSTANT)
            {
                return false;
            }
            var orig = this.constant;
            var mask = -(1) % this.__modulus >> e;
            var b = this.constant & this.__modulus >> e + 1;
            this.constant &= mask;
            if (b > 0)
            {
                if ((popcount(this.constant) > 1 || b == 1))
                {
                    this.constant |= ~(mask);
                }
            }
            this.__reduce_constant();
            return this.constant != orig;
        }

        public bool __check_move_in_bitwise_negations()
        {
            if (this.type != NodeType.NEGATION)
            {
                return false;
            }
            var childType = this.children[0].type;
            if (childType == NodeType.EXCL_DISJUNCTION)
            {
                return this.__check_move_in_bitwise_negation_excl_disj();
            }
            if (((new List<dynamic>() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION }).Contains(childType)))
            {
                return this.__check_move_in_bitwise_negation_conj_or_incl_disj();
            }
            return false;
        }

        public bool __check_move_in_bitwise_negation_conj_or_incl_disj()
        {
            Assert.True(this.type == NodeType.NEGATION);
            var child = this.children[0];
            if (!(child.__is_any_child_negated()))
            {
                return false;
            }
            child.__negate_all_children();
            child.type = (child.type == NodeType.CONJUNCTION) ? NodeType.INCL_DISJUNCTION : NodeType.CONJUNCTION;
            this.copy(child);
            return true;
        }

        public bool __is_any_child_negated()
        {
            foreach (var child in this.children)
            {
                if (child.type == NodeType.NEGATION)
                {
                    return true;
                }
                var node = child.__get_opt_transformed_negated();
                if (node != null)
                {
                    return true;
                }
            }
            return false;
        }

        public void __negate_all_children()
        {
            foreach (var child in this.children)
            {
                child.__negate();
            }
        }

        public void __negate()
        {
            if (this.type == NodeType.NEGATION)
            {
                this.copy(this.children[0]);
                return;
            }
            var node = this.__get_opt_transformed_negated();
            if (node != null)
            {
                this.copy(node);
                return;
            }
            node = this.__new_node_with_children(NodeType.NEGATION, new List<dynamic>() { this.get_copy() });
            this.copy(node);
        }

        public bool __check_move_in_bitwise_negation_excl_disj()
        {
            Assert.True(this.type == NodeType.NEGATION);
            var child = this.children[0];
            var (n, _) = child.__get_recursively_negated_child();
            if (n == null)
            {
                return false;
            }
            n.__negate();
            this.copy(child);
            return true;
        }

        public dynamic __get_recursively_negated_child(void maxDepth = null)
        {
            if (this.type == NodeType.NEGATION)
            {
                return (self, 0);
            }
            var node = this.__get_opt_transformed_negated();
            if (node != null)
            {
                return (self, 0);
            }
            if ((maxDepth != null && maxDepth == 0))
            {
                return null;
            }
            if (!(this.__is_bitwise_binop()))
            {
                return (null, null);
            }
            var opt = null;
            var candidate = null;
            var nextMax = (maxDepth == null) ? null : maxDepth - 1;
            foreach (var child in this.children)
            {
                var (_, d) = child.__get_recursively_negated_child(nextMax);
                if (d == null)
                {
                    continue;
                }
                if (maxDepth == null)
                {
                    return (child, d + 1);
                }
                Assert.True((opt == null || d < opt));
                opt = d;
                candidate = child;
                nextMax = opt - 1;
            }
            return (candidate, opt);
        }

        public dynamic __check_bitwise_negations_in_excl_disjunctions()
        {
            if (this.type != NodeType.EXCL_DISJUNCTION)
            {
                return false;
            }
            var neg = null;
            var changed = false;
            foreach (var child in this.children)
            {
                if (!(child.__is_negated()))
                {
                    continue;
                }
                if (neg == null)
                {
                    neg = child;
                    continue;
                }
                neg.__negate();
                child.__negate();
                neg = null;
                changed = true;
            }
            return changed;
        }

        public bool __is_negated()
        {
            if (this.type == NodeType.NEGATION)
            {
                return true;
            }
            var node = this.__get_opt_transformed_negated();
            return node != null;
        }

        public bool __check_rewrite_powers(dynamic parent)
        {
            if (this.type != NodeType.POWER)
            {
                return false;
            }
            var exp = this.children[1];
            if (exp.type != NodeType.CONSTANT)
            {
                return false;
            }
            var base = this.children[0];
            if (base.type != NodeType.PRODUCT)
            {
                return false;
            }
            if (base.children[0].type != NodeType.CONSTANT)
            {
                return false;
            }
            var const = this.__power(base.children[0].constant, exp.constant);
            base.children.RemoveAt(0);
            if (base.children.Count() == 1)
            {
                base.copy(base.children[0]);
            }
            if (const == 1) {
                return true;
            }
            if ((parent != null && parent.type == NodeType.PRODUCT))
            {
                if (parent.children[0].type == NodeType.PRODUCT)
                {
                    parent.children[0].__set_and_reduce_constant(parent.children[0].constant * const);
                }
                else
                {
                    parent.children.Insert(0, this.__new_constant_node(const));
                }
            }
            else
            {
                var prod = this.__new_node(NodeType.PRODUCT);
                prod.children.Add(this.__new_constant_node(const));
                prod.children.Add(this.__get_shallow_copy());
                this.copy(prod);
            }
            return true;
        }

        public dynamic __check_resolve_product_of_powers()
        {
            if (this.type != NodeType.PRODUCT)
            {
                return false;
            }
            var changed = false;
            var start = int(this.children[0].type == NodeType.CONSTANT);
            foreach (var i in range(this.children.Count() - 1, start, -(1)))
            {
                var child = this.children[i];
                var merged = false;
                foreach (var j in range(start, i))
                {
                    var child2 = this.children[j];
                    if (child2.type == NodeType.POWER)
                    {
                        var base2 = child2.children[0];
                        var exp2 = child2.children[1];
                        if (base2.Equals(child))
                        {
                            exp2.__add_constant(1);
                            this.children.RemoveAt(i);
                            changed = true;
                            break;
                        }
                        if ((child.type == NodeType.POWER && base2.Equals(child.children[0])))
                        {
                            exp2.__add(child.children[1]);
                            this.children.RemoveAt(i);
                            changed = true;
                            break;
                        }
                    }
                    if (child.type == NodeType.POWER)
                    {
                        var base = child.children[0];
                        var exp = child.children[1];
                        if (base.Equals(child2))
                        {
                            exp.__add_constant(1);
                            this.children[j] = this.children[i];
                            this.children.RemoveAt(i);
                            changed = true;
                        }
                        break;
                    }
                    if (child.Equals(child2))
                    {
                        this.children[j] = this.__new_node_with_children(NodeType.POWER, new List<dynamic>() { child, this.__new_constant_node(2) });
                        this.children.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public void __add(dynamic other)
        {
            if (this.type == NodeType.CONSTANT)
            {
                var constant = this.constant;
                this.copy(other.get_copy());
                this.__add_constant(constant);
                return;
            }
            if (other.type == NodeType.CONSTANT)
            {
                this.__add_constant(other.constant);
                return;
            }
            if (this.type == NodeType.SUM)
            {
                this.__add_to_sum(other);
                return;
            }
            if (other.type == NodeType.SUM)
            {
                var node = other.get_copy();
                node.__add_to_sum(self);
                this.copy(node);
                return;
            }
            node = this.__new_node_with_children(NodeType.SUM, new List<dynamic>() { this.get_copy(), other.get_copy() });
            this.copy(node);
            this.__merge_similar_nodes_sum();
        }

        public void __add_constant(dynamic constant)
        {
            if (this.type == NodeType.CONSTANT)
            {
                this.__set_and_reduce_constant(this.constant + constant);
                return;
            }
            if (this.type == NodeType.SUM)
            {
                if ((this.children.Count() > 0 && this.children[0].type == NodeType.CONSTANT))
                {
                    this.children[0].__add_constant(constant);
                    return;
                }
                this.children.Insert(0, this.__new_constant_node(constant));
                return;
            }
            var node = this.__new_node_with_children(NodeType.SUM, new List<dynamic>() { this.__new_constant_node(constant), this.get_copy() });
            this.copy(node);
        }

        public void __add_to_sum(dynamic other)
        {
            Assert.True(this.type == NodeType.SUM);
            Assert.True(other.type != NodeType.CONSTANT);
            if (other.type == NodeType.SUM)
            {
                foreach (var ochild in other.children)
                {
                    if (ochild.type == NodeType.CONSTANT)
                    {
                        this.__add_constant(ochild.constant);
                    }
                    else
                    {
                        this.children.Add(ochild.get_copy());
                    }
                }
            }
            else
            {
                this.children.Add(other.get_copy());
            }
            this.__merge_similar_nodes_sum();
        }

        public bool __check_resolve_product_of_constant_and_sum()
        {
            if (this.type != NodeType.PRODUCT)
            {
                return false;
            }
            if (this.children.Count() < 2)
            {
                return false;
            }
            var child0 = this.children[0];
            if (child0.type != NodeType.CONSTANT)
            {
                return false;
            }
            var child1 = this.children[1];
            if (child1.type != NodeType.SUM)
            {
                return false;
            }
            var constant = child0.constant;
            var sumNode = self;
            if (this.children.Count() == 2)
            {
                this.copy(child1);
            }
            else
            {
                this.children.RemoveAt(0);
                sumNode = this.children[0];
            }
            foreach (var i in range(sumNode.children.Count()))
            {
                if (sumNode.children[i].type == NodeType.CONSTANT)
                {
                    sumNode.children[i].__set_and_reduce_constant(sumNode.children[i].constant * constant);
                }
                else
                {
                    if (sumNode.children[i].type == NodeType.PRODUCT)
                    {
                        var first = sumNode.children[i].children[0];
                        if (first.type == NodeType.CONSTANT)
                        {
                            first.__set_and_reduce_constant(first.constant * constant);
                        }
                        else
                        {
                            sumNode.children[i].children.Insert(0, sumNode.__new_constant_node(constant));
                        }
                    }
                    else
                    {
                        var factors = new List<dynamic>() { sumNode.__new_constant_node(constant), sumNode.children[i] };
                        sumNode.children[i] = sumNode.__new_node_with_children(NodeType.PRODUCT, factors);
                    }
                }
            }
            return true;
        }

        public bool __check_factor_out_of_sum()
        {
            if ((this.type != NodeType.SUM || this.children.Count() <= 1))
            {
                return false;
            }
            var factors = new List<dynamic>() { };
            while (true)
            {
                var factor = this.__try_factor_out_of_sum();
                if (factor == null)
                {
                    break;
                }
                factors.Add(factor);
            }
            if (factors.Count() == 0)
            {
                return false;
            }
            var prod = this.__new_node_with_children(NodeType.PRODUCT, factors + new List<dynamic>() { this.get_copy() });
            this.copy(prod);
            return true;
        }

        public dynamic __try_factor_out_of_sum()
        {
            Assert.True(this.type == NodeType.SUM);
            Assert.True(this.children.Count() > 1);
            var factor = this.__get_common_factor_in_sum();
            if (factor == null)
            {
                return null;
            }
            foreach (var child in this.children)
            {
                child.__eliminate_factor(factor);
            }
            return factor;
        }

        public void __get_common_factor_in_sum()
        {
            Assert.True(this.type == NodeType.SUM);
            var first = this.children[0];
            if (first.type == NodeType.PRODUCT)
            {
                foreach (var child in first.children)
                {
                    if (child.type == NodeType.CONSTANT)
                    {
                        continue;
                    }
                    if (this.__has_factor_in_remaining_children(child))
                    {
                        return child.get_copy();
                    }
                    if (child.type == NodeType.POWER)
                    {
                        var exp = child.children[1];
                        if ((exp.type == NodeType.CONSTANT && !(exp.__is_constant(0))))
                        {
                            var base = child.children[0];
                            if (this.__has_factor_in_remaining_children(base))
                            {
                                return base.get_copy();
                            }
                        }
                    }
                }
                return null;
            }
            if (first.type == NodeType.POWER)
            {
                exp = first.children[1];
                if ((exp.type == NodeType.CONSTANT && !(exp.__is_constant(0))))
                {
                    base = first.children[0];
                    if ((base.type != NodeType.CONSTANT && this.__has_factor_in_remaining_children(base)))
                    {
                        return base.get_copy();
                    }
                }
                return null;
            }
            if ((first.type != NodeType.CONSTANT && this.__has_factor_in_remaining_children(first)))
            {
                return first.get_copy();
            }
            return null;
        }

        public bool __has_factor_in_remaining_children(dynamic factor)
        {
            Assert.True(this.type == NodeType.SUM);
            foreach (var child in this.children.Slice(1, null, null))
            {
                if (!(child.__has_factor(factor)))
                {
                    return false;
                }
            }
            return true;
        }

        public dynamic __has_factor(dynamic factor)
        {
            if (this.type == NodeType.PRODUCT)
            {
                return this.__has_factor_product(factor);
            }
            if (this.type == NodeType.POWER)
            {
                var exp = this.children[1];
                if ((exp.type == NodeType.CONSTANT && !(exp.__is_constant(0))))
                {
                    return this.children[0].Equals(factor);
                }
            }
            return this.Equals(factor);
        }

        public bool __has_factor_product(dynamic factor)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            foreach (var child in this.children)
            {
                if (child.Equals(factor))
                {
                    return true;
                }
                if (child.type == NodeType.POWER)
                {
                    var exp = child.children[1];
                    if ((exp.type == NodeType.CONSTANT && !(exp.__is_constant(0))))
                    {
                        if (child.children[0].Equals(factor))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool __has_child(dynamic node)
        {
            return this.__get_index_of_child(node) != null;
        }

        public void __get_index_of_child(dynamic node)
        {
            foreach (var i in range(this.children.Count()))
            {
                if (this.children[i].Equals(node))
                {
                    return i;
                }
            }
            return null;
        }

        public void __get_index_of_child_negated(dynamic node)
        {
            foreach (var i in range(this.children.Count()))
            {
                if (this.children[i].equals_negated(node))
                {
                    return i;
                }
            }
            return null;
        }

        public void __eliminate_factor(dynamic factor)
        {
            if (this.type == NodeType.PRODUCT)
            {
                this.__eliminate_factor_product(factor);
                return;
            }
            if (this.type == NodeType.POWER)
            {
                this.__eliminate_factor_power(factor);
                return;
            }
            Assert.True(this.Equals(factor));
            var c = this.__new_constant_node(1);
            this.copy(c);
        }

        public void __eliminate_factor_product(dynamic factor)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            foreach (var i in range(this.children.Count()))
            {
                var child = this.children[i];
                if (child.Equals(factor))
                {
                    this.children.RemoveAt(i);
                    if (this.children.Count() == 1)
                    {
                        this.copy(this.children[0]);
                    }
                    return;
                }
                if ((child.type == NodeType.POWER && child.children[0].Equals(factor)))
                {
                    child.__decrement_exponent();
                    return;
                }
            }
            Assert.True(false);
        }

        public void __eliminate_factor_power(dynamic factor)
        {
            Assert.True(this.type == NodeType.POWER);
            if (this.Equals(factor))
            {
                this.copy(this.__new_constant_node(1));
                return;
            }
            Assert.True(this.children[0].Equals(factor));
            this.__decrement_exponent();
        }

        public void __decrement_exponent()
        {
            Assert.True(this.type == NodeType.POWER);
            Assert.True(this.children.Count() == 2);
            this.children[1].__decrement();
            if (this.children[1].__is_constant(1))
            {
                this.copy(this.children[0]);
            }
            else
            {
                if (this.children[1].__is_constant(0))
                {
                    this.copy(this.__new_constant_node(1));
                }
            }
        }

        public void __decrement()
        {
            this.__add_constant(-(1));
        }

        public dynamic __check_resolve_inverse_negations_in_sum()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            var changed = false;
            var const = 0;
            var i = this.children.Count();
            while (i > 1)
            {
                i -= 1;
                var first = this.children[i];
                foreach (var j in range(i))
                {
                    var second = this.children[j];
                    if (first.equals_negated(second))
                    {
                        this.children.RemoveAt(i);
                        this.children.RemoveAt(j);
                        i -= 1;
                        const -= 1;
                        changed = true;
                        break;
                    }
                    if (first.type != NodeType.PRODUCT)
                    {
                        continue;
                    }
                    if (second.type != NodeType.PRODUCT)
                    {
                        continue;
                    }
                    var indices = first.__get_only_differing_child_indices(second);
                    if (indices == null)
                    {
                        continue;
                    }
                    var (firstIdx, secIdx) = indices;
                    if (first.children[firstIdx].equals_negated(second.children[secIdx]))
                    {
                        this.children.RemoveAt(i);
                        second.children.RemoveAt(secIdx);
                        if (second.children.Count() == 1)
                        {
                            second.copy(second.children[0]);
                        }
                        second.__multiply_by_minus_one();
                        changed = true;
                        break;
                    }
                }
            }
            if ((this.children.Count() > 0 && this.children[0].type == NodeType.CONSTANT))
            {
                this.children[0].__set_and_reduce_constant(this.children[0].constant + const);
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(const));
            }
            if ((this.children.Count() > 1 && this.children[0].__is_constant(0)))
            {
                this.children.RemoveAt(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            else
            {
                if (this.children.Count() == 0)
                {
                    this.copy(this.__new_constant_node(0));
                }
            }
            return changed;
        }

        public dynamic expand(bool restrictedScope = false)
        {
            if ((restrictedScope && !((new List<dynamic>() { NodeType.SUM, NodeType.PRODUCT, NodeType.POWER }).Contains(this.type))))
            {
                return false;
            }
            var changed = false;
            if ((restrictedScope && this.type == NodeType.POWER))
            {
                changed = this.children[0].expand(restrictedScope);
            }
            else
            {
                foreach (var c in this.children)
                {
                    if (c.expand(restrictedScope))
                    {
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                this.__inspect_constants();
                if (this.type == NodeType.SUM)
                {
                    this.__flatten_binary_generic();
                }
            }
            if (this.__check_expand())
            {
                changed = true;
            }
            if ((changed && this.type == NodeType.SUM))
            {
                this.__merge_similar_nodes_sum();
            }
            return changed;
        }

        public bool __check_expand()
        {
            if (this.type == NodeType.PRODUCT)
            {
                return this.__check_expand_product();
            }
            if (this.type == NodeType.POWER)
            {
                return this.__check_expand_power();
            }
            return false;
        }

        public bool __check_expand_product()
        {
            Assert.True(this.type == NodeType.PRODUCT);
            Assert.True(this.children.Count() > 0);
            if (this.children.Count() == 1)
            {
                return false;
            }
            if (!(this.__has_sum_child()))
            {
                return false;
            }
            this.__expand_product();
            return true;
        }

        public bool __has_sum_child()
        {
            return this.__get_first_sum_index() != null;
        }

        public void __get_first_sum_index()
        {
            foreach (var i in range(this.children.Count()))
            {
                if (this.children[i].type == NodeType.SUM)
                {
                    return i;
                }
            }
            return null;
        }

        public void __expand_product()
        {
            while (true)
            {
                var sumIdx = this.__get_first_sum_index();
                if (sumIdx == null)
                {
                    break;
                }
                var node = this.children[sumIdx].get_copy();
                Assert.True(node.type == NodeType.SUM);
                var repeat = false;
                foreach (var i in range(this.children.Count()))
                {
                    if (i == sumIdx)
                    {
                        continue;
                    }
                    node.__multiply_sum(this.children[i]);
                    if (node.__is_constant(0))
                    {
                        break;
                    }
                    if (node.type != NodeType.SUM)
                    {
                        this.children[sumIdx] = node;
                        foreach (var j in range(i, -(1), -(1)))
                        {
                            if (j == sumIdx)
                            {
                                continue;
                            }
                            this.children.RemoveAt(j);
                        }
                        repeat = true;
                        break;
                    }
                }
                if (!(repeat))
                {
                    break;
                }
            }
            if (node.children.Count() == 1)
            {
                this.copy(node.children[0]);
            }
            else
            {
                this.copy(node);
            }
        }

        public void __multiply_sum(dynamic other)
        {
            Assert.True(this.type == NodeType.SUM);
            if (other.type == NodeType.SUM)
            {
                this.__multiply_sum_with_sum(other, true);
                return;
            }
            var constant = 0;
            foreach (var i in range(this.children.Count() - 1, -(1), -(1)))
            {
                var child = this.children[i];
                child.__multiply_with_node_no_sum(other);
                if (child.type == NodeType.CONSTANT)
                {
                    constant = this.__get_reduced_constant(constant + child.constant);
                    if (i > 0)
                    {
                        this.children.RemoveAt(i);
                    }
                    continue;
                }
            }
            if (this.children[0].type == NodeType.CONSTANT)
            {
                if (constant == 0)
                {
                    this.children.RemoveAt(0);
                }
                else
                {
                    this.children[0].constant = constant;
                }
            }
            else
            {
                if (constant != 0)
                {
                    this.children.Insert(0, this.__new_constant_node(constant));
                }
            }
            this.__merge_similar_nodes_sum();
            if ((this.type == NodeType.SUM && this.children.Count() == 0))
            {
                this.copy(this.__new_constant_node(0));
            }
        }

        public void __multiply_sum_with_sum(dynamic other, bool keepSum = false)
        {
            Assert.True(this.type == NodeType.SUM);
            Assert.True(other.type == NodeType.SUM);
            var children = list(this.children);
            this.children = new List<dynamic>() { };
            foreach (var child in children)
            {
                foreach (var ochild in other.children)
                {
                    var prod = child.__get_product_with_node(ochild);
                    if (prod.type == NodeType.CONSTANT)
                    {
                        if (prod.__is_constant(0))
                        {
                            continue;
                        }
                        if ((this.children.Count() > 0 && this.children[0].type == NodeType.CONSTANT))
                        {
                            this.children[0].__set_and_reduce_constant(this.children[0].constant + prod.constant);
                            continue;
                        }
                        this.children.Insert(0, prod);
                        continue;
                    }
                    this.children.Add(prod);
                }
            }
            this.__merge_similar_nodes_sum();
            if (this.children.Count() == 1)
            {
                if (!(keepSum))
                {
                    this.copy(this.children[0]);
                }
            }
            else
            {
                if (this.children.Count() == 0)
                {
                    this.copy(this.__new_constant_node(0));
                }
            }
        }

        public dynamic __get_product_with_node(dynamic other)
        {
            if (this.type == NodeType.CONSTANT)
            {
                return this.__get_product_of_constant_and_node(other);
            }
            if (other.type == NodeType.CONSTANT)
            {
                return other.__get_product_of_constant_and_node(self);
            }
            if (this.type == NodeType.PRODUCT)
            {
                if (other.type == NodeType.PRODUCT)
                {
                    return this.__get_product_of_products(other);
                }
                if (other.type == NodeType.POWER)
                {
                    return this.__get_product_of_product_and_power(other);
                }
                return this.__get_product_of_product_and_other(other);
            }
            if (this.type == NodeType.POWER)
            {
                if (other.type == NodeType.POWER)
                {
                    return this.__get_product_of_powers(other);
                }
                if (other.type == NodeType.PRODUCT)
                {
                    return other.__get_product_of_product_and_power(self);
                }
                return this.__get_product_of_power_and_other(other);
            }
            if (other.type == NodeType.PRODUCT)
            {
                return other.__get_product_of_product_and_other(self);
            }
            if (other.type == NodeType.POWER)
            {
                return other.__get_product_of_power_and_other(self);
            }
            return this.__get_product_generic(other);
        }

        public Node __get_product_of_constant_and_node(dynamic other)
        {
            Assert.True(this.type == NodeType.CONSTANT);
            var node = other.get_copy();
            node.__multiply(this.constant);
            return node;
        }

        public Node __get_product_of_products(dynamic other)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            Assert.True(other.type == NodeType.PRODUCT);
            var node = this.get_copy();
            foreach (var ochild in other.children)
            {
                if (ochild.type == NodeType.CONSTANT)
                {
                    if (node.children[0].type == NodeType.CONSTANT)
                    {
                        node.children[0].__set_and_reduce_constant(node.children[0].constant * ochild.constant);
                        if (node.children[0].__is_constant(0))
                        {
                            return node.children[0];
                        }
                    }
                    else
                    {
                        node.children.Insert(0, ochild.get_copy());
                    }
                    continue;
                }
                if (ochild.type == NodeType.POWER)
                {
                    node.__merge_power_into_product(ochild);
                    continue;
                }
                var merged = false;
                foreach (var i in range(node.children.Count()))
                {
                    var child = node.children[i];
                    if (child.Equals(ochild))
                    {
                        node.children[i] = this.__new_node_with_children(NodeType.POWER, new List<dynamic>() { child, this.__new_constant_node(2) });
                        merged = true;
                        break;
                    }
                }
                if (merged)
                {
                    continue;
                }
                node.children.Add(ochild.get_copy());
            }
            if (node.children[0].__is_constant(1))
            {
                node.children.RemoveAt(0);
            }
            if (node.children.Count() == 1)
            {
                return node.children[0];
            }
            if (node.children.Count() == 0)
            {
                return this.__new_constant_node(1);
            }
            return node;
        }

        public void __merge_power_into_product(dynamic other)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            Assert.True(other.type == NodeType.POWER);
            var base = other.children[0];
            foreach (var i in range(this.children.Count()))
            {
                var child = this.children[i];
                if (child.Equals(base))
                {
                    this.children[i] = other.get_copy();
                    this.children[i].children[1].__add_constant(1);
                    if (this.children[i].children[1].__is_constant(0))
                    {
                        this.children.RemoveAt(i);
                    }
                    return;
                }
                if ((child.type == NodeType.POWER && child.children[0].Equals(base)))
                {
                    child.children[1].__add(other.children[1]);
                    if (child.children[1].__is_constant(0))
                    {
                        this.children.RemoveAt(i);
                    }
                    return;
                }
            }
            this.children.Add(other.get_copy());
        }

        public dynamic __get_product_of_powers(dynamic other)
        {
            Assert.True(this.type == NodeType.POWER);
            Assert.True(other.type == NodeType.POWER);
            if (this.children[0].Equals(other.children[0]))
            {
                var node = this.get_copy();
                node.children[1].__add(other.children[1]);
                if (node.children[1].__is_constant(0))
                {
                    return this.__new_constant_node(1);
                }
                if (node.children[1].__is_constant(1))
                {
                    return node.children[0];
                }
                return node;
            }
            return this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { this.get_copy(), other.get_copy() });
        }

        public Node __get_product_of_product_and_power(dynamic other)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            Assert.True(other.type == NodeType.POWER);
            var node = this.get_copy();
            node.__merge_power_into_product(other);
            if (node.children.Count() == 1)
            {
                return node.children[0];
            }
            if (node.children.Count() == 0)
            {
                return this.__new_constant_node(1);
            }
            return node;
        }

        public Node __get_product_of_product_and_other(dynamic other)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            Assert.True(!((new List<dynamic>() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER }).Contains(other.type)));
            var node = this.get_copy();
            foreach (var i in range(node.children.Count()))
            {
                var child = node.children[i];
                if (child.Equals(other))
                {
                    node.children[i] = this.__new_node_with_children(NodeType.POWER, new List<dynamic>() { child.get_copy(), this.__new_constant_node(2) });
                    return node;
                }
                if ((child.type == NodeType.POWER && child.children[0].Equals(other)))
                {
                    child.children[1].__add_constant(1);
                    if (child.children[1].__is_constant(0))
                    {
                        node.children.RemoveAt(i);
                        if (node.children.Count() == 1)
                        {
                            node = this.children[0];
                        }
                    }
                    return node;
                }
            }
            node.children.Add(other.get_copy());
            return node;
        }

        public dynamic __get_product_of_power_and_other(dynamic other)
        {
            Assert.True(this.type == NodeType.POWER);
            Assert.True(!((new List<dynamic>() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER }).Contains(other.type)));
            if (this.children[0].Equals(other))
            {
                var node = this.get_copy();
                node.children[1].__add_constant(1);
                if (node.children[1].__is_constant(0))
                {
                    return this.__new_constant_node(1);
                }
                return node;
            }
            return this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { this.get_copy(), other.get_copy() });
        }

        public dynamic __get_product_generic(dynamic other)
        {
            Assert.True(!((new List<dynamic>() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER }).Contains(this.type)));
            Assert.True(!((new List<dynamic>() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER }).Contains(other.type)));
            if (this.Equals(other))
            {
                return this.__new_node_with_children(NodeType.POWER, new List<dynamic>() { this.get_copy(), this.__new_constant_node(2) });
            }
            return this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { this.get_copy(), other.get_copy() });
        }

        public void __multiply_with_node_no_sum(dynamic other)
        {
            Assert.True(other.type != NodeType.SUM);
            if (this.type == NodeType.CONSTANT)
            {
                this.copy(this.__get_product_of_constant_and_node(other));
                return;
            }
            if (other.type == NodeType.CONSTANT)
            {
                this.__multiply(other.constant);
                return;
            }
            if (this.type == NodeType.PRODUCT)
            {
                if (other.type == NodeType.PRODUCT)
                {
                    this.__multiply_product_with_product(other);
                }
                else
                {
                    if (other.type == NodeType.POWER)
                    {
                        this.__multiply_product_with_power(other);
                    }
                    else
                    {
                        this.__multiply_product_with_other(other);
                    }
                }
                return;
            }
            if (this.type == NodeType.POWER)
            {
                if (other.type == NodeType.POWER)
                {
                    this.__multiply_power_with_power(other);
                }
                else
                {
                    if (other.type == NodeType.PRODUCT)
                    {
                        this.copy(other.__get_product_of_product_and_power(self));
                    }
                    else
                    {
                        this.__multiply_power_with_other(other);
                    }
                }
                return;
            }
            if (other.type == NodeType.PRODUCT)
            {
                this.copy(other.__get_product_of_product_and_other(self));
            }
            else
            {
                if (other.type == NodeType.POWER)
                {
                    this.copy(other.__get_product_of_power_and_other(self));
                }
                else
                {
                    this.__multiply_generic(other);
                }
            }
        }

        public void __multiply_product_with_product(dynamic other)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            Assert.True(other.type == NodeType.PRODUCT);
            foreach (var ochild in other.children)
            {
                if (ochild.type == NodeType.CONSTANT)
                {
                    if (this.children[0].type == NodeType.CONSTANT)
                    {
                        this.children[0].__set_and_reduce_constant(this.children[0].constant * ochild.constant);
                        if (this.children[0].__is_constant(0))
                        {
                            this.copy(this.children[0]);
                            return;
                        }
                    }
                    else
                    {
                        this.children.Insert(0, ochild.get_copy());
                    }
                    continue;
                }
                if (ochild.type == NodeType.POWER)
                {
                    this.__merge_power_into_product(ochild);
                    continue;
                }
                var merged = false;
                foreach (var i in range(this.children.Count()))
                {
                    var child = this.children[i];
                    if (child.Equals(ochild))
                    {
                        this.children[i] = this.__new_node_with_children(NodeType.POWER, new List<dynamic>() { child, this.__new_constant_node(2) });
                        merged = true;
                        break;
                    }
                }
                if (merged)
                {
                    continue;
                }
                this.children.Add(ochild.get_copy());
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            else
            {
                if (this.children.Count() == 0)
                {
                    this.copy(this.__new_constant_node(1));
                }
            }
        }

        public void __multiply_power_with_power(dynamic other)
        {
            Assert.True(this.type == NodeType.POWER);
            Assert.True(other.type == NodeType.POWER);
            if (this.children[0].Equals(other.children[0]))
            {
                this.children[1].__add(other.children[1]);
                if (this.children[1].__is_constant(0))
                {
                    this.copy(this.__new_constant_node(1));
                }
                else
                {
                    if (this.children[1].__is_constant(1))
                    {
                        this.copy(this.children[0]);
                    }
                }
            }
            else
            {
                this.copy(this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { this.__get_shallow_copy(), other.get_copy() }));
            }
        }

        public void __multiply_product_with_power(dynamic other)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            Assert.True(other.type == NodeType.POWER);
            this.__merge_power_into_product(other);
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            else
            {
                if (this.children.Count() == 0)
                {
                    return this.copy(this.__new_constant_node(1));
                }
            }
        }

        public void __multiply_product_with_other(dynamic other)
        {
            Assert.True(this.type == NodeType.PRODUCT);
            Assert.True(!((new List<dynamic>() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER }).Contains(other.type)));
            foreach (var i in range(this.children.Count()))
            {
                var child = this.children[i];
                if (child.Equals(other))
                {
                    this.children[i] = this.__new_node_with_children(NodeType.POWER, new List<dynamic>() { child.__get_shallow_copy(), this.__new_constant_node(2) });
                    return;
                }
                if ((child.type == NodeType.POWER && child.children[0].Equals(other)))
                {
                    child.children[1].__add_constant(1);
                    if (child.children[1].__is_constant(0))
                    {
                        this.children.RemoveAt(i);
                        if (this.children.Count() == 1)
                        {
                            this.copy(this.children[0]);
                        }
                    }
                    return;
                }
            }
            this.children.Add(other.get_copy());
        }

        public void __multiply_power_with_other(dynamic other)
        {
            Assert.True(this.type == NodeType.POWER);
            Assert.True(!((new List<dynamic>() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER }).Contains(other.type)));
            if (this.children[0].Equals(other))
            {
                this.children[1].__add_constant(1);
                if (this.children[1].__is_constant(0))
                {
                    this.copy(this.__new_constant_node(1));
                }
                return;
            }
            this.copy(this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { this.__get_shallow_copy(), other.get_copy() }));
        }

        public void __multiply_generic(dynamic other)
        {
            Assert.True(!((new List<dynamic>() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER }).Contains(this.type)));
            Assert.True(!((new List<dynamic>() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER }).Contains(other.type)));
            if (this.Equals(other))
            {
                this.copy(this.__new_node_with_children(NodeType.POWER, new List<dynamic>() { this.__get_shallow_copy(), this.__new_constant_node(2) }));
            }
            else
            {
                this.copy(this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { this.__get_shallow_copy(), other.get_copy() }));
            }
        }

        public bool __check_expand_power()
        {
            Assert.True(this.type == NodeType.POWER);
            if (this.children[0].type != NodeType.SUM)
            {
                return false;
            }
            var expNode = this.children[1];
            if (expNode.type != NodeType.CONSTANT)
            {
                return false;
            }
            var exp = expNode.constant % this.__modulus;
            if (exp > MAX_EXPONENT_TO_EXPAND)
            {
                return false;
            }
            this.__expand_power(exp);
            return true;
        }

        public void __expand_power(dynamic exp)
        {
            var base = this.children[0];
            var node = base.get_copy();
            Assert.True(node.type == NodeType.SUM);
            foreach (var i in range(1, exp))
            {
                node.__multiply_sum_with_sum(base, true);
                if (node.__is_constant(0))
                {
                    break;
                }
            }
            if (node.children.Count() == 1)
            {
                this.copy(node.children[0]);
            }
            else
            {
                this.copy(node);
            }
        }

        public dynamic factorize_sums(bool restrictedScope = false)
        {
            if (restrictedScope)
            {
                return this.__check_factorize_sum();
            }
            var changed = false;
            foreach (var c in this.children)
            {
                if (c.factorize_sums())
                {
                    changed = true;
                }
            }
            if (this.__check_factorize_sum())
            {
                changed = true;
            }
            return changed;
        }

        public bool __check_factorize_sum()
        {
            if ((this.type != NodeType.SUM || this.children.Count() <= 1))
            {
                return false;
            }
            if (this.is_linear())
            {
                return false;
            }
            var (nodes, nodesToTerms, termsToNodes) = this.__collect_all_factors_of_sum();
            var nodesTriviality = this.__determine_nodes_triviality(nodes);
            var nodesOrder = this.__determine_nodes_order(nodes);
            var partition = Batch(new List<dynamic>() { }, new List<dynamic>() { }, set(range(this.children.Count())), nodesToTerms, termsToNodes, nodesTriviality, nodesOrder);
            if (partition.is_trivial())
            {
                return false;
            }
            this.copy(this.__node_from_batch(partition, nodes, termsToNodes));
            return true;
        }

        public dynamic __collect_all_factors_of_sum()
        {
            var nodes = new List<dynamic>() { };
            var nodesToTerms = new List<dynamic>() { };
            var termsToNodes = new List<dynamic>() { };
            foreach (var i in range(this.children.Count()))
            {
                termsToNodes.Add(set(new List<dynamic>() { }));
                var term = this.children[i];
                term.__collect_factors(i, 1, nodes, nodesToTerms, termsToNodes);
            }
            Assert.True(nodes.Count() == nodesToTerms.Count());
            return (nodes, nodesToTerms, termsToNodes);
        }

        public void __collect_factors(dynamic i, dynamic multitude, dynamic nodes, dynamic nodesToTerms, dynamic termsToNodes)
        {
            if (this.type == NodeType.PRODUCT)
            {
                foreach (var factor in this.children)
                {
                    factor.__collect_factors(i, multitude, nodes, nodesToTerms, termsToNodes);
                }
            }
            else
            {
                if (this.type == NodeType.POWER)
                {
                    this.__collect_factors_of_power(i, multitude, nodes, nodesToTerms, termsToNodes);
                }
                else
                {
                    if (this.type != NodeType.CONSTANT)
                    {
                        this.__check_store_factor(i, multitude, nodes, nodesToTerms, termsToNodes);
                    }
                }
            }
        }

        public void __collect_factors_of_power(dynamic i, dynamic multitude, dynamic nodes, dynamic nodesToTerms, dynamic termsToNodes)
        {
            Assert.True(this.type == NodeType.POWER);
            var base = this.children[0];
            var exp = this.children[1];
            if (exp.type == NodeType.CONSTANT)
            {
                base.__collect_factors(i, exp.constant * multitude % this.__modulus, nodes, nodesToTerms, termsToNodes);
                return;
            }
            if (exp.type == NodeType.SUM)
            {
                var first = exp.children[0];
                if (first.type == NodeType.CONSTANT)
                {
                    base.__collect_factors(i, first.constant * multitude % this.__modulus, nodes, nodesToTerms, termsToNodes);
                    var node = this.get_copy();
                    node.children[1].children.RemoveAt(0);
                    if (node.children[1].children.Count() == 1)
                    {
                        node.children[1] = node.children[1].children[0];
                    }
                    node.__check_store_factor(i, multitude, nodes, nodesToTerms, termsToNodes);
                    return;
                }
            }
            this.__check_store_factor(i, multitude, nodes, nodesToTerms, termsToNodes);
        }

        public void __check_store_factor(dynamic i, dynamic multitude, dynamic nodes, dynamic nodesToTerms, dynamic termsToNodes)
        {
            var idx = this.__get_index_in_list(nodes);
            if (idx == null)
            {
                nodes.Add(this.__get_shallow_copy());
                nodesToTerms.Add(new List<dynamic>() { nodes.Count() - 1, set(new List<dynamic>() { IndexWithMultitude(i, multitude) }) });
                termsToNodes[i].add(IndexWithMultitude(nodes.Count() - 1, multitude));
                return;
            }
            var ntt = nodesToTerms[idx][1];
            var res = ntt.Where(p => p.idx == i).Select(p => p);
            Assert.True(res.Count() <= 1);
            if (res.Count() == 1)
            {
                res[0].multitude += multitude;
            }
            else
            {
                ntt.add(IndexWithMultitude(i, multitude));
            }
            var ttn = termsToNodes[i];
            var res2 = ttn.Where(p => p.idx == idx).Select(p => p);
            Assert.True(res2.Count() <= 1);
            Assert.True(res2.Count() == 1 == res.Count() == 1);
            if (res2.Count() == 1)
            {
                res2[0].multitude += multitude;
            }
            else
            {
                ttn.add(IndexWithMultitude(idx, multitude));
            }
        }

        public dynamic __determine_nodes_triviality(dynamic nodes)
        {
            return nodes.Where(n => ).Select(x => n.__is_trivial_in_factorization());
        }

        public dynamic __determine_nodes_order(dynamic nodes)
        {
            var enumNodes = list(enumerate(nodes));
            enumNodes.sort();
            return enumNodes.Where(p => ).Select(x => p[0]);
        }

        public dynamic __is_trivial_in_factorization()
        {
            return !(this.__is_bitwise_binop());
        }

        public Node __node_from_batch(dynamic batch, dynamic nodes, dynamic termsToNodes)
        {
            var node = this.__new_node(NodeType.SUM);
            foreach (var c in batch.children)
            {
                node.children.Add(this.__node_from_batch(c, nodes, termsToNodes));
            }
            foreach (var a in batch.atoms)
            {
                this.__reduce_node_set(termsToNodes[a], batch.factorIndices, batch.prevFactorIndices);
                var nodeIndices = termsToNodes[a];
                var term = this.children[a];
                var constant = term.__get_const_factor_respecting_powers();
                if (nodeIndices.Count() == 0)
                {
                    node.__add_constant(constant);
                    continue;
                }
                if ((nodeIndices.Count() == 1 && constant == 1))
                {
                    var p = nodeIndices.pop();
                    node.children.Add(this.__create_node_for_factor(nodes, p));
                    continue;
                }
                var prod = this.__new_node(NodeType.PRODUCT);
                if (constant != 1)
                {
                    prod.children.Add(this.__new_constant_node(constant));
                }
                foreach (var p in nodeIndices)
                {
                    prod.children.Add(this.__create_node_for_factor(nodes, p));
                }
                prod.__check_resolve_product_of_powers();
                node.children.Add(prod);
            }
            if (node.children.Count() == 1)
            {
                node.copy(node.children[0]);
            }
            if (batch.factorIndices.Count() == 0)
            {
                return node;
            }
            prod = this.__new_node(NodeType.PRODUCT);
            foreach (var p in batch.factorIndices)
            {
                prod.children.Add(this.__create_node_for_factor(nodes, p));
            }
            if ((node.children.Count() == 1 && node.children[0].type == NodeType.CONSTANT))
            {
                prod.children.Add(node);
            }
            else
            {
                prod.children.Insert(0, node);
            }
            prod.__check_resolve_product_of_powers();
            return prod;
        }

        public void __reduce_node_set(dynamic indicesWithMultitudes, dynamic l1, dynamic l2)
        {
            foreach (var p in l1 + l2)
            {
                var m = indicesWithMultitudes.Where(q => q.idx == p.idx).Select(q => q);
                Assert.True(m.Count() == 1);
                Assert.True(m[0].multitude >= p.multitude);
                m[0].multitude -= p.multitude;
                if (m[0].multitude == 0)
                {
                    indicesWithMultitudes.remove(m[0]);
                }
            }
        }

        public dynamic __get_const_factor_respecting_powers()
        {
            if (this.type == NodeType.CONSTANT)
            {
                return this.constant;
            }
            if (this.type == NodeType.PRODUCT)
            {
                var f = 1;
                foreach (var child in this.children)
                {
                    f = this.__get_reduced_constant(f * child.__get_const_factor_respecting_powers());
                }
                return f;
            }
            if (this.type != NodeType.POWER)
            {
                return 1;
            }
            var base = this.children[0];
            if (base.type != NodeType.PRODUCT)
            {
                return 1;
            }
            if (base.children[0].type != NodeType.CONSTANT)
            {
                return 1;
            }
            var const = base.children[0].constant;
            var exp = this.children[1];
            if (exp.type == NodeType.CONSTANT)
            {
                return this.__power(const, exp.constant);
            }
            if (exp.type != NodeType.SUM)
            {
                return 1;
            }
            if (exp.children[0].type != NodeType.CONSTANT)
            {
                return 1;
            }
            return this.__power(const, exp.children[0].constant);
        }

        public dynamic __create_node_for_factor(dynamic nodes, dynamic indexWithMultitude)
        {
            var exp = indexWithMultitude.multitude;
            Assert.True(exp > 0);
            var idx = indexWithMultitude.idx;
            if (exp == 1)
            {
                return nodes[idx].get_copy();
            }
            return this.__new_node_with_children(NodeType.POWER, new List<dynamic>() { nodes[idx].get_copy(), this.__new_constant_node(exp) });
        }

        public dynamic __insert_fixed_in_conj()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return false;
            }
            var changed = false;
            foreach (var i in range(this.children.Count()))
            {
                var child1 = this.children[i];
                foreach (var j in range(this.children.Count()))
                {
                    if (i == j)
                    {
                        continue;
                    }
                    if (this.children[j].__check_insert_fixed_true(child1))
                    {
                        changed = true;
                    }
                }
            }
            return changed;
        }

        public dynamic __check_insert_fixed_true(dynamic node)
        {
            if (!(this.__is_bitwise_op()))
            {
                return false;
            }
            var changed = false;
            foreach (var child in this.children)
            {
                if (child.Equals(node))
                {
                    child.copy(this.__new_constant_node(-(1)));
                    changed = true;
                }
                else
                {
                    if (child.__is_bitwise_op())
                    {
                        if (child.__check_insert_fixed_true(node))
                        {
                            changed = true;
                        }
                    }
                }
            }
            return changed;
        }

        public dynamic __insert_fixed_in_disj()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return false;
            }
            var changed = false;
            foreach (var i in range(this.children.Count()))
            {
                var child1 = this.children[i];
                foreach (var j in range(this.children.Count()))
                {
                    if (i == j)
                    {
                        continue;
                    }
                    if (this.children[j].__check_insert_fixed_false(child1))
                    {
                        changed = true;
                    }
                }
            }
            return changed;
        }

        public dynamic __check_insert_fixed_false(dynamic node)
        {
            if (!(this.__is_bitwise_op()))
            {
                return false;
            }
            var changed = false;
            foreach (var child in this.children)
            {
                if (child.Equals(node))
                {
                    child.copy(this.__new_constant_node(0));
                    changed = true;
                }
                else
                {
                    if (child.__is_bitwise_op())
                    {
                        if (child.__check_insert_fixed_false(node))
                        {
                            changed = true;
                        }
                    }
                }
            }
            return changed;
        }

        public bool __check_trivial_xor()
        {
            if (this.type != NodeType.EXCL_DISJUNCTION)
            {
                return false;
            }
            var c = this.children[0];
            if (this.children.Count() == 2)
            {
                if (c.__is_constant(-(1)))
                {
                    this.type = NodeType.NEGATION;
                    this.children.RemoveAt(0);
                    return true;
                }
                if (c.__is_constant(0))
                {
                    this.copy(this.children[1]);
                    return true;
                }
            }
            else
            {
                Assert.True(this.children.Count() > 2);
                if (c.__is_constant(0))
                {
                    this.children.RemoveAt(0);
                    return true;
                }
            }
            return false;
        }

        public dynamic __check_xor_same_mult_by_minus_one()
        {
            if (!(this.type == NodeType.PRODUCT))
            {
                return false;
            }
            var first = this.children[0];
            if (!(first.type == NodeType.CONSTANT))
            {
                return false;
            }
            if (first.constant % 2 != 0)
            {
                return false;
            }
            var changed = false;
            foreach (var i in range(this.children.Count() - 1, 0, -(1)))
            {
                var child = this.children[i];
                if (!((new List<dynamic>() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION }).Contains(child.type)))
                {
                    continue;
                }
                if (child.children.Count() != 2)
                {
                    continue;
                }
                var node = child.children[0].get_copy();
                node.__multiply_by_minus_one();
                if (!(node.Equals(child.children[1])))
                {
                    continue;
                }
                first.constant /= 2;
                if (child.type == NodeType.CONJUNCTION)
                {
                    first.__set_and_reduce_constant(-(first.constant));
                }
                child.type = NodeType.EXCL_DISJUNCTION;
                changed = true;
                if (first.constant % 2 != 0)
                {
                    break;
                }
            }
            return changed;
        }

        public bool __check_conj_zero_rule()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return false;
            }
            if (!(this.__has_conj_zero_rule()))
            {
                return false;
            }
            this.copy(this.__new_constant_node(0));
            return true;
        }

        public bool __has_conj_zero_rule()
        {
            Assert.True(this.type == NodeType.CONJUNCTION);
            foreach (var i in range(this.children.Count() - 1))
            {
                var child1 = this.children[i];
                foreach (var j in range(i + 1, this.children.Count()))
                {
                    var child2 = this.children[j];
                    var neg2 = child2.get_copy();
                    neg2.__multiply_by_minus_one();
                    if (!(child1.Equals(neg2)))
                    {
                        continue;
                    }
                    var double1 = child1.get_copy();
                    double1.__multiply(2);
                    var double2 = child2.get_copy();
                    double2.__multiply(2);
                    foreach (var k in range(this.children.Count()))
                    {
                        if (((new List<dynamic>() { i, j }).Contains(k)))
                        {
                            continue;
                        }
                        var child3 = this.children[k];
                        if ((child3.Equals(double1) || child3.Equals(double2)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool __check_conj_neg_xor_zero_rule()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return false;
            }
            if (!(this.__has_conj_neg_xor_zero_rule()))
            {
                return false;
            }
            this.copy(this.__new_constant_node(0));
            return true;
        }

        public bool __has_conj_neg_xor_zero_rule()
        {
            Assert.True(this.type == NodeType.CONJUNCTION);
            foreach (var i in range(this.children.Count()))
            {
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_neg_xor_same_neg();
                if (node == null)
                {
                    continue;
                }
                var node2 = node.get_copy();
                node2.__multiply_by_minus_one();
                foreach (var j in range(this.children.Count()))
                {
                    if (i == j)
                    {
                        continue;
                    }
                    var neg2 = this.children[j].get_copy();
                    neg2.__negate();
                    if ((neg2.__is_double(node) || neg2.__is_double(node2)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void __get_opt_arg_neg_xor_same_neg()
        {
            if (this.type != NodeType.PRODUCT)
            {
                return null;
            }
            if (this.children.Count() != 2)
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var xor = this.children[1];
            if (xor.type != NodeType.EXCL_DISJUNCTION)
            {
                return null;
            }
            if (xor.children.Count() != 2)
            {
                return null;
            }
            var node0 = xor.children[0].get_copy();
            node0.__multiply_by_minus_one();
            if (node0.Equals(xor.children[1]))
            {
                return node0;
            }
            return null;
        }

        public bool __check_conj_neg_xor_minus_one_rule()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return false;
            }
            if (!(this.__has_disj_neg_xor_minus_one_rule()))
            {
                return false;
            }
            this.copy(this.__new_constant_node(-(1)));
            return true;
        }

        public bool __has_disj_neg_xor_minus_one_rule()
        {
            Assert.True(this.type == NodeType.INCL_DISJUNCTION);
            foreach (var i in range(this.children.Count()))
            {
                var child1 = this.children[i];
                if (child1.type != NodeType.NEGATION)
                {
                    continue;
                }
                var node = child1.children[0].__get_opt_arg_neg_xor_same_neg();
                if (node == null)
                {
                    continue;
                }
                var node2 = node.get_copy();
                node2.__multiply_by_minus_one();
                foreach (var j in range(this.children.Count()))
                {
                    if (i == j)
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if ((child2.__is_double(node) || child2.__is_double(node2)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool __check_conj_negated_xor_zero_rule()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return false;
            }
            if (!(this.__has_conj_negated_xor_zero_rule()))
            {
                return false;
            }
            this.copy(this.__new_constant_node(0));
            return true;
        }

        public bool __has_conj_negated_xor_zero_rule()
        {
            Assert.True(this.type == NodeType.CONJUNCTION);
            foreach (var i in range(this.children.Count()))
            {
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_negated_xor_same_neg();
                if (node == null)
                {
                    continue;
                }
                var node2 = node.get_copy();
                node2.__multiply_by_minus_one();
                foreach (var j in range(this.children.Count()))
                {
                    if (i == j)
                    {
                        continue;
                    }
                    var child2 = this.children[j].get_copy();
                    if ((child2.__is_double(node) || child2.__is_double(node2)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void __get_opt_arg_negated_xor_same_neg()
        {
            if (this.type != NodeType.NEGATION)
            {
                return null;
            }
            var xor = this.children[0];
            if (xor.type != NodeType.EXCL_DISJUNCTION)
            {
                return null;
            }
            if (xor.children.Count() != 2)
            {
                return null;
            }
            var node0 = xor.children[0].get_copy();
            node0.__multiply_by_minus_one();
            if (node0.Equals(xor.children[1]))
            {
                return node0;
            }
            return null;
        }

        public dynamic __check_conj_xor_identity_rule()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                if (!(child1.__is_xor_same_neg()))
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if ((child2.__is_double(child1.children[0]) || child2.__is_double(child1.children[1])))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public dynamic __is_xor_same_neg()
        {
            if (this.type != NodeType.EXCL_DISJUNCTION)
            {
                return false;
            }
            if (this.children.Count() != 2)
            {
                return false;
            }
            var neg = this.children[1].get_copy();
            neg.__multiply_by_minus_one();
            return neg.Equals(this.children[0]);
        }

        public dynamic __is_double(dynamic node)
        {
            var cpy = node.get_copy();
            cpy.__multiply(2);
            return this.Equals(cpy);
        }

        public dynamic __check_disj_xor_identity_rule()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_xor_disj_xor_identity();
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if ((child2.__is_double(node.children[0]) || child2.__is_double(node.children[1])))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public dynamic __get_opt_xor_disj_xor_identity()
        {
            if (this.type != NodeType.PRODUCT)
            {
                return null;
            }
            if (this.children.Count() != 2)
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var child = this.children[1];
            if (child.type != NodeType.EXCL_DISJUNCTION)
            {
                return null;
            }
            if (child.children.Count() != 2)
            {
                return null;
            }
            var neg = child.children[1].get_copy();
            neg.__multiply_by_minus_one();
            return (neg.Equals(child.children[0])) ? child : null;
        }

        public dynamic __check_conj_neg_conj_identity_rule()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_neg_conj_double();
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    var neg = child2.get_copy();
                    neg.__multiply_by_minus_one();
                    if (neg.Equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public dynamic __get_opt_arg_neg_conj_double()
        {
            var node = this.__get_opt_arg_neg_conj_double_1();
            return (node != null) ? node : this.__get_opt_arg_neg_conj_double_2();
        }

        public void __get_opt_arg_neg_conj_double_1()
        {
            if (this.type != NodeType.NEGATION)
            {
                return null;
            }
            var child = this.children[0];
            if (child.type != NodeType.CONJUNCTION)
            {
                return null;
            }
            if (child.children.Count() != 2)
            {
                return null;
            }
            if (child.children[0].__is_double(child.children[1]))
            {
                return child.children[1];
            }
            if (child.children[1].__is_double(child.children[0]))
            {
                return child.children[0];
            }
            var node = child.children[0].get_copy();
            node.__multiply_by_minus_one();
            if (node.__is_double(child.children[1]))
            {
                return child.children[1];
            }
            node = child.children[1].get_copy();
            node.__multiply_by_minus_one();
            if (node.__is_double(child.children[0]))
            {
                return child.children[0];
            }
            return null;
        }

        public void __get_opt_arg_neg_conj_double_2()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return null;
            }
            if (this.children.Count() != 2)
            {
                return null;
            }
            var node0 = this.children[0].get_copy();
            node0.__negate();
            var node1 = this.children[1].get_copy();
            node1.__negate();
            if (node0.__is_double(node1))
            {
                return node1;
            }
            if (node1.__is_double(node0))
            {
                return node0;
            }
            var node = node0.get_copy();
            node.__multiply_by_minus_one();
            if (node.__is_double(node1))
            {
                return node1;
            }
            node1.__multiply_by_minus_one();
            if (node1.__is_double(node0))
            {
                return node0;
            }
            return null;
        }

        public dynamic __check_disj_disj_identity_rule()
        {
            return this.__check_nested_bitwise_identity_rule(NodeType.INCL_DISJUNCTION);
        }

        public dynamic __check_conj_conj_identity_rule()
        {
            return this.__check_nested_bitwise_identity_rule(NodeType.CONJUNCTION);
        }

        public dynamic __check_nested_bitwise_identity_rule(dynamic t)
        {
            if (this.type != t)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var nodes = child1.__get_candidates_nested_bitwise_identity(t);
                Assert.True(nodes.Count() <= 2);
                if (nodes.Count() == 0)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    var done = false;
                    foreach (var node in nodes)
                    {
                        if (child2.Equals(node))
                        {
                            this.children.RemoveAt(i);
                            changed = true;
                            done = true;
                            i -= 1;
                            break;
                        }
                    }
                    if (done)
                    {
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public dynamic __get_candidates_nested_bitwise_identity(dynamic t)
        {
            if (this.type != NodeType.PRODUCT)
            {
                return new List<dynamic>() { };
            }
            if (this.children.Count() != 2)
            {
                return new List<dynamic>() { };
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return new List<dynamic>() { };
            }
            var bitw = this.children[1];
            if (bitw.type != t)
            {
                return new List<dynamic>() { };
            }
            if (bitw.children.Count() != 2)
            {
                return new List<dynamic>() { };
            }
            var neg = bitw.children[1].get_copy();
            neg.__multiply_by_minus_one();
            if (neg.Equals(bitw.children[0]))
            {
                return new List<dynamic>() { bitw.children[0], bitw.children[1] };
            }
            var ot = (t == NodeType.INCL_DISJUNCTION) ? NodeType.CONJUNCTION : NodeType.INCL_DISJUNCTION;
            if (bitw.children[0].type == ot)
            {
                if (bitw.children[0].__has_child(neg))
                {
                    return new List<dynamic>() { neg };
                }
            }
            if (bitw.children[1].type == ot)
            {
                neg = bitw.children[0].get_copy();
                neg.__multiply_by_minus_one();
                if (bitw.children[1].__has_child(neg))
                {
                    return new List<dynamic>() { neg };
                }
            }
            return (neg.Equals(bitw.children[0])) ? bitw : new List<dynamic>() { };
        }

        public dynamic __check_disj_conj_identity_rule()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_conj_identity();
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    var neg = child2.get_copy();
                    neg.__multiply_by_minus_one();
                    if (neg.Equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public dynamic __get_opt_arg_disj_conj_identity()
        {
            var node = this.__get_opt_arg_disj_conj_identity_1();
            return (node != null) ? node : this.__get_opt_arg_disj_conj_identity_2();
        }

        public void __get_opt_arg_disj_conj_identity_1()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return null;
            }
            if (this.children.Count() != 2)
            {
                return null;
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                var oIdx = (idx == 1) ? 0 : 1;
                var oDiv = this.children[oIdx].__divided(2);
                if (oDiv == null)
                {
                    continue;
                }
                var oDivNeg = oDiv.get_copy();
                oDivNeg.__multiply_by_minus_one();
                var node = this.children[idx];
                if (node.type == NodeType.NEGATION)
                {
                    var neg = node.children[0];
                    if ((neg.Equals(oDiv) || neg.Equals(oDivNeg)))
                    {
                        return neg;
                    }
                }
                if (oDiv.type == NodeType.NEGATION)
                {
                    neg = oDiv.children[0];
                    if (neg.Equals(node))
                    {
                        return oDiv;
                    }
                }
                if (oDivNeg.type == NodeType.NEGATION)
                {
                    neg = oDivNeg.children[0];
                    if (neg.Equals(node))
                    {
                        return oDivNeg;
                    }
                }
                neg = node.__get_opt_transformed_negated();
                if (neg != null)
                {
                    if ((neg.Equals(oDiv) || neg.Equals(oDivNeg)))
                    {
                        return neg;
                    }
                }
                neg = oDiv.__get_opt_transformed_negated();
                if (neg != null)
                {
                    if (neg.Equals(node))
                    {
                        return oDiv;
                    }
                }
                neg = oDivNeg.__get_opt_transformed_negated();
                if (neg != null)
                {
                    if (neg.Equals(node))
                    {
                        return oDivNeg;
                    }
                }
            }
            return null;
        }

        public void __get_opt_arg_disj_conj_identity_2()
        {
            if (this.type != NodeType.NEGATION)
            {
                return null;
            }
            var child = this.children[0];
            if (child.type != NodeType.INCL_DISJUNCTION)
            {
                return null;
            }
            if (child.children.Count() != 2)
            {
                return null;
            }
            foreach (var negIdx in new List<dynamic>() { 0, 1 })
            {
                var ch = child.children[negIdx];
                var node = null;
                if (ch.type == NodeType.NEGATION)
                {
                    node = ch.children[0];
                }
                else
                {
                    node = ch.__get_opt_transformed_negated();
                }
                if (node == null)
                {
                    continue;
                }
                var oIdx = (negIdx == 1) ? 0 : 1;
                var other = child.children[oIdx];
                if (node.__is_double(other))
                {
                    return other;
                }
                var neg = node.get_copy();
                neg.__multiply_by_minus_one();
                if (neg.__is_double(other))
                {
                    return other;
                }
            }
            return null;
        }

        public void __divided(dynamic divisor)
        {
            if (this.type == NodeType.CONSTANT)
            {
                if (this.constant % divisor == 0)
                {
                    return this.__new_constant_node(this.constant / divisor);
                }
            }
            if (this.type == NodeType.PRODUCT)
            {
                foreach (var i in range(this.children.Count()))
                {
                    var node = this.children[i].__divided(divisor);
                    if (node == null)
                    {
                        continue;
                    }
                    var res = this.get_copy();
                    res.children[i] = node;
                    if (res.children[i].__is_constant(1))
                    {
                        res.children.RemoveAt(i);
                        if (res.children.Count() == 1)
                        {
                            return res.children[0];
                        }
                    }
                    return res;
                }
                return null;
            }
            if (this.type == NodeType.SUM)
            {
                res = this.__new_node(NodeType.SUM);
                foreach (var child in this.children)
                {
                    node = child.__divided(divisor);
                    if (node == null)
                    {
                        return null;
                    }
                    res.children.Add(node);
                }
                return res;
            }
            return null;
        }

        public dynamic __check_disj_conj_identity_rule_2()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_conj_identity_rule_2();
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (this.children[j].Equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public void __get_opt_arg_disj_conj_identity_rule_2()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return null;
            }
            if (this.children.Count() != 2)
            {
                return null;
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                var oIdx = (idx == 1) ? 0 : 1;
                if (this.children[oIdx].__is_double(this.children[idx]))
                {
                    var node = this.children[idx].get_copy();
                    node.__multiply_by_minus_one();
                    node.__negate();
                    return node;
                }
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                oIdx = (idx == 1) ? 0 : 1;
                node = this.children[idx].get_copy();
                node.__multiply_by_minus_one();
                if (this.children[oIdx].__is_double(node))
                {
                    node.__negate();
                    return node;
                }
            }
            return null;
        }

        public dynamic __check_disj_neg_disj_identity_rule()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_neg_disj_identity_rule();
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (this.children[j].Equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public void __get_opt_arg_disj_neg_disj_identity_rule()
        {
            if (this.type != NodeType.PRODUCT)
            {
                return null;
            }
            if (this.children.Count() != 2)
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var disj = this.children[1];
            if (disj.type != NodeType.INCL_DISJUNCTION)
            {
                return null;
            }
            if (disj.children.Count() != 2)
            {
                return null;
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                var oIdx = (idx == 1) ? 0 : 1;
                if (disj.children[oIdx].__is_double(disj.children[idx]))
                {
                    var node = disj.children[idx].get_copy();
                    node.__multiply_by_minus_one();
                    return node;
                }
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                oIdx = (idx == 1) ? 0 : 1;
                node = disj.children[idx].get_copy();
                node.__multiply_by_minus_one();
                if (disj.children[oIdx].__is_double(node))
                {
                    return node;
                }
            }
            return null;
        }

        public dynamic __check_conj_disj_identity_rule()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_conj_disj_identity();
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if (child2.Equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public dynamic __get_opt_arg_conj_disj_identity()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return null;
            }
            var node = this.__get_opt_arg_conj_disj_identity_1();
            return (node != null) ? node : this.__get_opt_arg_conj_disj_identity_2();
        }

        public void __get_opt_arg_conj_disj_identity_1()
        {
            Assert.True(this.type == NodeType.INCL_DISJUNCTION);
            if (this.children.Count() != 2)
            {
                return null;
            }
            var child0 = this.children[0].get_copy();
            var child1 = this.children[1].get_copy();
            child0.__multiply_by_minus_one();
            child1.__multiply_by_minus_one();
            child0.__negate();
            child1.__negate();
            if (child0.__is_double(child1))
            {
                return child1;
            }
            if (child1.__is_double(child0))
            {
                return child0;
            }
            return null;
        }

        public void __get_opt_arg_conj_disj_identity_2()
        {
            Assert.True(this.type == NodeType.INCL_DISJUNCTION);
            if (this.children.Count() != 2)
            {
                return null;
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                var neg = this.children[idx].get_copy();
                neg.__negate();
                var oIdx = (idx == 1) ? 0 : 1;
                var other = this.children[oIdx];
                var ok = neg.__is_double(other);
                if (!(ok))
                {
                    neg.__multiply_by_minus_one();
                    ok = neg.__is_double(other);
                }
                if (!(ok))
                {
                    continue;
                }
                var node = other.get_copy();
                node.__multiply_by_minus_one();
                node.__negate();
                return node;
            }
            return null;
        }

        public dynamic __check_disj_sub_disj_identity_rule()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_sub_disj_identity();
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (this.children[j].Equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public void __get_opt_arg_disj_sub_disj_identity()
        {
            if (this.type != NodeType.SUM)
            {
                return null;
            }
            if (this.children.Count() != 2)
            {
                return null;
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                var child = this.children[idx];
                if (child.type != NodeType.INCL_DISJUNCTION)
                {
                    continue;
                }
                if (child.children.Count() != 2)
                {
                    continue;
                }
                var oidx = (idx == 1) ? 0 : 1;
                var neg = this.children[oidx].get_copy();
                neg.__multiply_by_minus_one();
                if (neg.Equals(child.children[0]))
                {
                    return child.children[1];
                }
                if (neg.Equals(child.children[1]))
                {
                    return child.children[0];
                }
            }
            return null;
        }

        public dynamic __check_disj_sub_conj_identity_rule()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_sub_conj_identity();
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (this.children[j].Equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public void __get_opt_arg_disj_sub_conj_identity()
        {
            if (this.type != NodeType.SUM)
            {
                return null;
            }
            if (this.children.Count() != 2)
            {
                return null;
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                var child = this.children[idx];
                if (child.type != NodeType.PRODUCT)
                {
                    continue;
                }
                if (child.children.Count() != 2)
                {
                    continue;
                }
                if (!(child.children[0].__is_constant(-(1))))
                {
                    continue;
                }
                var conj = child.children[1];
                if (conj.type != NodeType.CONJUNCTION)
                {
                    continue;
                }
                var oidx = (idx == 1) ? 0 : 1;
                var other = this.children[oidx];
                foreach (var c in conj.children)
                {
                    if (c.Equals(other))
                    {
                        return other;
                    }
                }
            }
            return null;
        }

        public dynamic __check_conj_add_conj_identity_rule()
        {
            if (this.type != NodeType.CONJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_conj_add_conj_identity();
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (this.children[j].Equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public void __get_opt_arg_conj_add_conj_identity()
        {
            if (this.type != NodeType.SUM)
            {
                return null;
            }
            if (this.children.Count() != 2)
            {
                return null;
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                var child = this.children[idx];
                if (child.type != NodeType.CONJUNCTION)
                {
                    continue;
                }
                var oidx = (idx == 1) ? 0 : 1;
                var oneg = this.children[oidx].get_copy();
                oneg.__negate();
                foreach (var c in child.children)
                {
                    if (c.Equals(oneg))
                    {
                        return this.children[oidx];
                    }
                }
            }
            return null;
        }

        public dynamic __check_conj_conj_disj_rule()
        {
            return this.__check_nested_bitwise_rule(NodeType.CONJUNCTION);
        }

        public dynamic __check_disj_disj_conj_rule()
        {
            return this.__check_nested_bitwise_rule(NodeType.INCL_DISJUNCTION);
        }

        public dynamic __check_nested_bitwise_rule(dynamic t)
        {
            if (this.type != t)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var (node1, node2) = child1.__get_opt_arg_nested_bitwise(t);
                Assert.True(node1 == null == node2 == null);
                if (node1 == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (this.children[j].Equals(node1))
                    {
                        this.children[i].copy(node2);
                        changed = true;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public dynamic __get_opt_arg_nested_bitwise(dynamic t)
        {
            if (this.type != NodeType.PRODUCT)
            {
                return (null, null);
            }
            if (this.children.Count() != 2)
            {
                return (null, null);
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return (null, null);
            }
            var child = this.children[1];
            if (child.type != t)
            {
                return (null, null);
            }
            if (child.children.Count() != 2)
            {
                return (null, null);
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                var oidx = (idx == 1) ? 0 : 1;
                var c = child.children[idx];
                var ot = (t == NodeType.INCL_DISJUNCTION) ? NodeType.CONJUNCTION : NodeType.INCL_DISJUNCTION;
                if (c.type != ot)
                {
                    continue;
                }
                if (c.children.Count() != 2)
                {
                    return (null, null);
                }
                var oneg = child.children[oidx].get_copy();
                oneg.__multiply_by_minus_one();
                if (c.children[0].Equals(oneg))
                {
                    return (c.children[1], c.children[0]);
                }
                if (c.children[1].Equals(oneg))
                {
                    return (c.children[0], c.children[1]);
                }
            }
            return (null, null);
        }

        public dynamic __check_disj_disj_conj_rule_2()
        {
            if (this.type != NodeType.INCL_DISJUNCTION)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var child1 = this.children[i];
                var (node, conj) = child1.__get_opt_pair_disj_disj_conj_2();
                Assert.True(node == null == conj == null);
                if (node == null)
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (j == i)
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if (child2.Equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                    if (child2.type != NodeType.CONJUNCTION)
                    {
                        continue;
                    }
                    if (!(child2.__has_child(node)))
                    {
                        continue;
                    }
                    if (child2.__are_all_children_contained(conj))
                    {
                        this.children[j] = node.get_copy();
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public dynamic __get_opt_pair_disj_disj_conj_2()
        {
            if (this.type != NodeType.PRODUCT)
            {
                return (null, null);
            }
            if (this.children.Count() != 2)
            {
                return (null, null);
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return (null, null);
            }
            var disj = this.children[1];
            if (disj.type != NodeType.INCL_DISJUNCTION)
            {
                return (null, null);
            }
            if (disj.children.Count() != 2)
            {
                return (null, null);
            }
            foreach (var idx in new List<dynamic>() { 0, 1 })
            {
                var oIdx = (idx == 1) ? 0 : 1;
                var conj = disj.children[idx];
                if (conj.type != NodeType.CONJUNCTION)
                {
                    continue;
                }
                var neg = disj.children[oIdx].get_copy();
                neg.__multiply_by_minus_one();
                if (conj.__has_child(neg))
                {
                    return (neg, conj);
                }
            }
            return (null, null);
        }

        public dynamic refine_after_substitution()
        {
            var changed = false;
            foreach (var c in this.children)
            {
                if (c.refine_after_substitution())
                {
                    changed = true;
                }
            }
            if (this.__check_bitwise_in_sums_cancel_terms())
            {
                changed = true;
            }
            if (this.__check_bitwise_in_sums_replace_terms())
            {
                changed = true;
            }
            if (this.__check_disj_involving_xor_in_sums())
            {
                changed = true;
            }
            if (this.__check_xor_involving_disj())
            {
                changed = true;
            }
            if (this.__check_negative_bitw_inverse())
            {
                changed = true;
            }
            if (this.__check_xor_pairs_with_constants())
            {
                changed = true;
            }
            if (this.__check_bitw_pairs_with_constants())
            {
                changed = true;
            }
            if (this.__check_diff_bitw_pairs_with_constants())
            {
                changed = true;
            }
            if (this.__check_bitw_tuples_with_constants())
            {
                changed = true;
            }
            if (this.__check_bitw_pairs_with_inverses())
            {
                changed = true;
            }
            if (this.__check_diff_bitw_pairs_with_inverses())
            {
                changed = true;
            }
            if (this.__check_bitw_and_op_in_sum())
            {
                changed = true;
            }
            if (this.__check_insert_xor_in_sum())
            {
                changed = true;
            }
            return changed;
        }

        public dynamic __check_bitwise_in_sums_cancel_terms()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            if (this.children.Count() > MAX_CHILDREN_TO_TRANSFORM_BITW)
            {
                return false;
            }
            var changed = true;
            var i = 0;
            while (true)
            {
                if (i >= this.children.Count())
                {
                    return changed;
                }
                var child = this.children[i];
                var factor = 1;
                if (child.type == NodeType.PRODUCT)
                {
                    if ((child.children.Count() != 2 || child.children[0].type != NodeType.CONSTANT))
                    {
                        i += 1;
                        continue;
                    }
                    factor = child.children[0].constant;
                    child = child.children[1];
                }
                if ((!((new List<dynamic>() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION }).Contains(child.type)) || child.children.Count() != 2))
                {
                    i += 1;
                    continue;
                }
                var newIdx = this.__check_transform_bitwise_in_sum_cancel(i, child, factor);
                if (newIdx == null)
                {
                    i += 1;
                    continue;
                }
                Assert.True(this.children[newIdx] == child);
                if (this.children.Count() == 1)
                {
                    this.copy(this.children[0]);
                    return true;
                }
                i = newIdx + 1;
            }
            return changed;
        }

        public void __check_transform_bitwise_in_sum_cancel(dynamic idx, dynamic bitw, dynamic factor)
        {
            var withToXor = factor % 2 == 0;
            var newIdx = this.__check_transform_bitwise_in_sum_cancel_impl(false, idx, bitw, factor);
            if (newIdx != null)
            {
                return newIdx;
            }
            if (withToXor)
            {
                newIdx = this.__check_transform_bitwise_in_sum_cancel_impl(true, idx, bitw, factor / 2);
                if (newIdx != null)
                {
                    return newIdx;
                }
            }
            return null;
        }

        public void __check_transform_bitwise_in_sum_cancel_impl(dynamic toXor, dynamic idx, dynamic bitw, dynamic factor)
        {
            Assert.True(this.type == NodeType.SUM);
            Assert.True(idx < this.children.Count());
            var opSum = this.__new_node(NodeType.SUM);
            foreach (var op in bitw.children)
            {
                opSum.children.Add(op.get_copy());
            }
            opSum.__multiply(factor);
            opSum.expand();
            opSum.refine();
            var maxc = min(opSum.children.Count(), MAX_CHILDREN_SUMMED_UP);
            foreach (var i in range(1, 2 * *this.children.Count() - 1))
            {
                if (popcount(i) > maxc)
                {
                    continue;
                }
                var newIdx = this.__check_transform_bitwise_for_comb(toXor, idx, bitw, factor, opSum, i);
                if (newIdx != null)
                {
                    return newIdx;
                }
            }
            return null;
        }

        public dynamic __check_transform_bitwise_for_comb(dynamic toXor, dynamic idx, dynamic bitw, dynamic factor, dynamic opSum, dynamic combIdx)
        {
            var n = combIdx;
            var diff = opSum.get_copy();
            var indices = new List<dynamic>() { };
            foreach (var j in range(this.children.Count()))
            {
                if (j == idx)
                {
                    continue;
                }
                if (n & 1 == 1)
                {
                    indices.Add(j);
                    diff.__add(this.children[j]);
                }
                n = n >> 1;
            }
            diff.expand();
            diff.refine();
            if (diff.type != NodeType.CONSTANT)
            {
                Assert.True(this.__modulus % 2 == 0);
                var opSum = this.__new_node(NodeType.SUM);
                foreach (var op in bitw.children)
                {
                    opSum.children.Add(op.get_copy());
                }
                var hmod = this.__modulus / 2;
                opSum.__multiply(-(hmod));
                var diff2 = diff.get_copy();
                diff2.__add(opSum);
                diff2.expand();
                diff2.refine();
                if (diff2.type == NodeType.CONSTANT)
                {
                    diff = diff2;
                    factor += hmod;
                }
            }
            return this.__check_transform_bitwise_for_diff(toXor, idx, bitw, factor, diff, indices);
        }

        public void __check_transform_bitwise_for_diff(dynamic toXor, dynamic idx, dynamic bitw, dynamic factor, dynamic diff, dynamic indices)
        {
            var newIdx = this.__check_transform_bitwise_for_diff_full(toXor, idx, bitw, factor, diff, indices);
            if (newIdx != null)
            {
                return newIdx;
            }
            if (indices.Count() > 1)
            {
                newIdx = this.__check_transform_bitwise_for_diff_merge(toXor, idx, bitw, factor, diff, indices);
                if (newIdx != null)
                {
                    return newIdx;
                }
            }
            return null;
        }

        public dynamic __check_transform_bitwise_for_diff_full(dynamic toXor, dynamic idx, dynamic bitw, dynamic factor, dynamic diff, dynamic indices)
        {
            if (diff.type != NodeType.CONSTANT)
            {
                return null;
            }
            if ((!(toXor) || bitw.type == NodeType.CONJUNCTION))
            {
                var factor = -(factor);
            }
            bitw.type = bitw.__get_transformed_bitwise_type(toXor);
            if (factor - 1 % this.__modulus != 0)
            {
                bitw.__multiply(factor);
            }
            this.children[idx] = bitw;
            foreach (var j in range(indices.Count() - 1, -(1), -(1)))
            {
                this.children.RemoveAt(indices[j]);
                if (indices[j] < idx)
                {
                    idx -= 1;
                }
            }
            if (!(diff.__is_constant(0)))
            {
                if (this.children[0].type != NodeType.CONSTANT)
                {
                    idx += 1;
                }
                this.__add_constant(diff.constant);
                if (this.children[0].__is_constant(0))
                {
                    this.children.RemoveAt(0);
                    idx -= 1;
                }
            }
            return idx;
        }

        public void __check_transform_bitwise_for_diff_merge(dynamic toXor, dynamic idx, dynamic bitw, dynamic factor, dynamic diff, dynamic indices)
        {
            var constN = diff.__get_opt_const_factor();
            foreach (var i in indices)
            {
                var child = this.children[i];
                var constC = child.__get_opt_const_factor();
                if (!(diff.__equals_neglecting_constants(child, constN != null, constC != null)))
                {
                    continue;
                }
                if ((!(toXor) || bitw.type == NodeType.CONJUNCTION))
                {
                    var factor = -(factor);
                }
                bitw.type = bitw.__get_transformed_bitwise_type(toXor);
                if (factor - 1 % this.__modulus != 0)
                {
                    bitw.__multiply(factor);
                }
                this.children[idx] = bitw;
                if (constC != null)
                {
                    Assert.True(child.type == NodeType.PRODUCT);
                    Assert.True(child.children[0].type == NodeType.CONSTANT);
                    if (constN != null)
                    {
                        child.children[0].constant = constN;
                    }
                    else
                    {
                        child.children.RemoveAt(0);
                        if (child.children.Count() == 1)
                        {
                            child.copy(child.children[0]);
                        }
                    }
                }
                else
                {
                    if (constN != null)
                    {
                        child.__multiply(constN);
                    }
                }
                foreach (var j in range(indices.Count() - 1, -(1), -(1)))
                {
                    if (indices[j] != i)
                    {
                        this.children.RemoveAt(indices[j]);
                        if (indices[j] < idx)
                        {
                            idx -= 1;
                        }
                    }
                }
                return idx;
            }
            return null;
        }

        public dynamic __get_transformed_bitwise_type(dynamic toXor)
        {
            Assert.True(((new List<dynamic>() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION }).Contains(this.type)));
            if (toXor)
            {
                return NodeType.EXCL_DISJUNCTION;
            }
            if (this.type == NodeType.CONJUNCTION)
            {
                return NodeType.INCL_DISJUNCTION;
            }
            return NodeType.CONJUNCTION;
        }

        public bool __check_bitwise_in_sums_replace_terms()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            if (this.children.Count() > MAX_CHILDREN_TO_TRANSFORM_BITW)
            {
                return false;
            }
            var changed = true;
            var i = 0;
            while (true)
            {
                if (i >= this.children.Count())
                {
                    return changed;
                }
                var child = this.children[i];
                var factor = 1;
                if (child.type == NodeType.PRODUCT)
                {
                    if ((child.children.Count() != 2 || child.children[0].type != NodeType.CONSTANT))
                    {
                        i += 1;
                        continue;
                    }
                    factor = child.children[0].constant;
                    child = child.children[1];
                }
                if ((!((new List<dynamic>() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION }).Contains(child.type)) || child.children.Count() != 2))
                {
                    i += 1;
                    continue;
                }
                var newIdx = this.__check_transform_bitwise_in_sum_replace(i, child, factor);
                if (newIdx == null)
                {
                    i += 1;
                    continue;
                }
                Assert.True(this.children[newIdx] == child);
                Assert.True(this.children.Count() > 1);
                i = newIdx + 1;
            }
            return false;
        }

        public void __check_transform_bitwise_in_sum_replace(dynamic idx, dynamic bitw, dynamic factor)
        {
            var cIdx = bitw.__get_index_of_more_complex_operand();
            if (cIdx == null)
            {
                return null;
            }
            var withToXor = factor % 2 == 0;
            var newIdx = this.__check_transform_bitwise_in_sum_replace_impl(false, idx, bitw, cIdx, factor);
            if (newIdx != null)
            {
                return newIdx;
            }
            if (withToXor)
            {
                newIdx = this.__check_transform_bitwise_in_sum_replace_impl(true, idx, bitw, cIdx, factor / 2);
                if (newIdx != null)
                {
                    return newIdx;
                }
            }
            return null;
        }

        public void __get_index_of_more_complex_operand()
        {
            var idk = new List() { 0, 1, 2 };
            Assert.True(this.__is_bitwise_binop());
            Assert.True(this.children.Count() == 2);
            var (c0, c1) = (this.children[0], this.children[1]);
            c0.mark_linear();
            c1.mark_linear();
            var l0 = c0.is_linear();
            var l1 = c1.is_linear();
            if (l0 != l1)
            {
                return (l1) ? 0 : 1;
            }
            var b0 = c0.state == NodeState.BITWISE;
            var b1 = c1.state == NodeState.BITWISE;
            if (b0 != b1)
            {
                return (b1) ? 0 : 1;
            }
            else
            {
                return null;
            }
            return null;
        }

        public void __check_transform_bitwise_in_sum_replace_impl(dynamic toXor, dynamic idx, dynamic bitw, dynamic cIdx, dynamic factor)
        {
            Assert.True(this.type == NodeType.SUM);
            Assert.True(idx < this.children.Count());
            var cOp = bitw.children[cIdx].get_copy();
            cOp.__multiply(factor);
            var maxc = MAX_CHILDREN_SUMMED_UP;
            foreach (var i in range(1, 2 * *this.children.Count() - 1))
            {
                if (popcount(i) > maxc)
                {
                    continue;
                }
                var newIdx = this.__check_transform_bitwise_replace_for_comb(toXor, idx, bitw, factor, cOp, cIdx, i);
                if (newIdx != null)
                {
                    return newIdx;
                }
            }
            return null;
        }

        public dynamic __check_transform_bitwise_replace_for_comb(dynamic toXor, dynamic idx, dynamic bitw, dynamic factor, dynamic cOp, dynamic cIdx, dynamic combIdx)
        {
            var n = combIdx;
            var diff = cOp.get_copy();
            var indices = new List<dynamic>() { };
            foreach (var j in range(this.children.Count()))
            {
                if (j == idx)
                {
                    continue;
                }
                if (n & 1 == 1)
                {
                    indices.Add(j);
                    diff.__add(this.children[j]);
                }
                n = n >> 1;
            }
            diff.expand();
            diff.refine();
            return this.__check_transform_bitwise_replace_for_diff(toXor, idx, bitw, factor, diff, cIdx, indices);
        }

        public dynamic __check_transform_bitwise_replace_for_diff(dynamic toXor, dynamic idx, dynamic bitw, dynamic factor, dynamic diff, dynamic cIdx, dynamic indices)
        {
            return this.__check_transform_bitwise_replace_for_diff_full(toXor, idx, bitw, factor, diff, cIdx, indices);
        }

        public dynamic __check_transform_bitwise_replace_for_diff_full(dynamic toXor, dynamic idx, dynamic bitw, dynamic factor, dynamic diff, dynamic cIdx, dynamic indices)
        {
            if (diff.type != NodeType.CONSTANT)
            {
                return null;
            }
            var op = bitw.children[(cIdx == 1) ? 0 : 1].get_copy();
            op.__multiply(factor);
            this.children.Add(op);
            if ((!(toXor) || bitw.type == NodeType.CONJUNCTION))
            {
                var factor = -(factor);
            }
            bitw.type = bitw.__get_transformed_bitwise_type(toXor);
            if (factor - 1 % this.__modulus != 0)
            {
                bitw.__multiply(factor);
            }
            this.children[idx] = bitw;
            foreach (var j in range(indices.Count() - 1, -(1), -(1)))
            {
                this.children.RemoveAt(indices[j]);
                if (indices[j] < idx)
                {
                    idx -= 1;
                }
            }
            if (!(diff.__is_constant(0)))
            {
                if (this.children[0].type != NodeType.CONSTANT)
                {
                    idx += 1;
                }
                this.__add_constant(diff.constant);
                if (this.children[0].__is_constant(0))
                {
                    this.children.RemoveAt(0);
                    idx -= 1;
                }
            }
            return idx;
        }

        public dynamic __check_disj_involving_xor_in_sums()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            var changed = false;
            foreach (var child in this.children)
            {
                var factor = 1;
                var node = child;
                if (node.type == NodeType.PRODUCT)
                {
                    if (node.children.Count() != 2)
                    {
                        continue;
                    }
                    if (node.children[0].type != NodeType.CONSTANT)
                    {
                        continue;
                    }
                    factor = node.children[0].constant;
                    node = node.children[1];
                }
                if (node.type != NodeType.INCL_DISJUNCTION)
                {
                    continue;
                }
                if (node.children.Count() != 2)
                {
                    continue;
                }
                var xorIdx = null;
                if (node.children[0].type == NodeType.EXCL_DISJUNCTION)
                {
                    xorIdx = 0;
                }
                if (node.children[1].type == NodeType.EXCL_DISJUNCTION)
                {
                    if (xorIdx != null)
                    {
                        continue;
                    }
                    xorIdx = 1;
                }
                if (xorIdx == null)
                {
                    continue;
                }
                var oIdx = (xorIdx == 0) ? 1 : 0;
                var xor = node.children[xorIdx];
                var o = node.children[oIdx];
                if (xor.children.Count() != 2)
                {
                    continue;
                }
                if (o.Equals(xor.children[0]))
                {
                    o = this.__new_node_with_children(NodeType.CONJUNCTION, new List<dynamic>() { o.__get_shallow_copy(), xor.children[1].get_copy() });
                }
                else
                {
                    if (o.Equals(xor.children[1]))
                    {
                        o = this.__new_node_with_children(NodeType.CONJUNCTION, new List<dynamic>() { o.__get_shallow_copy(), xor.children[0].get_copy() });
                    }
                    else
                    {
                        if (o.type != NodeType.CONJUNCTION)
                        {
                            continue;
                        }
                        else
                        {
                            var found0 = false;
                            var found1 = false;
                            foreach (var ch in o.children)
                            {
                                if (ch.Equals(xor.children[0]))
                                {
                                    found0 = true;
                                }
                                else
                                {
                                    if (ch.Equals(xor.children[1]))
                                    {
                                        found1 = true;
                                    }
                                }
                                if ((found0 && found1))
                                {
                                    break;
                                }
                            }
                            if (found0)
                            {
                                if (!(found1))
                                {
                                    o.children.Add(xor.children[1].get_copy());
                                }
                            }
                            else
                            {
                                if (found1)
                                {
                                    o.children.Add(xor.children[0].get_copy());
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
                o.__inspect_constants();
                changed = true;
                if (factor == 1)
                {
                    this.children.Add(xor.__get_shallow_copy());
                }
                else
                {
                    var prod = this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { this.__new_constant_node(factor), xor.__get_shallow_copy() });
                    this.children.Add(prod);
                }
                node.copy(o);
            }
            return changed;
        }

        public bool __check_xor_involving_disj()
        {
            if (this.type != NodeType.EXCL_DISJUNCTION)
            {
                return false;
            }
            if (this.children.Count() != 2)
            {
                return false;
            }
            foreach (var disjIdx in new List<dynamic>() { 0, 1 })
            {
                var disj = this.children[disjIdx];
                if (disj.type != NodeType.INCL_DISJUNCTION)
                {
                    continue;
                }
                var oIdx = (disjIdx == 0) ? 1 : 0;
                var other = this.children[oIdx];
                var idx = disj.__get_index_of_child(other);
                if (idx == null)
                {
                    continue;
                }
                other.__negate();
                this.type = NodeType.CONJUNCTION;
                disj.children.RemoveAt(idx);
                if (disj.children.Count() == 1)
                {
                    this.children[disjIdx] = disj.children[0];
                }
                return true;
            }
            return false;
        }

        public bool __check_negative_bitw_inverse()
        {
            if (this.type != NodeType.PRODUCT)
            {
                return false;
            }
            if (this.children.Count() != 2)
            {
                return false;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return false;
            }
            var node = this.children[1];
            if (!((new List<dynamic>() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION }).Contains(node.type)))
            {
                return false;
            }
            if (node.children.Count() != 2)
            {
                return false;
            }
            if (node.children[0].type == NodeType.CONSTANT)
            {
                return false;
            }
            var inv = node.children[0].get_copy();
            inv.__multiply_by_minus_one();
            if (!(inv.Equals(node.children[1])))
            {
                return false;
            }
            if (node.type == NodeType.CONJUNCTION)
            {
                node.type = NodeType.INCL_DISJUNCTION;
            }
            else
            {
                node.type = NodeType.CONJUNCTION;
            }
            this.copy(node);
            return true;
        }

        public bool __check_xor_pairs_with_constants()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            var l = this.__collect_indices_of_bitw_with_constants_in_sum(NodeType.EXCL_DISJUNCTION);
            if (l.Count() == 0)
            {
                return false;
            }
            var toRemove = new List<dynamic>() { };
            var const = 0;
            foreach (var pair in l)
            {
                var factor = pair[0];
                var sublist = pair[1];
                var done = sublist.Where(x => ).Select(x => false);
                foreach (var i in range(sublist.Count() - 1, 0, -(1)))
                {
                    if (done[i])
                    {
                        continue;
                    }
                    foreach (var j in range(0, i))
                    {
                        if (done[j])
                        {
                            continue;
                        }
                        var firstIdx = sublist[i];
                        var first = this.children[firstIdx];
                        if (first.type == NodeType.PRODUCT)
                        {
                            first = first.children[1];
                        }
                        Assert.True(first.type == NodeType.EXCL_DISJUNCTION);
                        var secIdx = sublist[j];
                        var second = this.children[secIdx];
                        if (second.type == NodeType.PRODUCT)
                        {
                            second = second.children[1];
                        }
                        Assert.True(second.type == NodeType.EXCL_DISJUNCTION);
                        var firstConst = first.children[0].constant % this.__modulus;
                        var secConst = second.children[0].constant % this.__modulus;
                        if (firstConst & secConst != 0)
                        {
                            continue;
                        }
                        var (_, remove, add) = this.__merge_bitwise_terms(firstIdx, secIdx, first, second, factor, first.children[0].constant, second.children[0].constant);
                        const += add;
                        done[j] = true;
                        Assert.True(remove);
                        if (remove)
                        {
                            toRemove.Add(secIdx);
                        }
                    }
                }
            }
            if (toRemove.Count() == 0)
            {
                return false;
            }
            toRemove.sort();
            foreach (var idx in reversed(toRemove))
            {
                this.children.RemoveAt(idx);
            }
            if (this.children[0].type == NodeType.CONSTANT)
            {
                this.children[0].__set_and_reduce_constant(this.children[0].constant + const);
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(const));
            }
            if ((this.children.Count() > 1 && this.children[0].__is_constant(0)))
            {
                this.children.RemoveAt(0);
            }
            return true;
        }

        public dynamic __collect_indices_of_bitw_with_constants_in_sum(dynamic expType)
        {
            Assert.True(this.type == NodeType.SUM);
            var l = new List<dynamic>() { };
            foreach (var i in range(this.children.Count()))
            {
                var (factor, node) = this.children[i].__get_factor_of_bitw_with_constant(expType);
                Assert.True(factor == null == node == null);
                if (factor == null)
                {
                    continue;
                }
                var found = false;
                foreach (var pair in l)
                {
                    if (factor != pair[0])
                    {
                        continue;
                    }
                    var sublist = pair[1];
                    var firstIdx = sublist[0];
                    var first = this.children[firstIdx];
                    if (first.type == NodeType.PRODUCT)
                    {
                        first = first.children[1];
                    }
                    Assert.True(first.type == expType);
                    if (node.children.Count() != first.children.Count())
                    {
                        continue;
                    }
                    if (!(do_children_match(node.children.Slice(1, null, null), first.children.Slice(1, null, null))))
                    {
                        continue;
                    }
                    sublist.Add(i);
                    found = true;
                    break;
                }
                if (!(found))
                {
                    l.Add(new List<dynamic>() { factor, new List<dynamic>() { i } });
                }
            }
            return l;
        }

        public dynamic __get_factor_of_bitw_with_constant(void expType = null)
        {
            var factor = null;
            var node = null;
            if (this.__is_bitwise_binop())
            {
                if ((expType != null && this.type != expType))
                {
                    return (null, null);
                }
                factor = 1;
                node = self;
            }
            else
            {
                if (this.type != NodeType.PRODUCT)
                {
                    return (null, null);
                }
                else
                {
                    if (this.children.Count() != 2)
                    {
                        return (null, null);
                    }
                    else
                    {
                        if (this.children[0].type != NodeType.CONSTANT)
                        {
                            return (null, null);
                        }
                        else
                        {
                            if (!(this.children[1].__is_bitwise_binop()))
                            {
                                return (null, null);
                            }
                            else
                            {
                                if ((expType != null && this.children[1].type != expType))
                                {
                                    return (null, null);
                                }
                                else
                                {
                                    factor = this.children[0].constant;
                                    node = this.children[1];
                                }
                            }
                        }
                    }
                }
            }
            if (node.children[0].type != NodeType.CONSTANT)
            {
                return (null, null);
            }
            return (factor, node);
        }

        public dynamic __check_bitw_pairs_with_constants()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            var changed = false;
            foreach (var conj in new List<dynamic>() { true, false })
            {
                if (this.__check_bitw_pairs_with_constants_impl(conj))
                {
                    changed = true;
                    if (this.type != NodeType.SUM)
                    {
                        return true;
                    }
                }
            }
            return changed;
        }

        public bool __check_bitw_pairs_with_constants_impl(dynamic conj)
        {
            Assert.True(this.type == NodeType.SUM);
            var expType = (conj) ? NodeType.CONJUNCTION : NodeType.INCL_DISJUNCTION;
            var l = this.__collect_indices_of_bitw_with_constants_in_sum(expType);
            if (l.Count() == 0)
            {
                return false;
            }
            var toRemove = new List<dynamic>() { };
            var changed = false;
            foreach (var pair in l)
            {
                var factor = pair[0];
                var sublist = pair[1];
                foreach (var i in range(sublist.Count() - 1, 0, -(1)))
                {
                    foreach (var j in range(0, i))
                    {
                        var firstIdx = sublist[j];
                        var first = this.children[firstIdx];
                        if (first.type == NodeType.PRODUCT)
                        {
                            first = first.children[1];
                        }
                        Assert.True(first.type == expType);
                        var secIdx = sublist[i];
                        var second = this.children[secIdx];
                        if (second.type == NodeType.PRODUCT)
                        {
                            second = second.children[1];
                        }
                        Assert.True(second.type == expType);
                        var firstConst = first.children[0].constant % this.__modulus;
                        var secConst = second.children[0].constant % this.__modulus;
                        if (firstConst & secConst != 0)
                        {
                            continue;
                        }
                        var (_, remove, _) = this.__merge_bitwise_terms(firstIdx, secIdx, first, second, factor, firstConst, secConst);
                        if (remove)
                        {
                            toRemove.Add(secIdx);
                        }
                        changed = true;
                        break;
                    }
                }
            }
            if (!(changed))
            {
                return false;
            }
            toRemove.sort();
            foreach (var idx in reversed(toRemove))
            {
                this.children.RemoveAt(idx);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public bool __check_diff_bitw_pairs_with_constants()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            var l = this.__collect_all_indices_of_bitw_with_constants();
            if (l.Count() == 0)
            {
                return false;
            }
            var toRemove = new List<dynamic>() { };
            var const = 0;
            var changed = false;
            foreach (var sublist in l)
            {
                foreach (var i in range(sublist.Count() - 1, 0, -(1)))
                {
                    foreach (var j in range(0, i))
                    {
                        var (firstFactor, firstIdx) = sublist[j];
                        var first = this.children[firstIdx];
                        if (first.type == NodeType.PRODUCT)
                        {
                            first = first.children[1];
                        }
                        var (secFactor, secIdx) = sublist[i];
                        var second = this.children[secIdx];
                        if (second.type == NodeType.PRODUCT)
                        {
                            second = second.children[1];
                        }
                        if (first.type == second.type)
                        {
                            continue;
                        }
                        var firstConst = first.children[0].constant % this.__modulus;
                        var secConst = second.children[0].constant % this.__modulus;
                        if (firstConst & secConst != 0)
                        {
                            continue;
                        }
                        var factor = this.__get_factor_for_merging_bitwise(firstFactor, secFactor, first.type, second.type);
                        if (factor == null)
                        {
                            continue;
                        }
                        var (bitwFactor, remove, add) = this.__merge_bitwise_terms(firstIdx, secIdx, first, second, factor, firstConst, secConst);
                        const += add;
                        if (remove)
                        {
                            toRemove.Add(secIdx);
                        }
                        sublist[j][0] = bitwFactor;
                        changed = true;
                        break;
                    }
                }
            }
            if (!(changed))
            {
                return false;
            }
            toRemove.sort();
            foreach (var idx in reversed(toRemove))
            {
                this.children.RemoveAt(idx);
            }
            if (this.children[0].type == NodeType.CONSTANT)
            {
                this.children[0].__set_and_reduce_constant(this.children[0].constant + const);
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(const));
            }
            if ((this.children.Count() > 1 && this.children[0].__is_constant(0)))
            {
                this.children.RemoveAt(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public dynamic __collect_all_indices_of_bitw_with_constants()
        {
            Assert.True(this.type == NodeType.SUM);
            var l = new List<dynamic>() { };
            foreach (var i in range(this.children.Count()))
            {
                var (factor, node) = this.children[i].__get_factor_of_bitw_with_constant();
                Assert.True(factor == null == node == null);
                if (factor == null)
                {
                    continue;
                }
                Assert.True(node.__is_bitwise_binop());
                var found = false;
                foreach (var sublist in l)
                {
                    var firstIdx = sublist[0];
                    var first = this.children[firstIdx[1]];
                    if (first.type == NodeType.PRODUCT)
                    {
                        first = first.children[1];
                    }
                    if (node.children.Count() == 2)
                    {
                        if (first.children.Count() == 2)
                        {
                            if (!(node.children[1].Equals(first.children[1])))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (node.children[1].type != first.type)
                            {
                                continue;
                            }
                            Assert.True(first.children.Count() > 2);
                            if (!(do_children_match(node.children[1].children, first.children.Slice(1, null, null))))
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (first.children.Count() == 2)
                        {
                            if (first.children[1].type != node.type)
                            {
                                continue;
                            }
                            Assert.True(node.children.Count() > 2);
                            if (!(do_children_match(first.children[1].children, node.children.Slice(1, null, null))))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!(do_children_match(first.children.Slice(1, null, null), node.children.Slice(1, null, null))))
                            {
                                continue;
                            }
                        }
                    }
                    sublist.Add(new List<dynamic>() { factor, i });
                    found = true;
                    break;
                }
                if (!(found))
                {
                    l.Add(new List<dynamic>() { new List<dynamic>() { factor, i } });
                }
            }
            return l;
        }

        public dynamic __get_factor_for_merging_bitwise(dynamic fac1, dynamic fac2, dynamic type1, dynamic type2)
        {
            if (type1 == type2)
            {
                if (fac1 - fac2 % this.__modulus != 0)
                {
                    return null;
                }
                return fac1;
            }
            if (type1 == NodeType.EXCL_DISJUNCTION)
            {
                if (type2 == NodeType.CONJUNCTION)
                {
                    if (2 * fac1 + fac2 % this.__modulus != 0)
                    {
                        return null;
                    }
                }
                else
                {
                    if (2 * fac1 - fac2 % this.__modulus != 0)
                    {
                        return null;
                    }
                }
                return fac1;
            }
            if (type1 == NodeType.CONJUNCTION)
            {
                if (type2 == NodeType.EXCL_DISJUNCTION)
                {
                    if (fac1 + 2 * fac2 % this.__modulus != 0)
                    {
                        return null;
                    }
                }
                else
                {
                    if (fac1 + fac2 % this.__modulus != 0)
                    {
                        return null;
                    }
                }
                return fac2;
            }
            if (type2 == NodeType.EXCL_DISJUNCTION)
            {
                if (-(fac1) + 2 * fac2 % this.__modulus != 0)
                {
                    return null;
                }
                return fac2;
            }
            if (fac1 + fac2 % this.__modulus != 0)
            {
                return null;
            }
            return fac1;
        }

        public dynamic __merge_bitwise_terms(dynamic firstIdx, dynamic secIdx, dynamic first, dynamic second, dynamic factor, dynamic firstConst, dynamic secConst)
        {
            var (bitwFactor, add, opfac) = this.__merge_bitwise_terms_and_get_opfactor(firstIdx, secIdx, first, second, factor, firstConst, secConst);
            if (opfac == 0)
            {
                return (bitwFactor, true, add);
            }
            if (second.children.Count() == 2)
            {
                this.children[secIdx] = second.children[1];
            }
            else
            {
                this.children[secIdx] = second.__get_shallow_copy();
                this.children[secIdx].children.RemoveAt(0);
            }
            if (opfac != 1)
            {
                this.children[secIdx].__multiply(opfac);
            }
            return (bitwFactor, false, add);
        }

        public dynamic __merge_bitwise_terms_and_get_opfactor(dynamic firstIdx, dynamic secIdx, dynamic first, dynamic second, dynamic factor, dynamic firstConst, dynamic secConst)
        {
            var constSum = firstConst + secConst;
            var constNeg = -(constSum) - 1;
            var bitwFactor = this.__get_bitwise_factor_for_merging_bitwise(factor, first.type, second.type);
            var (opfac, add) = this.__get_operand_factor_and_constant_for_merging_bitwise(factor, first.type, second.type, firstConst, secConst);
            var hasFactor = this.children[firstIdx].type == NodeType.PRODUCT;
            first.children[0].__set_and_reduce_constant(this.__get_const_operand_for_merging_bitwise(constSum, first.type, second.type));
            if ((first.type != second.type || first.type != NodeType.INCL_DISJUNCTION))
            {
                if ((first.type != NodeType.CONJUNCTION && first.children.Count() > 2))
                {
                    var node = this.__new_node(first.type);
                    node.children.Add(first.children[0].__get_shallow_copy());
                    first.children.RemoveAt(0);
                    node.children.Add(first.__get_shallow_copy());
                    first.copy(node);
                }
                first.type = NodeType.CONJUNCTION;
            }
            if (hasFactor)
            {
                if (bitwFactor == 1)
                {
                    this.children[firstIdx] = first;
                }
                else
                {
                    this.children[firstIdx].children[0].__set_and_reduce_constant(bitwFactor);
                }
            }
            else
            {
                if (bitwFactor != 1)
                {
                    var factorNode = this.__new_constant_node(bitwFactor);
                    var prod = this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { factorNode, first.__get_shallow_copy() });
                    this.children[firstIdx].copy(prod);
                }
            }
            return (bitwFactor, add, opfac);
        }

        public dynamic __get_const_operand_for_merging_bitwise(dynamic constSum, dynamic type1, dynamic type2)
        {
            if ((type1 == type2 && type1 != NodeType.EXCL_DISJUNCTION))
            {
                return constSum;
            }
            return -(constSum) - 1;
        }

        public dynamic __get_bitwise_factor_for_merging_bitwise(dynamic factor, dynamic type1, dynamic type2)
        {
            if ((type1 == NodeType.EXCL_DISJUNCTION || type2 == NodeType.EXCL_DISJUNCTION))
            {
                return 2 * factor;
            }
            return factor;
        }

        public dynamic __get_operand_factor_and_constant_for_merging_bitwise(dynamic factor, dynamic type1, dynamic type2, dynamic const1, dynamic const2)
        {
            if (type1 == type2)
            {
                if (type1 == NodeType.CONJUNCTION)
                {
                    return (0, 0);
                }
                if (type1 == NodeType.INCL_DISJUNCTION)
                {
                    return (factor, 0);
                }
                return (0, const1 + const2 * factor);
            }
            if (type1 == NodeType.EXCL_DISJUNCTION)
            {
                if (type2 == NodeType.CONJUNCTION)
                {
                    return (-(factor), const1 * factor);
                }
                return (factor, const1 + 2 * const2 * factor);
            }
            if (type1 == NodeType.CONJUNCTION)
            {
                if (type2 == NodeType.EXCL_DISJUNCTION)
                {
                    return (-(factor), const2 * factor);
                }
                return (0, const2 * factor);
            }
            if (type2 == NodeType.EXCL_DISJUNCTION)
            {
                return (factor, 2 * const1 + const2 * factor);
            }
            return (0, const1 * factor);
        }

        public bool __check_bitw_tuples_with_constants()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            var l = this.__collect_all_indices_of_bitw_with_constants();
            if (l.Count() == 0)
            {
                return false;
            }
            var toRemove = new List<dynamic>() { };
            var const = 0;
            var changed = false;
            foreach (var sublist in l)
            {
                foreach (var i in range(sublist.Count() - 1, 1, -(1)))
                {
                    var add = this.__try_merge_bitwise_with_constants_with_2_others(sublist, i, toRemove);
                    if (add != null)
                    {
                        changed = true;
                        const += add;
                    }
                }
            }
            if (!(changed))
            {
                return false;
            }
            toRemove.sort();
            foreach (var idx in reversed(toRemove))
            {
                this.children.RemoveAt(idx);
            }
            if (this.children[0].type == NodeType.CONSTANT)
            {
                this.children[0].__set_and_reduce_constant(this.children[0].constant + const);
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(const));
            }
            if ((this.children.Count() > 1 && this.children[0].__is_constant(0)))
            {
                this.children.RemoveAt(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public void __try_merge_bitwise_with_constants_with_2_others(dynamic sublist, dynamic i, dynamic toRemove)
        {
            foreach (var j in range(1, i))
            {
                foreach (var k in range(0, j))
                {
                    var add = this.__try_merge_triple_bitwise_with_constants(sublist, i, j, k, toRemove);
                    if (add != null)
                    {
                        return add;
                    }
                }
            }
            return null;
        }

        public void __try_merge_triple_bitwise_with_constants(dynamic sublist, dynamic i, dynamic j, dynamic k, dynamic toRemove)
        {
            foreach (var perm in new List<dynamic>() { new List<dynamic>() { i, j, k }, new List<dynamic>() { j, i, k }, new List<dynamic>() { k, i, j } })
            {
                var (mainFactor, mainIdx) = sublist[perm[0]];
                var main = this.children[mainIdx];
                if (main.type == NodeType.PRODUCT)
                {
                    main = main.children[1];
                }
                var mainConst = main.children[0].constant % this.__modulus;
                var (firstFactor, firstIdx) = sublist[perm[1]];
                var first = this.children[firstIdx];
                if (first.type == NodeType.PRODUCT)
                {
                    first = first.children[1];
                }
                var firstConst = first.children[0].constant % this.__modulus;
                var (secFactor, secIdx) = sublist[perm[2]];
                var second = this.children[secIdx];
                if (second.type == NodeType.PRODUCT)
                {
                    second = second.children[1];
                }
                var secConst = second.children[0].constant % this.__modulus;
                var (factor1, factor2) = this.__get_factors_for_merging_triple(first.type, second.type, main.type, firstFactor, secFactor, mainFactor, firstConst, secConst, mainConst);
                Assert.True(factor1 == null == factor2 == null);
                if (factor1 == null)
                {
                    continue;
                }
                var i1 = perm[1];
                if (perm[0] != i)
                {
                    Assert.True(perm[1] == i);
                    var(sublist[perm[0]], sublist[perm[1]]) = (sublist[perm[1]], sublist[perm[0]]);
                    i1 = perm[0];
                }
                var (bitwFactor1, add1, opfac1) = this.__merge_bitwise_terms_and_get_opfactor(firstIdx, mainIdx, first, main, factor1, firstConst, mainConst);
                var (bitwFactor2, add2, opfac2) = this.__merge_bitwise_terms_and_get_opfactor(secIdx, mainIdx, second, main, factor2, secConst, mainConst);
                var opfac = opfac1 + opfac2 % this.__modulus;
                if (opfac == null)
                {
                    toRemove.Add(mainIdx);
                }
                else
                {
                    this.children[mainIdx] = main.children[1];
                    if (opfac != 1)
                    {
                        this.children[mainIdx].__multiply(opfac);
                    }
                }
                sublist[i1][0] = bitwFactor1;
                sublist[perm[2]][0] = bitwFactor2;
                return add1 + add2;
            }
            return null;
        }

        public dynamic __get_factors_for_merging_triple(dynamic type1, dynamic type2, dynamic type0, dynamic fac1, dynamic fac2, dynamic fac0, dynamic const1, dynamic const2, dynamic const0)
        {
            if (const1 & const0 != 0)
            {
                return (null, null);
            }
            if (const2 & const0 != 0)
            {
                return (null, null);
            }
            var factor1 = this.__get_possible_factor_for_merging_bitwise(fac1, type1, type0);
            if (factor1 == null)
            {
                return (null, null);
            }
            var factor2 = this.__get_possible_factor_for_merging_bitwise(fac2, type2, type0);
            if (factor2 == null)
            {
                return (null, null);
            }
            if (factor1 + factor2 - fac0 % this.__modulus != 0)
            {
                return (null, null);
            }
            factor1 = this.__get_factor_for_merging_bitwise(fac1, factor1, type1, type0);
            factor2 = this.__get_factor_for_merging_bitwise(fac2, factor2, type2, type0);
            Assert.True(factor1 != null);
            Assert.True(factor2 != null);
            return (factor1, factor2);
        }

        public dynamic __get_possible_factor_for_merging_bitwise(dynamic fac1, dynamic type1, dynamic type0)
        {
            if (type1 == NodeType.EXCL_DISJUNCTION)
            {
                if (type0 == NodeType.CONJUNCTION)
                {
                    return -(2) * fac1;
                }
                if (type0 == NodeType.INCL_DISJUNCTION)
                {
                    return 2 * fac1;
                }
                return fac1;
            }
            if (type1 == NodeType.CONJUNCTION)
            {
                if (type0 == NodeType.EXCL_DISJUNCTION)
                {
                    if (fac1 % 2 != 0)
                    {
                        return null;
                    }
                    return -(fac1) / 2;
                }
                if (type0 == NodeType.CONJUNCTION)
                {
                    return fac1;
                }
                return -(fac1);
            }
            if (type0 == NodeType.EXCL_DISJUNCTION)
            {
                if (fac1 % 2 != 0)
                {
                    return null;
                }
                return fac1 / 2;
            }
            if (type0 == NodeType.CONJUNCTION)
            {
                return -(fac1);
            }
            return fac1;
        }

        public dynamic __check_bitw_pairs_with_inverses()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            var changed = false;
            foreach (var expType in new List<dynamic>() { NodeType.CONJUNCTION, NodeType.EXCL_DISJUNCTION, NodeType.INCL_DISJUNCTION })
            {
                if (this.__check_bitw_pairs_with_inverses_impl(expType))
                {
                    changed = true;
                    if (this.type != NodeType.SUM)
                    {
                        return true;
                    }
                }
            }
            return changed;
        }

        public bool __check_bitw_pairs_with_inverses_impl(dynamic expType)
        {
            Assert.True(this.type == NodeType.SUM);
            var l = this.__collect_indices_of_bitw_without_constants_in_sum(expType);
            if (l.Count() == 0)
            {
                return false;
            }
            var toRemove = new List<dynamic>() { };
            var changed = false;
            var const = 0;
            foreach (var pair in l)
            {
                var factor = pair[0];
                var sublist = pair[2];
                var done = sublist.Where(x => ).Select(x => false);
                foreach (var i in range(sublist.Count() - 1, 0, -(1)))
                {
                    if (done[i])
                    {
                        continue;
                    }
                    foreach (var j in range(0, i))
                    {
                        if (done[j])
                        {
                            continue;
                        }
                        var firstIdx = sublist[j];
                        var first = this.children[firstIdx];
                        if (first.type == NodeType.PRODUCT)
                        {
                            first = first.children[1];
                        }
                        Assert.True(first.type == expType);
                        var secIdx = sublist[i];
                        var second = this.children[secIdx];
                        if (second.type == NodeType.PRODUCT)
                        {
                            second = second.children[1];
                        }
                        Assert.True(second.type == expType);
                        var indices = first.__get_only_differing_child_indices(second);
                        if (indices == null)
                        {
                            continue;
                        }
                        if (!(first.children[indices[0]].equals_negated(second.children[indices[1]])))
                        {
                            continue;
                        }
                        var (removeFirst, removeSec, add) = this.__merge_inverse_bitwise_terms(firstIdx, secIdx, first, second, factor, indices);
                        const += add;
                        if (removeFirst)
                        {
                            toRemove.Add(firstIdx);
                        }
                        if (removeSec)
                        {
                            toRemove.Add(secIdx);
                        }
                        done[j] = true;
                        changed = true;
                        break;
                    }
                }
            }
            if (!(changed))
            {
                return false;
            }
            if (toRemove.Count() > 0)
            {
                toRemove.sort();
                foreach (var idx in reversed(toRemove))
                {
                    this.children.RemoveAt(idx);
                }
            }
            if (this.children.Count() == 0)
            {
                this.copy(this.__new_constant_node(const));
                return true;
            }
            if (this.children[0].type == NodeType.CONSTANT)
            {
                this.children[0].__set_and_reduce_constant(this.children[0].constant + const);
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(const));
            }
            if ((this.children.Count() > 1 && this.children[0].__is_constant(0)))
            {
                this.children.RemoveAt(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public dynamic __collect_indices_of_bitw_without_constants_in_sum(dynamic expType)
        {
            Assert.True(this.type == NodeType.SUM);
            var l = new List<dynamic>() { };
            foreach (var i in range(this.children.Count()))
            {
                var (factor, node) = this.children[i].__get_factor_of_bitw_without_constant(expType);
                Assert.True(factor == null == node == null);
                if (factor == null)
                {
                    continue;
                }
                var opCnt = node.children.Count();
                var found = false;
                foreach (var triple in l)
                {
                    if (factor != triple[0])
                    {
                        continue;
                    }
                    if (opCnt != triple[1])
                    {
                        continue;
                    }
                    triple[2].Add(i);
                    found = true;
                    break;
                }
                if (!(found))
                {
                    l.Add(new List<dynamic>() { factor, opCnt, new List<dynamic>() { i } });
                }
            }
            return l;
        }

        public dynamic __get_factor_of_bitw_without_constant(void expType = null)
        {
            var factor = null;
            var node = null;
            if (this.__is_bitwise_binop())
            {
                if ((expType != null && this.type != expType))
                {
                    return (null, null);
                }
                factor = 1;
                node = self;
            }
            else
            {
                if (this.type != NodeType.PRODUCT)
                {
                    return (null, null);
                }
                else
                {
                    if (this.children.Count() != 2)
                    {
                        return (null, null);
                    }
                    else
                    {
                        if (this.children[0].type != NodeType.CONSTANT)
                        {
                            return (null, null);
                        }
                        else
                        {
                            if (!(this.children[1].__is_bitwise_binop()))
                            {
                                return (null, null);
                            }
                            else
                            {
                                if ((expType != null && this.children[1].type != expType))
                                {
                                    return (null, null);
                                }
                                else
                                {
                                    factor = this.children[0].constant;
                                    node = this.children[1];
                                }
                            }
                        }
                    }
                }
            }
            if (node.children[0].type == NodeType.CONSTANT)
            {
                return (null, null);
            }
            return (factor, node);
        }

        public dynamic __get_only_differing_child_indices(dynamic other)
        {
            if (this.type == other.type)
            {
                if (this.children.Count() != other.children.Count())
                {
                    return null;
                }
                return this.__get_only_differing_child_indices_same_len(other);
            }
            if (this.children.Count() == other.children.Count())
            {
                if (this.children.Count() != 2)
                {
                    return null;
                }
                return this.__get_only_differing_child_indices_same_len(other);
            }
            if (this.children.Count() < other.children.Count())
            {
                if (this.children.Count() != 2)
                {
                    return null;
                }
                return this.__get_only_differing_child_indices_diff_len(other);
            }
            if (other.children.Count() != 2)
            {
                return null;
            }
            var indices = other.__get_only_differing_child_indices_diff_len(self);
            if (indices == null)
            {
                return null;
            }
            return (indices[1], indices[0]);
        }

        public dynamic __get_only_differing_child_indices_same_len(dynamic other)
        {
            Assert.True((this.type == other.type || this.children.Count() == 2));
            Assert.True(this.children.Count() == other.children.Count());
            var idx1 = null;
            var oIndices = list(range(other.children.Count()));
            foreach (var i in range(this.children.Count()))
            {
                var child = this.children[i];
                var found = false;
                foreach (var j in oIndices)
                {
                    if (child.Equals(other.children[j]))
                    {
                        oIndices.remove(j);
                        found = true;
                    }
                }
                if (!(found))
                {
                    if (idx1 == null)
                    {
                        idx1 = i;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            if (idx1 == null)
            {
                return null;
            }
            Assert.True(oIndices.Count() == 1);
            return (idx1, oIndices[0]);
        }

        public void __get_only_differing_child_indices_diff_len(dynamic other)
        {
            Assert.True(this.type != other.type);
            Assert.True(this.children.Count() == 2);
            Assert.True(other.children.Count() > 2);
            foreach (var i in new List<dynamic>() { 0, 1 })
            {
                var idx = other.__get_index_of_child_negated(this.children[i]);
                if (idx == null)
                {
                    continue;
                }
                var oi = (i == 0) ? 1 : 0;
                if (this.children[oi].type != other.type)
                {
                    continue;
                }
                if (!(do_children_match(this.children[oi].children, other.children.Slice(null, idx, null) + other.children.Slice(idx + 1, null, null))))
                {
                    continue;
                }
                return (i, idx);
            }
            return null;
        }

        public dynamic __merge_inverse_bitwise_terms(dynamic firstIdx, dynamic secIdx, dynamic first, dynamic second, dynamic factor, dynamic indices)
        {
            var type1 = first.type;
            var type2 = second.type;
            var (invOpFac, sameOpFac, add) = this.__get_operand_factors_and_constant_for_merging_inverse_bitwise(factor, type1, type2);
            var removeFirst = sameOpFac == 0;
            if (!(removeFirst))
            {
                var hasFactor = this.children[firstIdx].type == NodeType.PRODUCT;
                if (first.children.Count() == 2)
                {
                    Assert.True(((new List<dynamic>() { 0, 1 }).Contains(indices[0])));
                    var oIdx = (indices[0] == 1) ? 0 : 1;
                    first.copy(first.children[oIdx]);
                }
                else
                {
                    first.children.RemoveAt(indices[0]);
                }
                if (hasFactor)
                {
                    if (sameOpFac == 1)
                    {
                        this.children[firstIdx] = first;
                    }
                    else
                    {
                        this.children[firstIdx].children[0].__set_and_reduce_constant(sameOpFac);
                    }
                }
                else
                {
                    if (sameOpFac != 1)
                    {
                        var factorNode = this.__new_constant_node(sameOpFac);
                        var prod = this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { factorNode, first.__get_shallow_copy() });
                        this.children[firstIdx].copy(prod);
                    }
                }
                this.children[firstIdx].__flatten();
            }
            var removeSecond = invOpFac == 0;
            if (!(removeSecond))
            {
                hasFactor = this.children[secIdx].type == NodeType.PRODUCT;
                second.copy(second.children[indices[1]]);
                if (this.__must_invert_at_merging_inverse_bitwise(type1, type2))
                {
                    second.__negate();
                }
                if (hasFactor)
                {
                    if (invOpFac == 1)
                    {
                        this.children[secIdx] = second;
                    }
                    else
                    {
                        this.children[secIdx].children[0].__set_and_reduce_constant(invOpFac);
                    }
                }
                else
                {
                    if (invOpFac != 1)
                    {
                        factorNode = this.__new_constant_node(invOpFac);
                        prod = this.__new_node_with_children(NodeType.PRODUCT, new List<dynamic>() { factorNode, second.__get_shallow_copy() });
                        this.children[secIdx].copy(prod);
                    }
                }
                this.children[secIdx].__flatten();
            }
            return (removeFirst, removeSecond, add);
        }

        public dynamic __get_operand_factors_and_constant_for_merging_inverse_bitwise(dynamic factor, dynamic type1, dynamic type2)
        {
            if (type1 == type2)
            {
                if (type1 == NodeType.CONJUNCTION)
                {
                    return (0, factor, 0);
                }
                if (type1 == NodeType.INCL_DISJUNCTION)
                {
                    return (0, factor, -(factor));
                }
                return (0, 0, -(factor));
            }
            if (type1 == NodeType.EXCL_DISJUNCTION)
            {
                if (type2 == NodeType.CONJUNCTION)
                {
                    return (factor, -(factor), 0);
                }
                return (-(factor), factor, -(2) * factor);
            }
            if (type1 == NodeType.CONJUNCTION)
            {
                if (type2 == NodeType.EXCL_DISJUNCTION)
                {
                    return (factor, -(factor), 0);
                }
                return (factor, 0, 0);
            }
            if (type2 == NodeType.EXCL_DISJUNCTION)
            {
                return (-(factor), factor, -(2) * factor);
            }
            return (factor, 0, 0);
        }

        public bool __must_invert_at_merging_inverse_bitwise(dynamic type1, dynamic type2)
        {
            Assert.True(type1 != type2);
            if (type1 == NodeType.EXCL_DISJUNCTION)
            {
                return true;
            }
            if (type2 == NodeType.EXCL_DISJUNCTION)
            {
                return false;
            }
            return type1 == NodeType.INCL_DISJUNCTION;
        }

        public bool __check_diff_bitw_pairs_with_inverses()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            var l = this.__collect_all_indices_of_bitw_without_constants();
            if (l.Count() == 0)
            {
                return false;
            }
            var toRemove = new List<dynamic>() { };
            var done = l.Where(x => ).Select(x => false);
            var changed = false;
            var const = 0;
            foreach (var i in range(l.Count() - 1, 0, -(1)))
            {
                if (done[i])
                {
                    continue;
                }
                foreach (var j in range(0, i))
                {
                    if (done[j])
                    {
                        continue;
                    }
                    var (firstFactor, firstIdx) = l[j];
                    var first = this.children[firstIdx];
                    if (first.type == NodeType.PRODUCT)
                    {
                        first = first.children[1];
                    }
                    var (secFactor, secIdx) = l[i];
                    var second = this.children[secIdx];
                    if (second.type == NodeType.PRODUCT)
                    {
                        second = second.children[1];
                    }
                    if (first.type == second.type)
                    {
                        continue;
                    }
                    var factor = this.__get_factor_for_merging_bitwise(firstFactor, secFactor, first.type, second.type);
                    if (factor == null)
                    {
                        continue;
                    }
                    var indices = first.__get_only_differing_child_indices(second);
                    if (indices == null)
                    {
                        continue;
                    }
                    if (!(first.children[indices[0]].equals_negated(second.children[indices[1]])))
                    {
                        continue;
                    }
                    var (removeFirst, removeSec, add) = this.__merge_inverse_bitwise_terms(firstIdx, secIdx, first, second, factor, indices);
                    const += add;
                    if (removeFirst)
                    {
                        toRemove.Add(firstIdx);
                    }
                    if (removeSec)
                    {
                        toRemove.Add(secIdx);
                    }
                    done[j] = true;
                    changed = true;
                    break;
                }
            }
            if (!(changed))
            {
                return false;
            }
            if (toRemove.Count() > 0)
            {
                toRemove.sort();
                foreach (var idx in reversed(toRemove))
                {
                    this.children.RemoveAt(idx);
                }
            }
            if (this.children.Count() == 0)
            {
                this.copy(this.__new_constant_node(const));
                return true;
            }
            if (this.children[0].type == NodeType.CONSTANT)
            {
                this.children[0].__set_and_reduce_constant(this.children[0].constant + const);
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(const));
            }
            if ((this.children.Count() > 1 && this.children[0].__is_constant(0)))
            {
                this.children.RemoveAt(0);
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public dynamic __collect_all_indices_of_bitw_without_constants()
        {
            Assert.True(this.type == NodeType.SUM);
            var l = new List<dynamic>() { };
            foreach (var i in range(this.children.Count()))
            {
                var (factor, node) = this.children[i].__get_factor_of_bitw_without_constant();
                Assert.True(factor == null == node == null);
                if (factor == null)
                {
                    continue;
                }
                l.Add(new List<dynamic>() { factor, i });
            }
            return l;
        }

        public bool __check_bitw_and_op_in_sum()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            foreach (var bitwIdx in range(this.children.Count()))
            {
                var bitw = this.children[bitwIdx];
                var disj = bitw.type == NodeType.INCL_DISJUNCTION;
                if (!(disj))
                {
                    if (bitw.type != NodeType.PRODUCT)
                    {
                        continue;
                    }
                    if (!(bitw.children[0].__is_constant(-(1))))
                    {
                        continue;
                    }
                    bitw = bitw.children[1];
                    if (bitw.type != NodeType.CONJUNCTION)
                    {
                        continue;
                    }
                }
                var other = null;
                if (this.children.Count() == 2)
                {
                    var oIdx = (bitwIdx == 0) ? 1 : 0;
                    other = this.children[oIdx].get_copy();
                }
                else
                {
                    other = this.get_copy();
                    other.children.RemoveAt(bitwIdx);
                }
                if (disj)
                {
                    other.__multiply_by_minus_one();
                }
                var idx = bitw.__get_index_of_child(other);
                if (idx == null)
                {
                    continue;
                }
                this.copy(bitw);
                if (this.children.Count() > 2)
                {
                    var node = bitw.__get_shallow_copy();
                    node.children.RemoveAt(idx);
                    var neg = this.__new_node_with_children(NodeType.NEGATION, new List<dynamic>() { node });
                    this.children = new List<dynamic>() { this.children[idx].__get_shallow_copy(), neg };
                }
                else
                {
                    oIdx = (idx == 0) ? 1 : 0;
                    this.children[oIdx].__negate();
                }
                if (disj)
                {
                    this.copy(this.__new_node_with_children(NodeType.NEGATION, new List<dynamic>() { this.__get_shallow_copy() }));
                }
                return true;
            }
            return false;
        }

        public bool __check_insert_xor_in_sum()
        {
            if (this.type != NodeType.SUM)
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if (i >= this.children.Count())
                {
                    break;
                }
                var first = this.children[i].get_copy();
                first.__multiply_by_minus_one();
                if (!((new List<dynamic>() { NodeType.CONJUNCTION, NodeType.PRODUCT }).Contains(first.type)))
                {
                    continue;
                }
                if ((first.type != NodeType.PRODUCT && first.children.Count() != 2))
                {
                    continue;
                }
                foreach (var j in range(this.children.Count()))
                {
                    if (i == j)
                    {
                        continue;
                    }
                    var disj = this.children[j];
                    if (!((new List<dynamic>() { NodeType.INCL_DISJUNCTION, NodeType.PRODUCT }).Contains(disj.type)))
                    {
                        continue;
                    }
                    if (first.type == NodeType.PRODUCT != disj.type == NodeType.PRODUCT)
                    {
                        continue;
                    }
                    var conj = first;
                    if (conj.type == NodeType.PRODUCT)
                    {
                        var indices = conj.__get_only_differing_child_indices(disj);
                        if (indices == null)
                        {
                            continue;
                        }
                        conj = conj.children[indices[0]];
                        disj = disj.children[indices[1]];
                        if (conj.type != NodeType.CONJUNCTION)
                        {
                            continue;
                        }
                        if (disj.type != NodeType.INCL_DISJUNCTION)
                        {
                            continue;
                        }
                    }
                    if (!(do_children_match(conj.children, disj.children)))
                    {
                        continue;
                    }
                    disj.type = NodeType.EXCL_DISJUNCTION;
                    this.children.RemoveAt(i);
                    i -= 1;
                    changed = true;
                    break;
                }
            }
            if (!(changed))
            {
                return false;
            }
            if (this.children.Count() == 1)
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public bool is_linear()
        {
            return (this.state == NodeState.BITWISE || this.state == NodeState.LINEAR);
        }

        public void mark_linear(bool restrictedScope = false)
        {
            foreach (var c in this.children)
            {
                if ((!(restrictedScope) || c.state == NodeState.UNKNOWN))
                {
                    c.mark_linear();
                }
            }
            if (this.type == NodeType.INCL_DISJUNCTION)
            {
                this.__mark_linear_bitwise();
            }
            else
            {
                if (this.type == NodeType.EXCL_DISJUNCTION)
                {
                    this.__mark_linear_bitwise();
                }
                else
                {
                    if (this.type == NodeType.CONJUNCTION)
                    {
                        this.__mark_linear_bitwise();
                    }
                    else
                    {
                        if (this.type == NodeType.SUM)
                        {
                            this.__mark_linear_sum();
                        }
                        else
                        {
                            if (this.type == NodeType.PRODUCT)
                            {
                                this.__mark_linear_product();
                            }
                            else
                            {
                                if (this.type == NodeType.NEGATION)
                                {
                                    this.__mark_linear_bitwise();
                                }
                                else
                                {
                                    if (this.type == NodeType.POWER)
                                    {
                                        this.__mark_linear_power();
                                    }
                                    else
                                    {
                                        if (this.type == NodeType.VARIABLE)
                                        {
                                            this.__mark_linear_variable();
                                        }
                                        else
                                        {
                                            if (this.type == NodeType.CONSTANT)
                                            {
                                                this.__mark_linear_constant();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            this.__reorder_and_determine_linear_end();
        }

        public void __mark_linear_bitwise()
        {
            foreach (var c in this.children)
            {
                Assert.True(c.state != NodeState.UNKNOWN);
                if (c.state != NodeState.BITWISE)
                {
                    this.state = NodeState.MIXED;
                    return;
                }
            }
            this.state = NodeState.BITWISE;
        }

        public void __mark_linear_sum()
        {
            Assert.True(this.children.Count() > 1);
            this.state = NodeState.UNKNOWN;
            foreach (var c in this.children)
            {
                Assert.True(c.state != NodeState.UNKNOWN);
                if (c.state == NodeState.MIXED)
                {
                    this.state = NodeState.MIXED;
                    return;
                }
                else
                {
                    if (c.state == NodeState.NONLINEAR)
                    {
                        this.state = NodeState.NONLINEAR;
                    }
                }
            }
            if (this.state != NodeState.NONLINEAR)
            {
                this.state = NodeState.LINEAR;
            }
        }

        public void __mark_linear_product()
        {
            Assert.True(this.children.Count() > 0);
            if (this.children.Count() < 2)
            {
                this.state = this.children[0].state;
            }
            foreach (var c in this.children)
            {
                Assert.True(c.state != NodeState.UNKNOWN);
                if (c.state == NodeState.MIXED)
                {
                    this.state = NodeState.MIXED;
                    return;
                }
            }
            if (this.children.Count() > 2)
            {
                this.state = NodeState.NONLINEAR;
            }
            else
            {
                if ((this.children[0].type == NodeType.CONSTANT && this.children[1].is_linear()))
                {
                    this.state = NodeState.LINEAR;
                }
                else
                {
                    if ((this.children[1].type == NodeType.CONSTANT && this.children[0].is_linear()))
                    {
                        this.state = NodeState.LINEAR;
                    }
                    else
                    {
                        this.state = NodeState.NONLINEAR;
                    }
                }
            }
        }

        public void __mark_linear_power()
        {
            foreach (var c in this.children)
            {
                Assert.True(c.state != NodeState.UNKNOWN);
                if (c.state == NodeState.MIXED)
                {
                    this.state = NodeState.MIXED;
                    return;
                }
            }
            this.state = NodeState.NONLINEAR;
        }

        public void __mark_linear_variable()
        {
            this.state = NodeState.BITWISE;
        }

        public void __mark_linear_constant()
        {
            if ((this.__is_constant(0) || this.__is_constant(-(1))))
            {
                this.state = NodeState.BITWISE;
            }
            else
            {
                this.state = NodeState.LINEAR;
            }
        }

        public void __reorder_and_determine_linear_end()
        {
            this.linearEnd = 0;
            if (this.type == NodeType.POWER)
            {
                return;
            }
            if ((this.state != NodeState.NONLINEAR && this.state != NodeState.MIXED))
            {
                this.linearEnd = this.children.Count();
                return;
            }
            if (this.type == NodeType.PRODUCT)
            {
                this.__reorder_and_determine_linear_end_product();
                return;
            }
            foreach (var i in range(this.children.Count()))
            {
                var child = this.children[i];
                var bitwise = ((new List<dynamic>() { NodeType.CONJUNCTION, NodeType.EXCL_DISJUNCTION, NodeType.INCL_DISJUNCTION, NodeType.NEGATION }).Contains(this.type));
                if ((child.state == NodeState.BITWISE || (!(bitwise) && child.state == NodeState.LINEAR)))
                {
                    if (this.linearEnd < i)
                    {
                        this.children.remove(child);
                        this.children.Insert(this.linearEnd, child);
                    }
                    this.linearEnd += 1;
                }
            }
        }

        public void __reorder_and_determine_linear_end_product()
        {
            if (this.children[0].type != NodeType.CONSTANT)
            {
                return;
            }
            this.linearEnd = 1;
            foreach (var i in range(1, this.children.Count()))
            {
                var child = this.children[i];
                if ((child.state != NodeState.NONLINEAR && child.state != NodeState.MIXED))
                {
                    if (this.linearEnd < i)
                    {
                        this.children.remove(child);
                        this.children.Insert(this.linearEnd, child);
                    }
                    this.linearEnd += 1;
                    if (this.linearEnd == 2)
                    {
                        return;
                    }
                }
            }
        }

        public void get_node_for_substitution(dynamic ignoreList)
        {
            if (this.__is_contained(ignoreList))
            {
                return null;
            }
            if (this.__is_bitwise_op())
            {
                foreach (var child in this.children)
                {
                    if ((child.type == NodeType.CONSTANT || child.__is_arithm_op()))
                    {
                        if (!(child.__is_contained(ignoreList)))
                        {
                            return child.get_copy();
                        }
                    }
                    else
                    {
                        if (child.__is_bitwise_op())
                        {
                            var node = child.get_node_for_substitution(ignoreList);
                            if (node != null)
                            {
                                return node;
                            }
                        }
                    }
                }
                return null;
            }
            if (this.type == NodeType.POWER)
            {
                return this.children[0].get_node_for_substitution(ignoreList);
            }
            foreach (var child in this.children)
            {
                node = child.get_node_for_substitution(ignoreList);
                if (node != null)
                {
                    return node;
                }
            }
            return null;
        }

        public bool __is_contained(dynamic l)
        {
            return this.__get_index_in_list(l) != null;
        }

        public void __get_index_in_list(dynamic l)
        {
            foreach (var i in range(l.Count()))
            {
                if (this.Equals(l[i]))
                {
                    return i;
                }
            }
            return null;
        }

        public dynamic substitute_all_occurences(dynamic node, dynamic vname, bool onlyFullMatch = false, bool withMod = true)
        {
            if (this.type == NodeType.POWER)
            {
                return this.children[0].substitute_all_occurences(node, vname, onlyFullMatch, withMod);
            }
            var changed = false;
            var bitwise = this.__is_bitwise_op();
            var inv = null;
            if ((!(bitwise) && !(onlyFullMatch) && withMod))
            {
                inv = node.get_copy();
                inv.__multiply_by_minus_one();
            }
            foreach (var child in this.children)
            {
                var (ch, done) = child.__try_substitute_node(node, vname, bitwise);
                if (ch)
                {
                    changed = true;
                }
                if (done)
                {
                    continue;
                }
                if ((!(bitwise) && !(onlyFullMatch) && withMod))
                {
                    Assert.True(inv != null);
                    var (ch, done) = child.__try_substitute_node(inv, vname, false, true);
                    if (ch)
                    {
                        changed = true;
                    }
                    if (done)
                    {
                        continue;
                    }
                    if (child.__try_substitute_part_of_sum(node, vname))
                    {
                        changed = true;
                        continue;
                    }
                    if (child.__try_substitute_part_of_sum(inv, vname, true))
                    {
                        changed = true;
                        continue;
                    }
                }
                if ((bitwise && !(child.__is_bitwise_op())))
                {
                    continue;
                }
                if (child.substitute_all_occurences(node, vname, onlyFullMatch, withMod))
                {
                    changed = true;
                }
            }
            return changed;
        }

        public dynamic __try_substitute_node(dynamic node, dynamic vname, dynamic onlyFull, bool inverse = false)
        {
            if (this.Equals(node))
            {
                var var = this.__new_variable_node(vname);
                if (inverse)
                {
                    var.__multiply_by_minus_one();
                }
                this.copy(var);
                return (true, true);
            }
            if (onlyFull)
            {
                return (false, false);
            }
            if ((node.children.Count() > 1 && node.type == this.type && this.children.Count() > node.children.Count()))
            {
                if (node.__are_all_children_contained(self))
                {
                    this.__remove_children_of_node(node);
                    var = this.__new_variable_node(vname);
                    if (inverse)
                    {
                        var.__multiply_by_minus_one();
                    }
                    this.children.Add(var);
                    return (true, false);
                }
            }
            return (false, false);
        }

        public dynamic __try_substitute_part_of_sum(dynamic node, dynamic vname, bool inverse = false)
        {
            if ((node.type != NodeType.SUM || node.children.Count() <= 1))
            {
                return false;
            }
            if (this.type == NodeType.SUM)
            {
                return this.__try_substitute_part_of_sum_in_sum(node, vname, inverse);
            }
            return this.__try_substitute_part_of_sum_term(node, vname, inverse);
        }

        public bool __try_substitute_part_of_sum_in_sum(dynamic node, dynamic vname, dynamic inverse)
        {
            Assert.True(this.type == NodeType.SUM);
            var common = this.__get_common_children(node);
            if (common.Count() == 0)
            {
                return false;
            }
            foreach (var c in common)
            {
                this.children.remove(c);
            }
            this.children.Add(this.__new_variable_node(vname));
            if (inverse)
            {
                this.children[-(1)].__multiply_by_minus_one();
            }
            foreach (var c in node.children)
            {
                var found = false;
                foreach (var c2 in common)
                {
                    if (c.Equals(c2))
                    {
                        common.remove(c2);
                        found = true;
                        break;
                    }
                }
                if (!(found))
                {
                    var n = c.get_copy();
                    n.__multiply_by_minus_one();
                    this.children.Add(n);
                }
            }
            return true;
        }

        public dynamic __get_common_children(dynamic other)
        {
            Assert.True(other.type == this.type);
            var common = new List<dynamic>() { };
            var oIndices = list(range(other.children.Count()));
            foreach (var child in this.children)
            {
                foreach (var i in oIndices)
                {
                    if (child.Equals(other.children[i]))
                    {
                        oIndices.remove(i);
                        common.Add(child);
                        break;
                    }
                }
            }
            return common;
        }

        public bool __try_substitute_part_of_sum_term(dynamic node, dynamic vname, dynamic inverse)
        {
            Assert.True(this.type != NodeType.SUM);
            if (!(node.__has_child(self)))
            {
                return false;
            }
            var var = this.__new_variable_node(vname);
            if (inverse)
            {
                var.__multiply_by_minus_one();
            }
            var sumNode = this.__new_node_with_children(NodeType.SUM, new List<dynamic>() { var });
            var found = false;
            foreach (var c in node.children)
            {
                if ((!(found) && this.Equals(c)))
                {
                    found = true;
                    continue;
                }
                var n = c.get_copy();
                n.__multiply_by_minus_one();
                sumNode.children.Add(n);
            }
            this.copy(sumNode);
            return true;
        }

        public dynamic count_nodes(void typeList = null)
        {
            var cnt = 0;
            foreach (var child in this.children)
            {
                cnt += child.count_nodes(typeList);
            }
            if ((typeList == null || ((typeList).Contains(this.type))))
            {
                cnt += 1;
            }
            return cnt;
        }

        public dynamic compute_alternation_linear(bool hasParent = false)
        {
            if (((new List<dynamic>() { NodeType.SUM, NodeType.PRODUCT }).Contains(this.type)))
            {
                Assert.True(this.children.Count() > 0);
                var cnt = 0;
                foreach (var child in this.children)
                {
                    cnt += child.compute_alternation_linear(true);
                }
                return cnt;
            }
            if (!(hasParent))
            {
                return 0;
            }
            return int((this.type != NodeType.VARIABLE && this.type != NodeType.CONSTANT));
        }

        public dynamic compute_alternation(void parentBitwise = null)
        {
            if (this.type == NodeType.VARIABLE)
            {
                return 0;
            }
            if (this.type == NodeType.CONSTANT)
            {
                return int((parentBitwise != null && parentBitwise == true));
            }
            var bitw = this.__is_bitwise_op();
            var cnt = int((parentBitwise != null && parentBitwise != bitw));
            foreach (var child in this.children)
            {
                cnt += child.compute_alternation(bitw);
            }
            return cnt;
        }

        public void polish(void parent = null)
        {
            foreach (var c in this.children)
            {
                c.polish(self);
            }
            this.__reorder_variables();
            this.__resolve_bitwise_negations_in_sums();
            this.__insert_bitwise_negations(parent);
            this.__reorder_variables();
        }

        public void __resolve_bitwise_negations_in_sums()
        {
            if (this.type != NodeType.SUM)
            {
                return;
            }
            var count = 0;
            foreach (var i in range(this.children.Count()))
            {
                if (this.children[i].type != NodeType.NEGATION)
                {
                    continue;
                }
                this.children[i] = this.children[i].children[0];
                this.children[i].__multiply_by_minus_one();
                count += 1;
            }
            if (count != 0)
            {
                if (this.children[0].type == NodeType.CONSTANT)
                {
                    this.children[0].__set_and_reduce_constant(this.children[0].constant - count);
                    if (this.children[0].__is_constant(0))
                    {
                        this.children.RemoveAt(0);
                    }
                }
                else
                {
                    this.children.Insert(0, this.__new_constant_node(-(count)));
                }
            }
            if (this.children[0].type != NodeType.CONSTANT)
            {
                return;
            }
            var negConst = -(this.children[0].constant) % this.__modulus;
            if (this.children.Count() < negConst)
            {
                return;
            }
            var countM = this.__count_children_mult_by_minus_one();
            if (countM < negConst)
            {
                return;
            }
            var todo = negConst;
            foreach (var i in range(this.children.Count()))
            {
                var child = this.children[i];
                if (todo == 0)
                {
                    break;
                }
                if (child.type != NodeType.PRODUCT)
                {
                    continue;
                }
                if (!(child.children[0].__is_constant(-(1))))
                {
                    continue;
                }
                child.children.RemoveAt(0);
                Assert.True(child.children.Count() > 0);
                if (child.children.Count() == 1)
                {
                    child.type = NodeType.NEGATION;
                }
                else
                {
                    this.children[i] = this.__new_node_with_children(NodeType.NEGATION, new List<dynamic>() { child.__get_shallow_copy() });
                }
                todo -= 1;
            }
            Assert.True(todo == 0);
            this.children.RemoveAt(0);
        }

        public dynamic __count_children_mult_by_minus_one()
        {
            var count = 0;
            foreach (var child in this.children)
            {
                if (child.type != NodeType.PRODUCT)
                {
                    continue;
                }
                if (child.children[0].__is_constant(-(1)))
                {
                    count += 1;
                }
            }
            return count;
        }

        public void __insert_bitwise_negations(dynamic parent)
        {
            var (child, factor) = this.__get_opt_transformed_negated_with_factor();
            Assert.True(child != null == factor != null);
            if (child == null)
            {
                return;
            }
            this.type = NodeType.NEGATION;
            this.children = new List<dynamic>() { child };
            if (factor == 1)
            {
                return;
            }
            if ((parent != null && parent.type == NodeType.PRODUCT))
            {
                parent.__multiply(factor);
            }
            else
            {
                this.__multiply(factor);
            }
        }

        public dynamic __get_opt_transformed_negated_with_factor()
        {
            if (this.type != NodeType.SUM)
            {
                return (null, null);
            }
            if (this.children.Count() < 2)
            {
                return (null, null);
            }
            if (this.children[0].type != NodeType.CONSTANT)
            {
                return (null, null);
            }
            var factor = this.children[0].constant;
            var res = this.__new_node(NodeType.SUM);
            foreach (var i in range(1, this.children.Count()))
            {
                res.children.Add(this.children[i].get_copy());
                var child = res.children[-(1)];
                if (factor - 1 % this.__modulus == 0)
                {
                    continue;
                }
                if (factor + 1 % this.__modulus == 0)
                {
                    child.__multiply_by_minus_one();
                    continue;
                }
                if (child.type != NodeType.PRODUCT)
                {
                    return (null, null);
                }
                if (child.children[0].type != NodeType.CONSTANT)
                {
                    return (null, null);
                }
                var constNode = child.children[0];
                var c = this.__get_reduced_constant_closer_to_zero(constNode.constant);
                if (c % factor != 0)
                {
                    return (null, null);
                }
                constNode.__set_and_reduce_constant(c / factor);
                if (constNode.__is_constant(1))
                {
                    res.children[-(1)].children.RemoveAt(0);
                    if (child.children.Count() == 1)
                    {
                        res.children[-(1)] = child.children[0];
                    }
                }
            }
            Assert.True(res.children.Count() > 0);
            if (res.children.Count() == 1)
            {
                return (res.children[0], -(factor));
            }
            return (res, -(factor));
        }

        public void sort()
        {
            foreach (var c in this.children)
            {
                c.sort();
            }
            this.__reorder_variables();
        }

        public void __reorder_variables()
        {
            if (this.type < NodeType.PRODUCT)
            {
                return;
            }
            if (this.children.Count() <= 1)
            {
                return;
            }
            this.children.sort();
        }

        public dynamic __lt__(dynamic other)
        {
            if (this.type == NodeType.CONSTANT)
            {
                return true;
            }
            if (other.type == NodeType.CONSTANT)
            {
                return false;
            }
            var vn1 = this.__get_extended_variable();
            var vn2 = other.__get_extended_variable();
            if (vn1 != null)
            {
                if (vn2 == null)
                {
                    return true;
                }
                if (vn1 != vn2)
                {
                    return vn1 < vn2;
                }
                return this.type == NodeType.VARIABLE;
            }
            if (vn2 != null)
            {
                return false;
            }
            return (this.type != other.type) ? this.type < other.type : this.children.Count() < other.children.Count();
        }

        public void __get_extended_variable()
        {
            if (this.type == NodeType.VARIABLE)
            {
                return this.vname;
            }
            if (this.type == NodeType.NEGATION)
            {
                return (this.children[0].type == NodeType.VARIABLE) ? this.children[0].vname : null;
            }
            if (this.type == NodeType.PRODUCT)
            {
                if ((this.children.Count() == 2 && this.children[0].type == NodeType.CONSTANT && this.children[1].type == NodeType.VARIABLE))
                {
                    return this.children[1].vname;
                }
                return null;
            }
            return null;
        }

        public bool check_verify(dynamic other, ulong bitCount = 2)
        {
            var variables = new List<dynamic>() { };
            other.collect_and_enumerate_variables(variables);
            this.enumerate_variables(variables);
            var vnumber = variables.Count();
            public dynamic f1(dynamic X)
            {
                return other.eval(X);
            }

            public dynamic f2(dynamic X)
            {
                return this.eval(X);
            }

            var mask = 2 * *bitCount - 1;
            var total = 2 * *vnumber * bitCount;
            var currJ = -(1);
            foreach (var i in range(total))
            {
                var n = i;
                var par = new List<dynamic>() { };
                foreach (var j in range(vnumber))
                {
                    par.Add(n & mask);
                    n = n >> bitCount;
                }
                var v1 = f1(par);
                var v2 = f2(par);
                if (v1 != v2)
                {
                    print("
    * **... verification failed for input " + str(par) + ": orig " + str(v1) + ", output " + str(v2));
                    return false;
                }
                var j = i + 1 / total;
                if (j != currJ)
                {
                    sys.stdout.write("
    ");
    
                    sys.stdout.write("*** ... verify via evaluation ... [%-20s] %d%%" % ("=" * int(20 * j), 100 * j));
                    sys.stdout.flush();
                }
            }
            print();
            print("*** ... verification successful!");
            return true;
        }

        public ulong count_terms_linear()
        {
            Assert.True(this.is_linear());
            if (this.type == NodeType.SUM)
            {
                var t = 0;
                foreach (var child in this.children)
                {
                    t += child.count_terms_linear();
                }
                return t;
            }
            if (this.type == NodeType.PRODUCT)
            {
                Assert.True(this.children.Count() == 2);
                if (this.children[0].type == NodeType.CONSTANT)
                {
                    return this.children[1].count_terms_linear();
                }
                Assert.True(this.children[1].type == NodeType.CONSTANT);
                return this.children[0].count_terms_linear();
            }
            return 1;
        }

        public void print(ulong level = 0)
        {
            var indent = 2 * level;
            var prefix = indent * " " + "[" + str(level) + "] ";
            if (this.type == NodeType.CONSTANT)
            {
                print(prefix + "CONST " + str(this.constant));
                return;
            }
            if (this.type == NodeType.VARIABLE)
            {
                print(prefix + "VAR " + this.vname + " [vidx " + str(this.__vidx) + "]");
                return;
            }
            print(prefix + str(this.type));
            foreach (var c in this.children)
            {
                c.print(level + 1);
            }
        }

    }



}
