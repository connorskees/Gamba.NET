using Antlr4.Runtime.Dfa;
using Gamba.Prototyping.Extensions;
using GambaDotnet;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
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
        // Some bounds to control performance.
        const int MAX_CHILDREN_SUMMED_UP = 3;
        const int MAX_CHILDREN_TO_TRANSFORM_BITW = 5;
        const int MAX_EXPONENT_TO_EXPAND = 2;

        const int NICE_RANDOM_NUMBER_BOUND = 32;
        const int SPLIT_FREQ_BITWISE = 1;
        const int SPLIT_FREQ_OTHERS = 4;

        public static long popcount(long x) => long.PopCount(x);

        // https://stackoverflow.com/a/45225089/6855629
        // This may not be equivalent to GAMBA's trailing_bits implementation.
        // TODO: Verify
        public static long trailing_zeros(long n)
        {
            return (long)BitOperations.TrailingZeroCount(n);

            ulong bits = 0;
            ulong x = (ulong)n;

            if (x != 0)
            {
                while ((x & 1) == 0)
                {
                    ++bits;
                    x >>= 1;
                }
            }
            return (long)bits;
        }

        // TODO: Use an actual power implementation.
        public static long LongPower(long x, long y)
        {
            return (long)ULongPower((ulong)x, (ulong)y);
            if (y == 0)
                return 1;
            if (y == 1)
                return x;

            var original = x;
            for (long i = 0; i < y - 1; i++)
            {
                x *= original;
            }

            return x;
        }

        // TODO: Use an actual power implementation.
        public static ulong ULongPower(ulong x, ulong y)
        {
            ;
            if (y == 0)
                return 1;
            if (y == 1)
                return x;

            var original = x;
            for (ulong i = 0; i < y - 1; i++)
            {
                x = x * original;
            }

            return x;
        }

        public static long power(long x, long e, long m)
        {
            if (x == 1)
                return 1;

            long r = 1;
            for (long i = 0; i < e; i++)
            {
                r = (r * x) % m;
                if (r == 0)
                    return 0;
            }

            return r;
        }

        static long py_mod(long a, long b)
        {
            if (a < 0)
                if (b < 0)
                    return -(-a % -b);
                else
                    return -a % b - (-a % -b != 0 ? 1 : 0);
            else if (b < 0)
                return -(a % -b) + (-a % -b != 0 ? 1 : 0);
            else
                return a % b;
        }

        public static Context ctx = new();

        public static long mod_red(long n, long modulus)
        {
            var mod = ctx.MkBVSMod(ctx.MkBV(n, 64), ctx.MkBV(modulus, 64)).Simplify() as BitVecNum;
            return mod.Int64;
        }

        public static bool do_children_match(List<Node> l1, List<Node> l2)
        {
            if ((l1.Count()) != (l2.Count()))
            {
                return false;
            }
            return are_all_children_contained(l1, l2);
        }

        public static bool are_all_children_contained(List<Node> l1, List<Node> l2)
        {
            List<int> oIndices = new(Range.Get(l2.Count()));
            foreach (var child in l1)
            {
                var found = false;
                foreach (var i in oIndices.ToList())
                {
                    if (child.equals(l2[i]))
                    {
                        oIndices.Remove(i);
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

        public static string str(object obj)
        {
            return obj.ToString();
        }

        public override string ToString()
        {
            return to_string();
        }

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

        public long __modulus;

        public bool __modRed;

        public List<Node> children;

        public string vname = "";

        public int __vidx = -1;

        public long constant = 0;

        public NodeState state = NodeState.UNKNOWN;

        public int linearEnd = 0;

        public int __MAX_IT = 10;

        public Node(NodeType nodeType, long modulus, bool modRed = false)
        {
            this.type = nodeType;
            this.children = new List<Node>() { };
            this.vname = "";
            this.__vidx = -(1);
            this.constant = 0;
            this.state = NodeState.UNKNOWN;
            this.__modulus = modulus;
            this.__modRed = modRed;
            this.linearEnd = 0;
            this.__MAX_IT = 10;
        }

        public string __str__()
        {
            return this.to_string();
        }

        public string to_string(bool withParentheses = false, int end = -(1), List<string> varNames = null)
        {
            if ((end) == (-(1)))
            {
                end = this.children.Count();
            }
            else
            {
                Assert.True((end) <= (this.children.Count()));
            }
            if ((this.type) == (NodeType.CONSTANT))
            {
                return str(this.constant);
            }
            if ((this.type) == (NodeType.VARIABLE))
            {
                return ((varNames) == (null)) ? this.vname : varNames[this.__vidx];
            }
            string ret = null;
            Node child1 = null;
            if ((this.type) == (NodeType.POWER))
            {
                Assert.True((this.children.Count()) == (2));
                child1 = this.children[0];
                var child2 = this.children[1];
                ret = ((((child1.to_string((child1.type) > (NodeType.VARIABLE), -(1), varNames)) + ("**"))) + (child2.to_string((child2.type) > (NodeType.VARIABLE), -(1), varNames)));
                if (withParentheses)
                {
                    ret = (((("(") + (ret))) + (")"));
                }
                return ret;
            }
            if ((this.type) == (NodeType.NEGATION))
            {
                Assert.True((this.children.Count()) == (1));
                var child = this.children[0];
                ret = (("~") + (child.to_string((child.type) > (NodeType.NEGATION), -(1), varNames)));
                if (withParentheses)
                {
                    ret = (((("(") + (ret))) + (")"));
                }
                return ret;
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                Assert.True((this.children.Count()) > (0));
                child1 = this.children[0];
                var ret1 = child1.to_string((child1.type) > (NodeType.PRODUCT), -(1), varNames);
                ret = ret1;
                foreach (var child in this.children.Slice(1, end, null))
                {
                    ret += (("*") + (child.to_string((child.type) > (NodeType.PRODUCT), -(1), varNames)));
                }
                if ((((ret1) == ("-1")) && ((this.children.Count()) > (1)) && ((end) > (1))))
                {
                    ret = (("-") + (ret.Slice(3, null, null)));
                }
                if (withParentheses)
                {
                    ret = (((("(") + (ret))) + (")"));
                }
                return ret;
            }
            if ((this.type) == (NodeType.SUM))
            {
                Assert.True((this.children.Count()) > (0));
                child1 = this.children[0];
                ret = child1.to_string((child1.type) > (NodeType.SUM), -(1), varNames);
                foreach (var child in this.children.Slice(1, end, null))
                {
                    var s = child.to_string((child.type) > (NodeType.SUM), -(1), varNames);
                    if ((str(s[0])) != ("-"))
                    {
                        ret += "+";
                    }
                    ret += s;
                }
                if (withParentheses)
                {
                    ret = (((("(") + (ret))) + (")"));
                }
                return ret;
            }
            if ((this.type) == (NodeType.CONJUNCTION))
            {
                Assert.True((this.children.Count()) > (0));
                child1 = this.children[0];
                ret = child1.to_string((child1.type) > (NodeType.CONJUNCTION), -(1), varNames);
                foreach (var child in this.children.Slice(1, end, null))
                {
                    ret += (("&") + (child.to_string((child.type) > (NodeType.CONJUNCTION), -(1), varNames)));
                }
                if (withParentheses)
                {
                    ret = (((("(") + (ret))) + (")"));
                }
                return ret;
            }
            else
            {
                if ((this.type) == (NodeType.EXCL_DISJUNCTION))
                {
                    Assert.True((this.children.Count()) > (0));
                    child1 = this.children[0];
                    ret = child1.to_string((child1.type) > (NodeType.EXCL_DISJUNCTION), -(1), varNames);
                    foreach (var child in this.children.Slice(1, end, null))
                    {
                        ret += (("^") + (child.to_string((child.type) > (NodeType.EXCL_DISJUNCTION), -(1), varNames)));
                    }
                    if (withParentheses)
                    {
                        ret = (((("(") + (ret))) + (")"));
                    }
                    return ret;
                }
            }
            if ((this.type) == (NodeType.INCL_DISJUNCTION))
            {
                Assert.True((this.children.Count()) > (0));
                child1 = this.children[0];
                ret = child1.to_string((child1.type) > (NodeType.INCL_DISJUNCTION), -(1), varNames);
                foreach (var child in this.children.Slice(1, end, null))
                {
                    ret += (("|") + (child.to_string((child.type) > (NodeType.INCL_DISJUNCTION), -(1), varNames)));
                }
                if (withParentheses)
                {
                    ret = (((("(") + (ret))) + (")"));
                }
                return ret;
            }
            Assert.True(false);
            return null;
        }

        public string part_to_string(int end)
        {
            return this.to_string(false, end);
        }

        public long __power(long b, long e)
        {
            return power(b, e, this.__modulus);
        }

        public long __get_reduced_constant(long c)
        {
            if (this.__modRed)
            {
                return mod_red(c, this.__modulus);
            }
            return this.__get_reduced_constant_closer_to_zero(c);
        }

        public long __get_reduced_constant_closer_to_zero(long c)
        {
            c = mod_red(c, this.__modulus);
            if ((((2) * (c))) > (this.__modulus))
            {
                c -= this.__modulus;
            }
            return c;
        }

        public void __reduce_constant()
        {
            if (constant == 12)
                Debugger.Break();
            this.constant = this.__get_reduced_constant(this.constant);
        }

        public void __set_and_reduce_constant(long c)
        {
            this.constant = this.__get_reduced_constant(c);
        }

        public void collect_and_enumerate_variables(List<string> variables)
        {
            this.collect_variables(variables);
            variables.Sort();
            this.enumerate_variables(variables);
        }

        public void collect_variables(List<string> variables)
        {
            if ((this.type) == (NodeType.VARIABLE))
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

        public void enumerate_variables(List<string> variables)
        {
            if ((this.type) == (NodeType.VARIABLE))
            {
                Assert.True(((variables).Contains(this.vname)));
                this.__vidx = variables.IndexOf(this.vname);
            }
            else
            {
                foreach (var child in this.children)
                {
                    child.enumerate_variables(variables);
                }
            }
        }

        public NullableI32 get_max_vname(string start, string end)
        {
            string nStr = null;
            if ((this.type) == (NodeType.VARIABLE))
            {
                if ((this.vname.Slice(null, start.Count(), null)) != (start))
                {
                    return null;
                }
                if ((this.vname.Slice(-(end.Count()), null, null)) != (end))
                {
                    return null;
                }
                nStr = this.vname.Slice(start.Count(), -(end.Count()), null);
                if (!(nStr.isnumeric()))
                {
                    return null;
                }
                return Convert.ToInt32(nStr);
            }
            else
            {
                NullableI32 maxn = null;
                foreach (var child in this.children)
                {
                    var n = child.get_max_vname(start, end);
                    if ((((n) != (null)) && ((((maxn) == (null)) || ((n) > (maxn))))))
                    {
                        maxn = n;
                    }
                }
                return maxn;
            }
        }

        public long eval(List<long> X)
        {
            if ((this.type) == (NodeType.CONSTANT))
            {
                return ((this.constant) % (this.__modulus));
            }
            if ((this.type) == (NodeType.VARIABLE))
            {
                if ((this.__vidx) < (0))
                {
                    throw new InvalidOperationException("ERROR: Variable index not set prior to evaluation!");
                }
                return ((X[this.__vidx]) % (this.__modulus));
            }
            Assert.True((this.children.Count()) > (0));
            if ((this.type) == (NodeType.NEGATION))
            {
                return ((~(this.children[0].eval(X))) % (this.__modulus));
            }
            var val = this.children[0].eval(X);
            foreach (var i in Range.Get(1, this.children.Count()))
            {
                val = ((this.__apply_binop(val, this.children[i].eval(X))) % (this.__modulus));
            }
            return val;
        }

        public long __apply_binop(long x, long y)
        {
            if ((this.type) == (NodeType.POWER))
            {
                return this.__power(x, y);
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                return ((x) * (y));
            }
            if ((this.type) == (NodeType.SUM))
            {
                return ((x) + (y));
            }
            return this.__apply_bitwise_binop(x, y);
        }

        public long __apply_bitwise_binop(long x, long y)
        {
            if ((this.type) == (NodeType.CONJUNCTION))
            {
                return ((x) & (y));
            }
            if ((this.type) == (NodeType.EXCL_DISJUNCTION))
            {
                return ((x) ^ (y));
            }
            if ((this.type) == (NodeType.INCL_DISJUNCTION))
            {
                return ((x) | (y));
            }
            Assert.True(false);
            return 0;
        }

        public bool has_nonlinear_child()
        {
            foreach (var child in this.children)
            {
                if ((((child.state) == (NodeState.NONLINEAR)) || ((child.state) == (NodeState.MIXED))))
                {
                    return true;
                }
            }
            return false;
        }

        public bool __are_all_children_contained(Node other)
        {
            Assert.True((other.type) == (this.type));
            return are_all_children_contained(this.children, other.children);
        }

        public bool equals(Node other)
        {
            if ((this.type) != (other.type))
            {
                return this.__equals_rewriting_bitwise(other);
            }
            if ((this.type) == (NodeType.CONSTANT))
            {
                return (this.constant) == (other.constant);
            }
            if ((this.type) == (NodeType.VARIABLE))
            {
                return (this.vname) == (other.vname);
            }
            if ((this.children.Count()) != (other.children.Count()))
            {
                return false;
            }
            return this.__are_all_children_contained(other);
        }

        public bool equals_negated(Node other)
        {
            if ((((this.type) == (NodeType.NEGATION)) && (this.children[0].equals(other))))
            {
                return true;
            }
            if ((((other.type) == (NodeType.NEGATION)) && (other.children[0].equals(this))))
            {
                return true;
            }
            return false;
        }

        public bool __equals_rewriting_bitwise(Node other)
        {
            return ((this.__equals_rewriting_bitwise_asymm(other)) || (other.__equals_rewriting_bitwise_asymm(this)));
        }

        public bool __equals_rewriting_bitwise_asymm(Node other)
        {
            Node node = null;
            if ((this.type) == (NodeType.NEGATION))
            {
                node = other.__get_opt_transformed_negated();
                return (((node) != (null)) && (node.equals(this.children[0])));
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                if ((this.children.Count()) != (2))
                {
                    return false;
                }
                if (!(this.children[0].__is_constant(-(1))))
                {
                    return false;
                }
                if ((this.children[1].type) != (NodeType.NEGATION))
                {
                    return false;
                }
                node = other.__get_opt_negative_transformed_negated();
                return (((node) != (null)) && (node.equals(this.children[1].children[0])));
            }
            return false;
        }

        public Node __get_opt_negative_transformed_negated()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return null;
            }
            if ((this.children.Count()) < (2))
            {
                return null;
            }
            if (!(this.children[0].__is_constant(1)))
            {
                return null;
            }
            var res = this.__new_node(NodeType.SUM);
            foreach (var i in Range.Get(1, this.children.Count()))
            {
                res.children.Add(this.children[i].get_copy());
            }
            Assert.True((res.children.Count()) > (0));
            if ((res.children.Count()) == (1))
            {
                return res.children[0];
            }
            return res;
        }

        public void __remove_children_of_node(Node other)
        {
            foreach (var ochild in other.children)
            {
                foreach (var i in Range.Get(this.children.Count()))
                {
                    if (this.children[i].equals(ochild))
                    {
                        this.children.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public Node __new_node(NodeType t)
        {
            return new Node(t, this.__modulus, this.__modRed);
        }

        public Node __new_constant_node(long constant)
        {
            var node = this.__new_node(NodeType.CONSTANT);
            node.constant = constant;
            node.__reduce_constant();
            return node;
        }

        public Node __new_variable_node(string vname)
        {
            var node = this.__new_node(NodeType.VARIABLE);
            node.vname = vname;
            return node;
        }

        public Node __new_node_with_children(NodeType t, List<Node> children)
        {
            var node = this.__new_node(t);
            node.children = children;
            return node;
        }

        public void replace_variable_by_constant(string vname, long constant)
        {
            this.replace_variable(vname, this.__new_constant_node(constant));
        }

        public void replace_variable(string vname, Node node)
        {
            if ((this.type) == (NodeType.VARIABLE))
            {
                if ((this.vname) == (vname))
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

        public void refine(Node parent = null, bool restrictedScope = false)
        {
            foreach (var i in Range.Get(this.__MAX_IT))
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
            if ((this.type) == (NodeType.INCL_DISJUNCTION))
            {
                this.__inspect_constants_incl_disjunction();
            }
            else
            {
                if ((this.type) == (NodeType.EXCL_DISJUNCTION))
                {
                    this.__inspect_constants_excl_disjunction();
                }
                else
                {
                    if ((this.type) == (NodeType.CONJUNCTION))
                    {
                        this.__inspect_constants_conjunction();
                    }
                    else
                    {
                        if ((this.type) == (NodeType.SUM))
                        {
                            this.__inspect_constants_sum();
                        }
                        else
                        {
                            if ((this.type) == (NodeType.PRODUCT))
                            {
                                this.__inspect_constants_product();
                            }
                            else
                            {
                                if ((this.type) == (NodeType.NEGATION))
                                {
                                    this.__inspect_constants_negation();
                                }
                                else
                                {
                                    if ((this.type) == (NodeType.POWER))
                                    {
                                        this.__inspect_constants_power();
                                    }
                                    else
                                    {
                                        if ((this.type) == (NodeType.CONSTANT))
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
            List<Node> toRemove = new() { };
            if (!(isMinusOne))
            {
                foreach (var child in this.children.Slice(1, null, null))
                {
                    if ((child.type) == (NodeType.CONSTANT))
                    {
                        if ((child.constant) == (0))
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
                        if ((first.type) == (NodeType.CONSTANT))
                        {
                            first.__set_and_reduce_constant(((first.constant) | (child.constant)));
                            toRemove.Add(child);
                        }
                        else
                        {
                            this.children.Remove(child);
                            this.children.Insert(0, child);
                        }
                    }
                }
            }
            if (isMinusOne)
            {
                this.children = new() { };
                this.type = NodeType.CONSTANT;
                this.__set_and_reduce_constant(-(1));
                return;
            }
            foreach (var child in toRemove)
            {
                this.children.Remove(child);
            }
            first = this.children[0];
            if ((((this.children.Count()) > (1)) && (first.__is_constant(0))))
            {
                this.children.Pop(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
        }

        public bool __is_constant(int value)
        {
            return (((this.type) == (NodeType.CONSTANT)) && ((mod_red(((this.constant) - (value)), this.__modulus)) == (0)));
        }

        public void __inspect_constants_excl_disjunction()
        {
            List<Node> toRemove = new() { };
            Node first = null;
            foreach (var child in this.children.Slice(1, null, null))
            {
                if ((child.type) == (NodeType.CONSTANT))
                {
                    if ((child.constant) == (0))
                    {
                        toRemove.Add(child);
                        continue;
                    }
                    first = this.children[0];
                    if ((first.type) == (NodeType.CONSTANT))
                    {
                        first.__set_and_reduce_constant(((first.constant) ^ (child.constant)));
                        toRemove.Add(child);
                    }
                    else
                    {
                        this.children.Remove(child);
                        this.children.Insert(0, child);
                    }
                }
            }
            foreach (var child in toRemove)
            {
                this.children.Remove(child);
            }
            first = this.children[0];
            if ((((this.children.Count()) > (1)) && (first.__is_constant(0))))
            {
                this.children.Pop(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
        }

        public void __inspect_constants_conjunction()
        {
            var first = this.children[0];
            var isZero = first.__is_constant(0);
            List<Node> toRemove = new() { };
            if (!(isZero))
            {
                foreach (var child in this.children.Slice(1, null, null))
                {
                    if ((child.type) == (NodeType.CONSTANT))
                    {
                        if (child.__is_constant(-(1)))
                        {
                            toRemove.Add(child);
                            continue;
                        }
                        if ((child.constant) == (0))
                        {
                            isZero = true;
                            break;
                        }
                        first = this.children[0];
                        if ((first.type) == (NodeType.CONSTANT))
                        {
                            first.__set_and_reduce_constant(((first.constant) & (child.constant)));
                            toRemove.Add(child);
                        }
                        else
                        {
                            this.children.Remove(child);
                            this.children.Insert(0, child);
                        }
                    }
                }
            }
            if (isZero)
            {
                this.children = new() { };
                this.type = NodeType.CONSTANT;
                this.constant = 0;
                return;
            }
            foreach (var child in toRemove)
            {
                this.children.Remove(child);
            }
            first = this.children[0];
            if ((((this.children.Count()) > (1)) && (first.__is_constant(-(1)))))
            {
                this.children.Pop(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
        }

        public void __inspect_constants_sum()
        {
            var first = this.children[0];
            List<Node> toRemove = new() { };
            var slice = this.children.Slice(1, null, null);
            foreach (var child in slice)
            {
                if ((child.type) == (NodeType.CONSTANT))
                {
                    if ((child.constant) == (0))
                    {
                        toRemove.Add(child);
                        continue;
                    }
                    first = this.children[0];
                    if ((first.type) == (NodeType.CONSTANT))
                    {
                        first.__set_and_reduce_constant(((first.constant) + (child.constant)));
                        toRemove.Add(child);
                    }
                    else
                    {
                        this.children.Remove(child);
                        this.children.Insert(0, child);
                    }
                }
            }
            foreach (var child in toRemove)
            {
                this.children.Remove(child);
            }
            first = this.children[0];
            if ((((this.children.Count()) > (1)) && (first.__is_constant(0))))
            {
                this.children.Pop(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
        }

        public void __inspect_constants_product()
        {
            var first = this.children[0];
            var isZero = first.__is_constant(0);
            List<Node> toRemove = new() { };
            if (!(isZero))
            {
                foreach (var child in this.children.Slice(1, null, null))
                {
                    if ((child.type) == (NodeType.CONSTANT))
                    {
                        if ((child.constant) == (1))
                        {
                            toRemove.Add(child);
                            continue;
                        }
                        if ((child.constant) == (0))
                        {
                            isZero = true;
                            break;
                        }
                        first = this.children[0];
                        if ((first.type) == (NodeType.CONSTANT))
                        {
                            first.__set_and_reduce_constant(((first.constant) * (child.constant)));
                            toRemove.Add(child);
                        }
                        else
                        {
                            this.children.Remove(child);
                            this.children.Insert(0, child);
                        }
                    }
                }
            }
            if (isZero)
            {
                this.children = new() { };
                this.type = NodeType.CONSTANT;
                this.constant = 0;
                return;
            }
            foreach (var child in toRemove)
            {
                this.children.Remove(child);
            }
            first = this.children[0];
            if ((((this.children.Count()) > (1)) && (first.__is_constant(1))))
            {
                this.children.Pop(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
        }

        public void __inspect_constants_negation()
        {
            Assert.True((this.children.Count()) == (1));
            var child = this.children[0];
            if ((child.type) == (NodeType.NEGATION))
            {
                this.copy(child.children[0]);
            }
            else
            {
                if ((child.type) == (NodeType.CONSTANT))
                {
                    child.__set_and_reduce_constant(((-(child.constant)) - (1)));
                    this.copy(child);
                }
            }
        }

        public void __inspect_constants_power()
        {
            Assert.True((this.children.Count()) == (2));
            var _base = this.children[0];
            var exp = this.children[1];
            if ((((_base.type) == (NodeType.CONSTANT)) && ((exp.type) == (NodeType.CONSTANT))))
            {
                _base.__set_and_reduce_constant(this.__power(_base.constant, exp.constant));
                this.copy(_base);
                return;
            }
            if ((exp.type) == (NodeType.CONSTANT))
            {
                if ((exp.constant) == (0))
                {
                    this.type = NodeType.CONSTANT;
                    this.constant = 1;
                    this.children = new() { };
                }
                else
                {
                    if ((exp.constant) == (1))
                    {
                        this.copy(_base);
                    }
                }
            }
        }

        public void __inspect_constants_constant()
        {
            this.__reduce_constant();
        }

        public void copy(Node node)
        {
            this.type = node.type;
            this.state = node.state;
            this.children = node.children;
            this.vname = node.vname;
            this.__vidx = node.__vidx;
            this.constant = node.constant;
        }

        public void __copy_all(Node node)
        {
            this.type = node.type;
            this.state = node.state;
            this.children = new() { };
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
            foreach (var i in Range.Get(this.children.Count()))
            {
                this.children[i] = this.children[i].get_copy();
                this.children[i].__copy_children();
            }
        }

        public Node get_copy()
        {
            var n = this.__new_node(this.type);
            n.state = this.state;
            n.children = new() { };
            n.vname = this.vname;
            n.__vidx = this.__vidx;
            n.constant = this.constant;
            foreach (var child in this.children)
            {
                n.children.Add(child.get_copy());
            }
            return n;
        }

        public Node __get_shallow_copy()
        {
            var n = this.__new_node(this.type);
            n.state = this.state;
            n.children = new() { };
            n.vname = this.vname;
            n.__vidx = this.__vidx;
            n.constant = this.constant;
            n.children = new(this.children);
            return n;
        }

        public void __flatten()
        {
            if (this.ToString().Contains("2*(x|~y)-(x|y)-2*x-2-2*(x^y)+2+2*x+1+(x&y)-2*y+2+2*(x|y)-6-6*(x|~y)+4*(x&~y)"))
                Debugger.Break();
            if ((this.type) == (NodeType.INCL_DISJUNCTION))
            {
                this.__flatten_binary_generic();
            }
            else
            {
                if ((this.type) == (NodeType.EXCL_DISJUNCTION))
                {
                    this.__flatten_binary_generic();
                }
                else
                {
                    if ((this.type) == (NodeType.CONJUNCTION))
                    {
                        this.__flatten_binary_generic();
                    }
                    else
                    {
                        if ((this.type) == (NodeType.SUM))
                        {
                            this.__flatten_binary_generic();
                        }
                        else
                        {
                            if ((this.type) == (NodeType.PRODUCT))
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
            foreach (var i in Range.Get(((this.children.Count()) - (1)), -(1), -(1)))
            {
                var child = this.children[i];
                if ((child.type) != (this.type))
                {
                    continue;
                }
                this.children.RemoveAt(i);
                this.children.AddRange(child.children);
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
            while ((i) < (this.children.Count()))
            {
                var child = this.children[i];
                if ((child.type) != (this.type))
                {
                    i += 1;
                    continue;
                }
                changed = true;
                this.children.RemoveAt(i);
                if ((child.children[0].type) == (NodeType.CONSTANT))
                {
                    if ((((i) > (0)) && ((this.children[0].type) == (NodeType.CONSTANT))))
                    {
                        var prod = ((this.children[0].constant) * (child.children[0].constant));
                        this.children[0].__set_and_reduce_constant(prod);
                    }
                    else
                    {
                        this.children.Insert(0, child.children[0]);
                        i += 1;
                    }
                    child.children.RemoveAt(0);
                }
                //this.children.Slice(i, i, null) = child.children;
                child.children.InsertRange(i, child.children);
                i += child.children.Count();
            }
            if (!(changed))
            {
                return;
            }
            if ((this.children.Count()) > (1))
            {
                var first = this.children[0];
                if ((first.type) != (NodeType.CONSTANT))
                {
                    return;
                }
                if (!(first.__is_constant(1)))
                {
                    return;
                }
                this.children.RemoveAt(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
        }

        public void __check_duplicate_children()
        {
           // if (this.ToString() == "(-1^b)-5*b-13*(-1^b)")
             //   Debugger.Break();

            if ((this.type) == (NodeType.INCL_DISJUNCTION))
            {
                this.__remove_duplicate_children();
            }
            else
            {
                if ((this.type) == (NodeType.EXCL_DISJUNCTION))
                {
                    this.__remove_pairs_of_children();
                }
                else
                {
                    if ((this.type) == (NodeType.CONJUNCTION))
                    {
                        this.__remove_duplicate_children();
                    }
                    else
                    {
                        if ((this.type) == (NodeType.SUM))
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
            while ((i) < (this.children.Count()))
            {
                foreach (var j in Range.Get(((this.children.Count()) - (1)), i, -(1)))
                {
                    if (this.children[i].equals(this.children[j]))
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
            while ((i) < (this.children.Count()))
            {
                var range = Range.Get(((this.children.Count()) - (1)), i, -(1));
                foreach (var j in Range.Get(((this.children.Count()) - (1)), i, -(1)))
                {
                    if (this.children[i].equals(this.children[j]))
                    {
                        this.children.RemoveAt(j);
                        this.children.RemoveAt(i);
                        i -= 1;
                        break;
                    }
                }
                i += 1;
            }
            if ((this.children.Count()) == (0))
            {
                this.children = new() { this.__new_constant_node(0) };
            }
        }

        public void __merge_similar_nodes_sum()
        {
            Assert.True((this.type) == (NodeType.SUM));
            var i = 0;
            while ((i) < (((this.children.Count()) - (1))))
            {
                var j = ((i) + (1));
                while ((j) < (this.children.Count()))
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
                        this.children[i].children.Pop(0);
                        if ((this.children[i].children.Count()) == (1))
                        {
                            this.children[i] = this.children[i].children[0];
                        }
                    }
                    i += 1;
                }
            }
            if ((this.children.Count()) > (1))
            {
                return;
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
                return;
            }
            this.type = NodeType.CONSTANT;
            this.children = new() { };
            this.constant = 0;
        }

        public bool __is_zero_product()
        {
            return (((this.type) == (NodeType.PRODUCT)) && (this.children[0].__is_constant(0)));
        }

        public bool __has_factor_one()
        {
            return (((this.type) == (NodeType.PRODUCT)) && (this.children[0].__is_constant(1)));
        }

        public NullableI64 __get_opt_const_factor()
        {
            if ((((this.type) == (NodeType.PRODUCT)) && ((this.children[0].type) == (NodeType.CONSTANT))))
            {
                return this.children[0].constant;
            }
            return null;
        }

        public bool __try_merge_sum_children(int i, int j)
        {
            var child1 = this.children[i];
            var const1 = child1.__get_opt_const_factor();
            var child2 = this.children[j];
            var const2 = child2.__get_opt_const_factor();
            if (!(child1.__equals_neglecting_constants(child2, (const1) != (null), (const2) != (null))))
            {
                return false;
            }
            if ((const2) == (null))
            {
                const2 = 1;
            }
            if ((const1) == (null))
            {
                if ((child1.type) == (NodeType.PRODUCT))
                {
                    child1.children.Insert(0, this.__new_constant_node(((1) + (const2))));
                }
                else
                {
                    var c = this.__new_constant_node(((1) + (const2)));
                    this.children[i] = this.__new_node_with_children(NodeType.PRODUCT, new() { c, child1 });
                }
            }
            else
            {
                child1.children[0].constant += const2;
                child1.children[0].__reduce_constant();
            }
            return true;
        }

        public bool __equals_neglecting_constants(Node other, bool hasConst, bool hasConstOther)
        {
            Assert.True(((!(hasConst)) || ((this.type) == (NodeType.PRODUCT))));
            Assert.True(((!(hasConstOther)) || ((other.type) == (NodeType.PRODUCT))));
            Assert.True(((!(hasConst)) || ((this.children[0].type) == (NodeType.CONSTANT))));
            Assert.True(((!(hasConstOther)) || ((other.children[0].type) == (NodeType.CONSTANT))));
            if (hasConst)
            {
                if (hasConstOther)
                {
                    return this.__equals_neglecting_constants_both_const(other);
                }
                return other.__equals_neglecting_constants_other_const(this);
            }
            if (hasConstOther)
            {
                return this.__equals_neglecting_constants_other_const(other);
            }
            return this.equals(other);
        }

        public bool __equals_neglecting_constants_other_const(Node other)
        {
            Assert.True((other.type) == (NodeType.PRODUCT));
            Assert.True((other.children[0].type) == (NodeType.CONSTANT));
            Assert.True((((this.type) != (NodeType.PRODUCT)) || ((this.children[0].type) != (NodeType.CONSTANT))));
            if ((other.children.Count()) == (2))
            {
                return this.equals(other.children[1]);
            }
            if ((this.type) != (NodeType.PRODUCT))
            {
                return false;
            }
            if ((this.children.Count()) != (((other.children.Count()) - (1))))
            {
                return false;
            }
            List<int> oIndices = new(Range.Get(1, other.children.Count()));
            foreach (var child in this.children)
            {
                var found = false;
                foreach (var i in oIndices)
                {
                    if (child.equals(other.children[i]))
                    {
                        oIndices.Remove(i);
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

        public bool __equals_neglecting_constants_both_const(Node other)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            Assert.True((this.children[0].type) == (NodeType.CONSTANT));
            Assert.True((other.type) == (NodeType.PRODUCT));
            Assert.True((other.children[0].type) == (NodeType.CONSTANT));
            if ((this.children.Count()) != (other.children.Count()))
            {
                return false;
            }
            if ((this.children.Count()) == (2))
            {
                return this.children[1].equals(other.children[1]);
            }
            List<int> oIndices = new(Range.Get(1, other.children.Count()));
            foreach (var child in this.children.Slice(1, null, null))
            {
                var found = false;
                foreach (var i in oIndices)
                {
                    if (child.equals(other.children[i]))
                    {
                        oIndices.Remove(i);
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
            List<NodeType> types = new() { NodeType.INCL_DISJUNCTION, NodeType.EXCL_DISJUNCTION, NodeType.CONJUNCTION };
            if (((types).Contains(this.type)))
            {
                this.__resolve_inverse_nodes_bitwise();
            }
        }

        public void __resolve_inverse_nodes_bitwise()
        {
            var i = 0;
            while (true)
            {
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var neg1 = (child1.type) == (NodeType.NEGATION);
                foreach (var j in Range.Get(((i) + (1)), this.children.Count()))
                {
                    var child2 = this.children[j];
                    if (!(child1.__is_bitwise_inverse(child2)))
                    {
                        continue;
                    }
                    if ((((this.type) != (NodeType.EXCL_DISJUNCTION)) || ((this.children.Count()) == (2))))
                    {
                        this.copy(this.__new_constant_node(((this.type) == (NodeType.CONJUNCTION)) ? 0 : -(1)));
                        return;
                    }
                    this.children.RemoveAt(j);
                    this.children.RemoveAt(i);
                    if ((this.children[0].type) == (NodeType.CONSTANT))
                    {
                        this.children[0].__set_and_reduce_constant(((-(1)) ^ (this.children[0].constant)));
                        i -= 1;
                    }
                    else
                    {
                        this.children.Insert(0, this.__new_constant_node(-(1)));
                    }
                    if ((this.children.Count()) == (1))
                    {
                        this.copy(this.children[0]);
                        return;
                    }
                    break;
                }
                i += 1;
            }
        }

        public bool __is_bitwise_inverse(Node other)
        {
            if ((this.type) == (NodeType.NEGATION))
            {
                if ((other.type) == (NodeType.NEGATION))
                {
                    return this.children[0].__is_bitwise_inverse(other.children[0]);
                }
                return this.children[0].equals(other);
            }
            if ((other.type) == (NodeType.NEGATION))
            {
                return this.equals(other.children[0]);
            }
            var node = this.get_copy();
            if ((((node.type) == (NodeType.PRODUCT)) && ((node.children.Count()) == (2)) && ((node.children[0].type) == (NodeType.CONSTANT))))
            {
                if ((node.children[1].type) == (NodeType.SUM))
                {
                    foreach (var n in node.children[1].children)
                    {
                        n.__multiply(node.children[0].constant);
                    }
                    node.copy(node.children[1]);
                }
            }
            var onode = other.get_copy();
            if ((((onode.type) == (NodeType.PRODUCT)) && ((onode.children.Count()) == (2)) && ((onode.children[0].type) == (NodeType.CONSTANT))))
            {
                if ((onode.children[1].type) == (NodeType.SUM))
                {
                    foreach (var n in onode.children[1].children)
                    {
                        n.__multiply(onode.children[0].constant);
                    }
                    onode.copy(onode.children[1]);
                }
            }
            if ((node.type) == (NodeType.SUM))
            {
                if ((onode.type) != (NodeType.SUM))
                {
                    if ((((node.children.Count()) > (2)) || (!(node.children[0].__is_constant(-(1))))))
                    {
                        return false;
                    }
                    node.children[1].__multiply_by_minus_one();
                    return node.children[1].equals(onode);
                }
                foreach (var child in node.children)
                {
                    child.__multiply_by_minus_one();
                }
                if ((node.children[0].type) == (NodeType.CONSTANT))
                {
                    node.children[0].constant -= 1;
                    node.children[0].__reduce_constant();
                    if (node.children[0].__is_constant(0))
                    {
                        node.children.RemoveAt(0);
                        Assert.True((node.children.Count()) >= (1));
                        if ((this.children.Count()) == (1))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    node.children.Insert(0, this.__new_constant_node(-(1)));
                }
                return node.equals(onode);
            }
            if ((onode.type) != (NodeType.SUM))
            {
                return false;
            }
            if ((((onode.children.Count()) > (2)) || (!(onode.children[0].__is_constant(-(1))))))
            {
                return false;
            }
            onode.children[1].__multiply_by_minus_one();
            return onode.children[1].equals(node);
        }

        public void __remove_trivial_nodes()
        {
            if ((this.type) < (NodeType.PRODUCT))
            {
                return;
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
        }

        public bool __refine_step_2(Node parent = null, bool restrictedScope = false)
        {
            var changed = false;
            if (!(restrictedScope))
            {
                foreach (var c in this.children)
                {
                    if (c.__refine_step_2(this))
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
            Node child = null;
            Node node = null;
            if ((this.type) == (NodeType.NEGATION))
            {
                child = this.children[0];
                if ((child.type) == (NodeType.NEGATION))
                {
                    this.copy(child.children[0]);
                    return true;
                }
                node = child.__get_opt_transformed_negated();
                if ((node) != (null))
                {
                    this.copy(node);
                    return true;
                }
                if ((((child.type) == (NodeType.SUM)) && (child.children[0].__is_constant(-(1)))))
                {
                    this.type = NodeType.SUM;
                    this.children = child.children.Slice(1, null, null);
                    foreach (var newChild in this.children)
                    {
                        child = newChild;
                        child.__multiply_by_minus_one();
                    }
                    Assert.True((this.children.Count()) >= (1));
                    if ((this.children.Count()) == (1))
                    {
                        this.copy(this.children[0]);
                    }
                    return true;
                }
                return false;
            }
            child = this.__get_opt_transformed_negated();
            if ((child) == (null))
            {
                return false;
            }
            if ((child.type) == (NodeType.NEGATION))
            {
                this.copy(child.children[0]);
                return true;
            }
            node = child.__get_opt_transformed_negated();
            if ((node) != (null))
            {
                this.copy(node);
                return true;
            }
            return false;
        }

        public void __multiply(long factor)
        {
            if ((((((factor) - (1))) % (this.__modulus))) == (0))
            {
                return;
            }
            if ((this.type) == (NodeType.CONSTANT))
            {
                this.__set_and_reduce_constant(((this.constant) * (factor)));
                return;
            }
            if ((this.type) == (NodeType.SUM))
            {
                foreach (var child in this.children)
                {
                    child.__multiply(factor);
                }
                return;
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                if ((this.children[0].type) == (NodeType.PRODUCT))
                {
                    //this.children.Slice(1, 1, null) = this.children[0].children.Slice(1, null, null);
                    children.InsertRange(1, this.children[0].children.Slice(1, null, null));
                    this.children[0] = this.children[0].children[0];
                }
                if ((this.children[0].type) == (NodeType.CONSTANT))
                {
                    this.children[0].__multiply(factor);
                    if (this.children[0].__is_constant(1))
                    {
                        this.children.RemoveAt(0);
                        if ((this.children.Count()) == (1))
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
            node.copy(this);
            List<Node> prod_children = new() { fac, node };
            var prod = this.__new_node_with_children(NodeType.PRODUCT, prod_children);
            this.copy(prod);
        }

        public void __multiply_by_minus_one()
        {
            this.__multiply(-(1));
        }

        public Node __get_opt_transformed_negated()
        {
            if ((this.type) == (NodeType.SUM))
            {
                return this.__get_opt_transformed_negated_sum();
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                return this.__get_opt_transformed_negated_product();
            }
            return null;
        }

        public Node __get_opt_transformed_negated_sum()
        {
            Assert.True((this.type) == (NodeType.SUM));
            if ((this.children.Count()) < (2))
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var res = this.__new_node(NodeType.SUM);
            foreach (var i in Range.Get(1, this.children.Count()))
            {
                var child = this.children[i];
                var hasMinusOne = (((child.type) == (NodeType.PRODUCT)) && (child.children[0].__is_constant(-(1))));
                if (hasMinusOne)
                {
                    Assert.True((child.children.Count()) > (1));
                    if ((child.children.Count()) == (2))
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
            Assert.True((res.children.Count()) > (0));
            if ((res.children.Count()) == (1))
            {
                return res.children[0];
            }
            return res;
        }

        public Node __get_opt_transformed_negated_product()
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var child1 = this.children[1];
            if ((child1.type) != (NodeType.SUM))
            {
                return null;
            }
            if (!(child1.children[0].__is_constant(1)))
            {
                return null;
            }
            if ((child1.children.Count()) < (2))
            {
                return null;
            }
            if ((child1.children.Count()) == (2))
            {
                return child1.children[1];
            }
            return this.__new_node_with_children(NodeType.SUM, child1.children.Slice(1, null, null));
        }

        public bool __check_bitwise_negations(Node parent)
        {
            if ((this.type) == (NodeType.NEGATION))
            {
                if ((((parent) != (null)) && (parent.__is_bitwise_op())))
                {
                    return false;
                }
                if ((this.children[0].type) == (NodeType.PRODUCT))
                {
                    this.__substitute_bitwise_negation_product();
                    return true;
                }
                if ((this.children[0].type) == (NodeType.SUM))
                {
                    this.__substitute_bitwise_negation_sum();
                    return true;
                }
                return this.__substitute_bitwise_negation_generic(parent);
            }
            if ((((parent) == (null)) || (!(parent.__is_bitwise_op()))))
            {
                return false;
            }
            var child = this.__get_opt_transformed_negated();
            if ((child) == (null))
            {
                return false;
            }
            this.type = NodeType.NEGATION;
            this.children = new() { child };
            return true;
        }

        public bool __is_bitwise_op()
        {
            return (((this.type) == (NodeType.NEGATION)) || (this.__is_bitwise_binop()));
        }

        public bool __is_bitwise_binop()
        {
            List<NodeType> types = new() { NodeType.CONJUNCTION, NodeType.EXCL_DISJUNCTION, NodeType.INCL_DISJUNCTION };
            return ((types).Contains(this.type));
        }

        public bool __is_arithm_op()
        {
            List<NodeType> types = new() { NodeType.SUM, NodeType.PRODUCT, NodeType.POWER };
            return ((types).Contains(this.type));
        }

        public void __substitute_bitwise_negation_product()
        {
            this.type = NodeType.SUM;
            this.children.Insert(0, this.__new_constant_node(-(1)));
            var child = this.children[1];
            if ((child.children[0].type) == (NodeType.CONSTANT))
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
            if ((child.children[0].type) == (NodeType.CONSTANT))
            {
                child.children[0].__set_and_reduce_constant(((child.children[0].constant) + (1)));
            }
            else
            {
                child.children.Insert(0, this.__new_constant_node(1));
            }
        }

        public bool __substitute_bitwise_negation_generic(Node parent)
        {
            Assert.True((this.type) == (NodeType.NEGATION));
            if ((parent) == (null))
            {
                return false;
            }
            if ((((parent.type) != (NodeType.SUM)) && ((parent.type) != (NodeType.PRODUCT))))
            {
                return false;
            }
            if ((parent.type) == (NodeType.PRODUCT))
            {
                if ((((parent.children.Count()) > (2)) || ((parent.children[0].type) != (NodeType.CONSTANT))))
                {
                    return false;
                }
            }
            List<Node> prod_children = new() { this.__new_constant_node(-(1)), this.children[0] };
            var prod = this.__new_node_with_children(NodeType.PRODUCT, prod_children);
            this.type = NodeType.SUM;
            this.children = new() { this.__new_constant_node(-(1)), prod };
            return true;
        }

        public bool __check_bitwise_powers_of_two()
        {
            if (!(this.__is_bitwise_binop()))
            {
                return false;
            }
            var e = this.__get_max_factor_power_of_two_in_children();
            if ((e) <= (0))
            {
                return false;
            }
            NullableI64 c = null;
            if ((this.children[0].type) == (NodeType.CONSTANT))
            {
                c = this.children[0].constant;
            }
            NullableI64 add = null;
            foreach (var child in this.children)
            {
                var rem = child.__divide_by_power_of_two(e);
                if ((add) == (null))
                {
                    add = rem;
                }
                else
                {
                    if ((this.type) == (NodeType.CONJUNCTION))
                    {
                        add &= rem;
                    }
                    else
                    {
                        if ((this.type) == (NodeType.INCL_DISJUNCTION))
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
            prod.children = new() { this.__new_constant_node(LongPower(2, e)), this.__get_shallow_copy() };
            this.copy(prod);
            if ((((add) % (this.__modulus))) != (0))
            {
                var constNode = this.__new_constant_node(add);
                List<Node> sum_children = new() { constNode, this.__get_shallow_copy() };
                var sumNode = this.__new_node_with_children(NodeType.SUM, sum_children);
                this.copy(sumNode);
            }
            return true;
        }

        public long __get_max_factor_power_of_two_in_children(bool allowRem = true)
        {
            Assert.True((this.children.Count()) > (1));
            Assert.True(((this.__is_bitwise_binop()) || ((this.type) == (NodeType.SUM))));
            var withNeg = ((allowRem) && (this.__is_bitwise_binop()));
            var maxe = this.children[0].__get_max_factor_power_of_two(withNeg);
            if (((allowRem) && ((this.children[0].type) == (NodeType.CONSTANT))))
            {
                maxe = -(1);
            }
            if ((maxe) == (0))
            {
                return 0;
            }
            foreach (var child in this.children.Slice(1, null, null))
            {
                var e = child.__get_max_factor_power_of_two(withNeg);
                if ((e) == (0))
                {
                    return 0;
                }
                if ((e) == (-(1)))
                {
                    continue;
                }
                maxe = ((maxe) == (-(1))) ? e : Math.Min(maxe, e);
            }
            return maxe;
        }

        public long __get_max_factor_power_of_two(bool allowRem)
        {
            if ((this.type) == (NodeType.CONSTANT))
            {
                return trailing_zeros(this.constant);
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                return this.children[0].__get_max_factor_power_of_two(false);
            }
            if ((this.type) == (NodeType.SUM))
            {
                return this.__get_max_factor_power_of_two_in_children(allowRem);
            }
            if (((allowRem) && ((this.type) == (NodeType.NEGATION))))
            {
                return this.children[0].__get_max_factor_power_of_two(false);
            }
            return 0;
        }

        public long __divide_by_power_of_two(long ee)
        {
            var e = Convert.ToInt32(ee);
            if ((this.type) == (NodeType.CONSTANT))
            {
                var orig = this.constant;
                this.constant >>= e;
                return ((orig) - (((this.constant) << (e))));
            }
            long rem = 0;
            if ((this.type) == (NodeType.PRODUCT))
            {
                rem = this.children[0].__divide_by_power_of_two(e);
                Assert.True((rem) == (0));
                if (this.children[0].__is_constant(1))
                {
                    this.children.RemoveAt(0);
                    if ((this.children.Count()) == (1))
                    {
                        this.copy(this.children[0]);
                    }
                }
                return 0;
            }
            if ((this.type) == (NodeType.SUM))
            {
                long add = 0;
                foreach (var child in this.children)
                {
                    rem = child.__divide_by_power_of_two(e);
                    if ((rem) != (0))
                    {
                        Assert.True((add) == (0));
                        add = rem;
                    }
                }
                if (this.children[0].__is_constant(0))
                {
                    this.children.RemoveAt(0);
                    if ((this.children.Count()) == (1))
                    {
                        this.copy(this.children[0]);
                    }
                }
                return add;
            }
            Assert.True((this.type) == (NodeType.NEGATION));
            rem = this.children[0].__divide_by_power_of_two(e);
            Assert.True((rem) == (0));
            return ((((1) << (e))) - (1));
        }

        public bool __check_beautify_constants_in_products()
        {
            if (this.ToString() == "6*(-1^-6+7-7*b-12*~b-5*b^c)")
                Debugger.Break();


            if ((this.type) != (NodeType.PRODUCT))
            {
                if (this.ToString() == "6*(-1^-6+7-7*b-12*~b-5*b^c)")
                    Debugger.Break();
                return false;
            }
            if ((this.children[0].type) != (NodeType.CONSTANT))
            {
                if (this.ToString() == "6*(-1^-6+7-7*b-12*~b-5*b^c)")
                    Debugger.Break();
                return false;
            }
            var e = trailing_zeros(this.children[0].constant);
            //var e = this.children[0].constant.Shor
            if ((e) <= (0))
            {
                if (this.ToString() == "6*(-1^-6+7-7*b-12*~b-5*b^c)")
                    Debugger.Break();
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

            if (this.ToString() == "6*(-1^-6+7-7*b-12*~b-5*b^c)")
                Debugger.Break();
            return changed;
        }

        public bool __check_beautify_constants(long ee)
        {
            if (ee > int.MaxValue)
                throw new InvalidOperationException();

            var e = Convert.ToInt32(ee);
            List<NodeType> types = new() { NodeType.SUM, NodeType.PRODUCT };
            if (((this.__is_bitwise_op()) || (((types).Contains(this.type)))))
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
            if ((this.type) != (NodeType.CONSTANT))
            {
                return false;
            }
            var orig = this.constant;
            var mask = ((((-(1)) % (this.__modulus))) >> (e));
            var b = ((this.constant) & (((this.__modulus) >> (((e) + (1))))));
            Console.WriteLine($"orig: {orig}");
            Console.WriteLine($"mask: {mask}");
            Console.WriteLine($"b: {b}");
            Console.WriteLine($"popcnt cond: {(((popcount(this.constant)) > (1)) || ((b) == (1)))}");
            this.constant &= mask;
            if ((b) > (0))
            {
                if ((((popcount(this.constant)) > (1)) || ((b) == (1))))
                {
                    this.constant |= ~(mask);
                }
            }
            this.__reduce_constant();
            return (this.constant) != (orig);
        }

        public bool __check_move_in_bitwise_negations()
        {
            if ((this.type) != (NodeType.NEGATION))
            {
                return false;
            }
            var childType = this.children[0].type;
            if ((childType) == (NodeType.EXCL_DISJUNCTION))
            {
                return this.__check_move_in_bitwise_negation_excl_disj();
            }
            List<NodeType> types = new() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION };
            if (((types).Contains(childType)))
            {
                return this.__check_move_in_bitwise_negation_conj_or_incl_disj();
            }
            return false;
        }

        public bool __check_move_in_bitwise_negation_conj_or_incl_disj()
        {
            Assert.True((this.type) == (NodeType.NEGATION));
            var child = this.children[0];
            if (!(child.__is_any_child_negated()))
            {
                return false;
            }
            child.__negate_all_children();
            child.type = ((child.type) == (NodeType.CONJUNCTION)) ? NodeType.INCL_DISJUNCTION : NodeType.CONJUNCTION;
            this.copy(child);
            return true;
        }

        public bool __is_any_child_negated()
        {
            foreach (var child in this.children)
            {
                if ((child.type) == (NodeType.NEGATION))
                {
                    return true;
                }
                var node = child.__get_opt_transformed_negated();
                if ((node) != (null))
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
            if ((this.type) == (NodeType.NEGATION))
            {
                this.copy(this.children[0]);
                return;
            }
            var node = this.__get_opt_transformed_negated();
            if ((node) != (null))
            {
                this.copy(node);
                return;
            }
            List<Node> neg_children = new() { this.get_copy() };
            node = this.__new_node_with_children(NodeType.NEGATION, neg_children);
            this.copy(node);
        }

        public bool __check_move_in_bitwise_negation_excl_disj()
        {
            Assert.True((this.type) == (NodeType.NEGATION));
            var child = this.children[0];
            var (n, _) = child.__get_recursively_negated_child();
            if ((n) == (null))
            {
                return false;
            }
            n.__negate();
            this.copy(child);
            return true;
        }

        public (Node, NullableI32) __get_recursively_negated_child(NullableI32 maxDepth = null)
        {
            if ((this.type) == (NodeType.NEGATION))
            {
                return (this, 0);
            }
            var node = this.__get_opt_transformed_negated();
            if ((node) != (null))
            {
                return (this, 0);
            }
            if ((((maxDepth) != (null)) && ((maxDepth) == (0))))
            {
                return (null, null);
            }
            if (!(this.__is_bitwise_binop()))
            {
                return (null, null);
            }
            NullableI32 opt = null;
            Node candidate = null;
            NullableI32 nextMax = ((maxDepth) == (null)) ? null : ((maxDepth) - (1));
            foreach (var child in this.children)
            {
                var (_, d) = child.__get_recursively_negated_child(nextMax);
                if ((d) == (null))
                {
                    continue;
                }
                if ((maxDepth) == (null))
                {
                    return (child, ((d) + (1)));
                }
                Assert.True((((opt) == (null)) || ((d) < (opt))));
                opt = d;
                candidate = child;
                nextMax = ((opt) - (1));
            }
            return (candidate, opt);
        }

        public bool __check_bitwise_negations_in_excl_disjunctions()
        {
            if ((this.type) != (NodeType.EXCL_DISJUNCTION))
            {
                return false;
            }
            Node neg = null;
            var changed = false;
            foreach (var child in this.children)
            {
                if (!(child.__is_negated()))
                {
                    continue;
                }
                if ((neg) == (null))
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
            if ((this.type) == (NodeType.NEGATION))
            {
                return true;
            }
            var node = this.__get_opt_transformed_negated();
            return (node) != (null);
        }

        public bool __check_rewrite_powers(Node parent)
        {
            if ((this.type) != (NodeType.POWER))
            {
                return false;
            }
            var exp = this.children[1];
            if ((exp.type) != (NodeType.CONSTANT))
            {
                return false;
            }
            var _base = this.children[0];
            if ((_base.type) != (NodeType.PRODUCT))
            {
                return false;
            }
            if ((_base.children[0].type) != (NodeType.CONSTANT))
            {
                return false;
            }
            var _const = this.__power(_base.children[0].constant, exp.constant);
            _base.children.RemoveAt(0);
            if ((_base.children.Count()) == (1))
            {
                _base.copy(_base.children[0]);
            }
            if ((_const) == (1))
            {
                return true;
            }
            if ((((parent) != (null)) && ((parent.type) == (NodeType.PRODUCT))))
            {
                if ((parent.children[0].type) == (NodeType.PRODUCT))
                {
                    parent.children[0].__set_and_reduce_constant(((parent.children[0].constant) * (_const)));
                }
                else
                {
                    parent.children.Insert(0, this.__new_constant_node(_const));
                }
            }
            else
            {
                var prod = this.__new_node(NodeType.PRODUCT);
                prod.children.Add(this.__new_constant_node(_const));
                prod.children.Add(this.__get_shallow_copy());
                this.copy(prod);
            }
            return true;
        }

        public bool __check_resolve_product_of_powers()
        {
            if ((this.type) != (NodeType.PRODUCT))
            {
                return false;
            }
            var changed = false;
            var start = Convert.ToInt32((this.children[0].type) == (NodeType.CONSTANT));
            foreach (var i in Range.Get(((this.children.Count()) - (1)), start, -(1)))
            {
                var child = this.children[i];
                var merged = false;
                foreach (var j in Range.Get(start, i))
                {
                    var child2 = this.children[j];
                    if ((child2.type) == (NodeType.POWER))
                    {
                        var base2 = child2.children[0];
                        var exp2 = child2.children[1];
                        if (base2.equals(child))
                        {
                            exp2.__add_constant(1);
                            this.children.RemoveAt(i);
                            changed = true;
                            break;
                        }
                        if ((((child.type) == (NodeType.POWER)) && (base2.equals(child.children[0]))))
                        {
                            exp2.__add(child.children[1]);
                            this.children.RemoveAt(i);
                            changed = true;
                            break;
                        }
                    }
                    if ((child.type) == (NodeType.POWER))
                    {
                        var _base = child.children[0];
                        var exp = child.children[1];
                        if (_base.equals(child2))
                        {
                            exp.__add_constant(1);
                            this.children[j] = this.children[i];
                            this.children.RemoveAt(i);
                            changed = true;
                        }
                        break;
                    }
                    if (child.equals(child2))
                    {
                        List<Node> power_children = new() { child, this.__new_constant_node(2) };
                        this.children[j] = this.__new_node_with_children(NodeType.POWER, power_children);
                        this.children.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public void __add(Node other)
        {
            Node node = null;
            if ((this.type) == (NodeType.CONSTANT))
            {
                var constant = this.constant;
                this.copy(other.get_copy());
                this.__add_constant(constant);
                return;
            }
            if ((other.type) == (NodeType.CONSTANT))
            {
                this.__add_constant(other.constant);
                return;
            }
            if ((this.type) == (NodeType.SUM))
            {
                this.__add_to_sum(other);
                return;
            }
            if ((other.type) == (NodeType.SUM))
            {
                node = other.get_copy();
                node.__add_to_sum(this);
                this.copy(node);
                return;
            }
            List<Node> sum_children = new() { this.get_copy(), other.get_copy() };
            node = this.__new_node_with_children(NodeType.SUM, sum_children);
            this.copy(node);
            this.__merge_similar_nodes_sum();
        }

        public void __add_constant(long constant)
        {
            if ((this.type) == (NodeType.CONSTANT))
            {
                this.__set_and_reduce_constant(((this.constant) + (constant)));
                return;
            }
            if ((this.type) == (NodeType.SUM))
            {
                if ((((this.children.Count()) > (0)) && ((this.children[0].type) == (NodeType.CONSTANT))))
                {
                    this.children[0].__add_constant(constant);
                    return;
                }
                this.children.Insert(0, this.__new_constant_node(constant));
                return;
            }
            List<Node> sum_children = new() { this.__new_constant_node(constant), this.get_copy() };
            var node = this.__new_node_with_children(NodeType.SUM, sum_children);
            this.copy(node);
        }

        public void __add_to_sum(Node other)
        {
            Assert.True((this.type) == (NodeType.SUM));
            Assert.True((other.type) != (NodeType.CONSTANT));
            if ((other.type) == (NodeType.SUM))
            {
                foreach (var ochild in other.children)
                {
                    if ((ochild.type) == (NodeType.CONSTANT))
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
            if ((this.type) != (NodeType.PRODUCT))
            {
                return false;
            }
            if ((this.children.Count()) < (2))
            {
                return false;
            }
            var child0 = this.children[0];
            if ((child0.type) != (NodeType.CONSTANT))
            {
                return false;
            }
            var child1 = this.children[1];
            if ((child1.type) != (NodeType.SUM))
            {
                return false;
            }
            var constant = child0.constant;
            var sumNode = this;
            if ((this.children.Count()) == (2))
            {
                this.copy(child1);
            }
            else
            {
                this.children.RemoveAt(0);
                sumNode = this.children[0];
            }
            foreach (var i in Range.Get(sumNode.children.Count()))
            {
                if ((sumNode.children[i].type) == (NodeType.CONSTANT))
                {
                    sumNode.children[i].__set_and_reduce_constant(((sumNode.children[i].constant) * (constant)));
                }
                else
                {
                    if ((sumNode.children[i].type) == (NodeType.PRODUCT))
                    {
                        var first = sumNode.children[i].children[0];
                        if ((first.type) == (NodeType.CONSTANT))
                        {
                            first.__set_and_reduce_constant(((first.constant) * (constant)));
                        }
                        else
                        {
                            sumNode.children[i].children.Insert(0, sumNode.__new_constant_node(constant));
                        }
                    }
                    else
                    {
                        List<Node> factors = new() { sumNode.__new_constant_node(constant), sumNode.children[i] };
                        sumNode.children[i] = sumNode.__new_node_with_children(NodeType.PRODUCT, factors);
                    }
                }
            }
            return true;
        }

        public bool __check_factor_out_of_sum()
        {
            if ((((this.type) != (NodeType.SUM)) || ((this.children.Count()) <= (1))))
            {
                return false;
            }
            List<Node> factors = new() { };
            while (true)
            {
                var factor = this.__try_factor_out_of_sum();
                if ((factor) == (null))
                {
                    break;
                }
                factors.Add(factor);
            }
            if ((factors.Count()) == (0))
            {
                return false;
            }
            List<Node> prod_children = factors.ToList();
            prod_children.Add(this.get_copy());
            var prod = this.__new_node_with_children(NodeType.PRODUCT, prod_children);
            this.copy(prod);
            return true;
        }

        public Node __try_factor_out_of_sum()
        {
            Assert.True((this.type) == (NodeType.SUM));
            Assert.True((this.children.Count()) > (1));
            var factor = this.__get_common_factor_in_sum();
            if ((factor) == (null))
            {
                return null;
            }
            foreach (var child in this.children)
            {
                child.__eliminate_factor(factor);
            }
            return factor;
        }

        public Node __get_common_factor_in_sum()
        {
            Assert.True((this.type) == (NodeType.SUM));
            var first = this.children[0];
            Node exp = null;
            Node _base = null;
            if ((first.type) == (NodeType.PRODUCT))
            {
                foreach (var child in first.children)
                {
                    if ((child.type) == (NodeType.CONSTANT))
                    {
                        continue;
                    }
                    if (this.__has_factor_in_remaining_children(child))
                    {
                        return child.get_copy();
                    }
                    if ((child.type) == (NodeType.POWER))
                    {
                        exp = child.children[1];
                        if ((((exp.type) == (NodeType.CONSTANT)) && (!(exp.__is_constant(0)))))
                        {
                            _base = child.children[0];
                            if (this.__has_factor_in_remaining_children(_base))
                            {
                                return _base.get_copy();
                            }
                        }
                    }
                }
                return null;
            }
            if ((first.type) == (NodeType.POWER))
            {
                exp = first.children[1];
                if ((((exp.type) == (NodeType.CONSTANT)) && (!(exp.__is_constant(0)))))
                {
                    _base = first.children[0];
                    if ((((_base.type) != (NodeType.CONSTANT)) && (this.__has_factor_in_remaining_children(_base))))
                    {
                        return _base.get_copy();
                    }
                }
                return null;
            }
            if ((((first.type) != (NodeType.CONSTANT)) && (this.__has_factor_in_remaining_children(first))))
            {
                return first.get_copy();
            }
            return null;
        }

        public bool __has_factor_in_remaining_children(Node factor)
        {
            Assert.True((this.type) == (NodeType.SUM));
            foreach (var child in this.children.Slice(1, null, null))
            {
                if (!(child.__has_factor(factor)))
                {
                    return false;
                }
            }
            return true;
        }

        public bool __has_factor(Node factor)
        {
            if ((this.type) == (NodeType.PRODUCT))
            {
                return this.__has_factor_product(factor);
            }
            if ((this.type) == (NodeType.POWER))
            {
                var exp = this.children[1];
                if ((((exp.type) == (NodeType.CONSTANT)) && (!(exp.__is_constant(0)))))
                {
                    return this.children[0].equals(factor);
                }
            }
            return this.equals(factor);
        }

        public bool __has_factor_product(Node factor)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            foreach (var child in this.children)
            {
                if (child.equals(factor))
                {
                    return true;
                }
                if ((child.type) == (NodeType.POWER))
                {
                    var exp = child.children[1];
                    if ((((exp.type) == (NodeType.CONSTANT)) && (!(exp.__is_constant(0)))))
                    {
                        if (child.children[0].equals(factor))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool __has_child(Node node)
        {
            return (this.__get_index_of_child(node)) != (null);
        }

        public NullableI32 __get_index_of_child(Node node)
        {
            foreach (var i in Range.Get(this.children.Count()))
            {
                if (this.children[i].equals(node))
                {
                    return i;
                }
            }
            return null;
        }

        public NullableI32 __get_index_of_child_negated(Node node)
        {
            foreach (var i in Range.Get(this.children.Count()))
            {
                if (this.children[i].equals_negated(node))
                {
                    return i;
                }
            }
            return null;
        }

        public void __eliminate_factor(Node factor)
        {
            if ((this.type) == (NodeType.PRODUCT))
            {
                this.__eliminate_factor_product(factor);
                return;
            }
            if ((this.type) == (NodeType.POWER))
            {
                this.__eliminate_factor_power(factor);
                return;
            }
            Assert.True(this.equals(factor));
            var c = this.__new_constant_node(1);
            this.copy(c);
        }

        public void __eliminate_factor_product(Node factor)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child = this.children[i];
                if (child.equals(factor))
                {
                    this.children.RemoveAt(i);
                    if ((this.children.Count()) == (1))
                    {
                        this.copy(this.children[0]);
                    }
                    return;
                }
                if ((((child.type) == (NodeType.POWER)) && (child.children[0].equals(factor))))
                {
                    child.__decrement_exponent();
                    return;
                }
            }
            Assert.True(false);
        }

        public void __eliminate_factor_power(Node factor)
        {
            Assert.True((this.type) == (NodeType.POWER));
            if (this.equals(factor))
            {
                this.copy(this.__new_constant_node(1));
                return;
            }
            Assert.True(this.children[0].equals(factor));
            this.__decrement_exponent();
        }

        public void __decrement_exponent()
        {
            Assert.True((this.type) == (NodeType.POWER));
            Assert.True((this.children.Count()) == (2));
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

        public bool __check_resolve_inverse_negations_in_sum()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            var changed = false;
            var _const = 0;
            var i = this.children.Count();
            while ((i) > (1))
            {
                i -= 1;
                var first = this.children[i];
                foreach (var j in Range.Get(i))
                {
                    var second = this.children[j];
                    if (first.equals_negated(second))
                    {
                        this.children.RemoveAt(i);
                        this.children.RemoveAt(j);
                        i -= 1;
                        _const -= 1;
                        changed = true;
                        break;
                    }
                    if ((first.type) != (NodeType.PRODUCT))
                    {
                        continue;
                    }
                    if ((second.type) != (NodeType.PRODUCT))
                    {
                        continue;
                    }
                    var indices = first.__get_only_differing_child_indices(second);
                    if ((indices) == (null))
                    {
                        continue;
                    }
                    var (firstIdx, secIdx) = indices;
                    if (first.children[firstIdx].equals_negated(second.children[secIdx]))
                    {
                        this.children.RemoveAt(i);
                        second.children.RemoveAt(secIdx);
                        if ((second.children.Count()) == (1))
                        {
                            second.copy(second.children[0]);
                        }
                        second.__multiply_by_minus_one();
                        changed = true;
                        break;
                    }
                }
            }
            if ((((this.children.Count()) > (0)) && ((this.children[0].type) == (NodeType.CONSTANT))))
            {
                this.children[0].__set_and_reduce_constant(((this.children[0].constant) + (_const)));
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(_const));
            }
            if ((((this.children.Count()) > (1)) && (this.children[0].__is_constant(0))))
            {
                this.children.RemoveAt(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            else
            {
                if ((this.children.Count()) == (0))
                {
                    this.copy(this.__new_constant_node(0));
                }
            }
            return changed;
        }

        public bool expand(bool restrictedScope = false)
        {
            List<NodeType> types = new() { NodeType.SUM, NodeType.PRODUCT, NodeType.POWER };
            if (((restrictedScope) && (!((types).Contains(this.type)))))
            {
                return false;
            }
            var changed = false;
            if (((restrictedScope) && ((this.type) == (NodeType.POWER))))
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
                if ((this.type) == (NodeType.SUM))
                {
                    this.__flatten_binary_generic();
                }
            }
            if (this.__check_expand())
            {
                changed = true;
            }
            if (((changed) && ((this.type) == (NodeType.SUM))))
            {
                this.__merge_similar_nodes_sum();
            }
            return changed;
        }

        public bool __check_expand()
        {
            if ((this.type) == (NodeType.PRODUCT))
            {
                return this.__check_expand_product();
            }
            if ((this.type) == (NodeType.POWER))
            {
                return this.__check_expand_power();
            }
            return false;
        }

        public bool __check_expand_product()
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            Assert.True((this.children.Count()) > (0));
            if ((this.children.Count()) == (1))
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
            return (this.__get_first_sum_index()) != (null);
        }

        public NullableI32 __get_first_sum_index()
        {
            foreach (var i in Range.Get(this.children.Count()))
            {
                if ((this.children[i].type) == (NodeType.SUM))
                {
                    return i;
                }
            }
            return null;
        }

        public void __expand_product()
        {
            Node node = null;
            while (true)
            {
                var sumIdx = this.__get_first_sum_index();
                if ((sumIdx) == (null))
                {
                    break;
                }
                node = this.children[sumIdx].get_copy();
                Assert.True((node.type) == (NodeType.SUM));
                var repeat = false;
                foreach (var i in Range.Get(this.children.Count()))
                {
                    if ((i) == (sumIdx))
                    {
                        continue;
                    }
                    node.__multiply_sum(this.children[i]);
                    if (node.__is_constant(0))
                    {
                        break;
                    }
                    if ((node.type) != (NodeType.SUM))
                    {
                        this.children[sumIdx] = node;
                        foreach (var j in Range.Get(i, -(1), -(1)))
                        {
                            if ((j) == (sumIdx))
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
            if ((node.children.Count()) == (1))
            {
                this.copy(node.children[0]);
            }
            else
            {
                this.copy(node);
            }
        }

        public void __multiply_sum(Node other)
        {
            Assert.True((this.type) == (NodeType.SUM));
            if ((other.type) == (NodeType.SUM))
            {
                this.__multiply_sum_with_sum(other, true);
                return;
            }
            long constant = 0;
            foreach (var i in Range.Get(((this.children.Count()) - (1)), -(1), -(1)))
            {
                var child = this.children[i];
                child.__multiply_with_node_no_sum(other);
                if ((child.type) == (NodeType.CONSTANT))
                {
                    constant = this.__get_reduced_constant(((constant) + (child.constant)));
                    if ((i) > (0))
                    {
                        this.children.RemoveAt(i);
                    }
                    continue;
                }
            }
            if ((this.children[0].type) == (NodeType.CONSTANT))
            {
                if ((constant) == (0))
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
                if ((constant) != (0))
                {
                    this.children.Insert(0, this.__new_constant_node(constant));
                }
            }
            this.__merge_similar_nodes_sum();
            if ((((this.type) == (NodeType.SUM)) && ((this.children.Count()) == (0))))
            {
                this.copy(this.__new_constant_node(0));
            }
        }

        public void __multiply_sum_with_sum(Node other, bool keepSum = false)
        {
            Assert.True((this.type) == (NodeType.SUM));
            Assert.True((other.type) == (NodeType.SUM));
            List<Node> children = new(this.children);
            this.children = new() { };
            foreach (var child in children)
            {
                foreach (var ochild in other.children)
                {
                    var prod = child.__get_product_with_node(ochild);
                    if ((prod.type) == (NodeType.CONSTANT))
                    {
                        if (prod.__is_constant(0))
                        {
                            continue;
                        }
                        if ((((this.children.Count()) > (0)) && ((this.children[0].type) == (NodeType.CONSTANT))))
                        {
                            this.children[0].__set_and_reduce_constant(((this.children[0].constant) + (prod.constant)));
                            continue;
                        }
                        this.children.Insert(0, prod);
                        continue;
                    }
                    this.children.Add(prod);
                }
            }
            this.__merge_similar_nodes_sum();
            if ((this.children.Count()) == (1))
            {
                if (!(keepSum))
                {
                    this.copy(this.children[0]);
                }
            }
            else
            {
                if ((this.children.Count()) == (0))
                {
                    this.copy(this.__new_constant_node(0));
                }
            }
        }

        public Node __get_product_with_node(Node other)
        {
            if ((this.type) == (NodeType.CONSTANT))
            {
                return this.__get_product_of_constant_and_node(other);
            }
            if ((other.type) == (NodeType.CONSTANT))
            {
                return other.__get_product_of_constant_and_node(this);
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                if ((other.type) == (NodeType.PRODUCT))
                {
                    return this.__get_product_of_products(other);
                }
                if ((other.type) == (NodeType.POWER))
                {
                    return this.__get_product_of_product_and_power(other);
                }
                return this.__get_product_of_product_and_other(other);
            }
            if ((this.type) == (NodeType.POWER))
            {
                if ((other.type) == (NodeType.POWER))
                {
                    return this.__get_product_of_powers(other);
                }
                if ((other.type) == (NodeType.PRODUCT))
                {
                    return other.__get_product_of_product_and_power(this);
                }
                return this.__get_product_of_power_and_other(other);
            }
            if ((other.type) == (NodeType.PRODUCT))
            {
                return other.__get_product_of_product_and_other(this);
            }
            if ((other.type) == (NodeType.POWER))
            {
                return other.__get_product_of_power_and_other(this);
            }
            return this.__get_product_generic(other);
        }

        public Node __get_product_of_constant_and_node(Node other)
        {
            Assert.True((this.type) == (NodeType.CONSTANT));
            var node = other.get_copy();
            node.__multiply(this.constant);
            return node;
        }

        public Node __get_product_of_products(Node other)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            Assert.True((other.type) == (NodeType.PRODUCT));
            var node = this.get_copy();
            foreach (var ochild in other.children)
            {
                if ((ochild.type) == (NodeType.CONSTANT))
                {
                    if ((node.children[0].type) == (NodeType.CONSTANT))
                    {
                        node.children[0].__set_and_reduce_constant(((node.children[0].constant) * (ochild.constant)));
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
                if ((ochild.type) == (NodeType.POWER))
                {
                    node.__merge_power_into_product(ochild);
                    continue;
                }
                var merged = false;
                foreach (var i in Range.Get(node.children.Count()))
                {
                    var child = node.children[i];
                    if (child.equals(ochild))
                    {
                        List<Node> power_children = new() { child, this.__new_constant_node(2) };
                        node.children[i] = this.__new_node_with_children(NodeType.POWER, power_children);
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
            if ((node.children.Count()) == (1))
            {
                return node.children[0];
            }
            if ((node.children.Count()) == (0))
            {
                return this.__new_constant_node(1);
            }
            return node;
        }

        public void __merge_power_into_product(Node other)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            Assert.True((other.type) == (NodeType.POWER));
            var _base = other.children[0];
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child = this.children[i];
                if (child.equals(_base))
                {
                    this.children[i] = other.get_copy();
                    this.children[i].children[1].__add_constant(1);
                    if (this.children[i].children[1].__is_constant(0))
                    {
                        this.children.RemoveAt(i);
                    }
                    return;
                }
                if ((((child.type) == (NodeType.POWER)) && (child.children[0].equals(_base))))
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

        public Node __get_product_of_powers(Node other)
        {
            Assert.True((this.type) == (NodeType.POWER));
            Assert.True((other.type) == (NodeType.POWER));
            if (this.children[0].equals(other.children[0]))
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
            List<Node> prod_children = new() { this.get_copy(), other.get_copy() };
            return this.__new_node_with_children(NodeType.PRODUCT, prod_children);
        }

        public Node __get_product_of_product_and_power(Node other)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            Assert.True((other.type) == (NodeType.POWER));
            var node = this.get_copy();
            node.__merge_power_into_product(other);
            if ((node.children.Count()) == (1))
            {
                return node.children[0];
            }
            if ((node.children.Count()) == (0))
            {
                return this.__new_constant_node(1);
            }
            return node;
        }

        public Node __get_product_of_product_and_other(Node other)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            List<NodeType> types = new() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER };
            Assert.True(!((types).Contains(other.type)));
            var node = this.get_copy();
            foreach (var i in Range.Get(node.children.Count()))
            {
                var child = node.children[i];
                if (child.equals(other))
                {
                    List<Node> power_children = new() { child.get_copy(), this.__new_constant_node(2) };
                    node.children[i] = this.__new_node_with_children(NodeType.POWER, power_children);
                    return node;
                }
                if ((((child.type) == (NodeType.POWER)) && (child.children[0].equals(other))))
                {
                    child.children[1].__add_constant(1);
                    if (child.children[1].__is_constant(0))
                    {
                        node.children.RemoveAt(i);
                        if ((node.children.Count()) == (1))
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

        public Node __get_product_of_power_and_other(Node other)
        {
            Assert.True((this.type) == (NodeType.POWER));
            List<NodeType> types = new() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER };
            Assert.True(!((types).Contains(other.type)));
            if (this.children[0].equals(other))
            {
                var node = this.get_copy();
                node.children[1].__add_constant(1);
                if (node.children[1].__is_constant(0))
                {
                    return this.__new_constant_node(1);
                }
                return node;
            }
            List<Node> prod_children = new() { this.get_copy(), other.get_copy() };
            return this.__new_node_with_children(NodeType.PRODUCT, prod_children);
        }

        public Node __get_product_generic(Node other)
        {
            List<NodeType> types = new() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER };
            Assert.True(!((types).Contains(this.type)));
            Assert.True(!((types).Contains(other.type)));
            if (this.equals(other))
            {
                List<Node> power_children = new() { this.get_copy(), this.__new_constant_node(2) };
                return this.__new_node_with_children(NodeType.POWER, power_children);
            }
            List<Node> prod_children = new() { this.get_copy(), other.get_copy() };
            return this.__new_node_with_children(NodeType.PRODUCT, prod_children);
        }

        public void __multiply_with_node_no_sum(Node other)
        {
            Assert.True((other.type) != (NodeType.SUM));
            if ((this.type) == (NodeType.CONSTANT))
            {
                this.copy(this.__get_product_of_constant_and_node(other));
                return;
            }
            if ((other.type) == (NodeType.CONSTANT))
            {
                this.__multiply(other.constant);
                return;
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                if ((other.type) == (NodeType.PRODUCT))
                {
                    this.__multiply_product_with_product(other);
                }
                else
                {
                    if ((other.type) == (NodeType.POWER))
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
            if ((this.type) == (NodeType.POWER))
            {
                if ((other.type) == (NodeType.POWER))
                {
                    this.__multiply_power_with_power(other);
                }
                else
                {
                    if ((other.type) == (NodeType.PRODUCT))
                    {
                        this.copy(other.__get_product_of_product_and_power(this));
                    }
                    else
                    {
                        this.__multiply_power_with_other(other);
                    }
                }
                return;
            }
            if ((other.type) == (NodeType.PRODUCT))
            {
                this.copy(other.__get_product_of_product_and_other(this));
            }
            else
            {
                if ((other.type) == (NodeType.POWER))
                {
                    this.copy(other.__get_product_of_power_and_other(this));
                }
                else
                {
                    this.__multiply_generic(other);
                }
            }
        }

        public void __multiply_product_with_product(Node other)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            Assert.True((other.type) == (NodeType.PRODUCT));
            foreach (var ochild in other.children)
            {
                if ((ochild.type) == (NodeType.CONSTANT))
                {
                    if ((this.children[0].type) == (NodeType.CONSTANT))
                    {
                        this.children[0].__set_and_reduce_constant(((this.children[0].constant) * (ochild.constant)));
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
                if ((ochild.type) == (NodeType.POWER))
                {
                    this.__merge_power_into_product(ochild);
                    continue;
                }
                var merged = false;
                foreach (var i in Range.Get(this.children.Count()))
                {
                    var child = this.children[i];
                    if (child.equals(ochild))
                    {
                        List<Node> power_children = new() { child, this.__new_constant_node(2) };
                        this.children[i] = this.__new_node_with_children(NodeType.POWER, power_children);
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
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            else
            {
                if ((this.children.Count()) == (0))
                {
                    this.copy(this.__new_constant_node(1));
                }
            }
        }

        public void __multiply_power_with_power(Node other)
        {
            Assert.True((this.type) == (NodeType.POWER));
            Assert.True((other.type) == (NodeType.POWER));
            if (this.children[0].equals(other.children[0]))
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
                List<Node> prod_children = new() { this.__get_shallow_copy(), other.get_copy() };
                this.copy(this.__new_node_with_children(NodeType.PRODUCT, prod_children));
            }
        }

        public void __multiply_product_with_power(Node other)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            Assert.True((other.type) == (NodeType.POWER));
            this.__merge_power_into_product(other);
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            else
            {
                if ((this.children.Count()) == (0))
                {
                    this.copy(this.__new_constant_node(1));
                }
            }
        }

        public void __multiply_product_with_other(Node other)
        {
            Assert.True((this.type) == (NodeType.PRODUCT));
            List<NodeType> types = new() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER };
            Assert.True(!((types).Contains(other.type)));
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child = this.children[i];
                if (child.equals(other))
                {
                    List<Node> power_children = new() { child.__get_shallow_copy(), this.__new_constant_node(2) };
                    this.children[i] = this.__new_node_with_children(NodeType.POWER, power_children);
                    return;
                }
                if ((((child.type) == (NodeType.POWER)) && (child.children[0].equals(other))))
                {
                    child.children[1].__add_constant(1);
                    if (child.children[1].__is_constant(0))
                    {
                        this.children.RemoveAt(i);
                        if ((this.children.Count()) == (1))
                        {
                            this.copy(this.children[0]);
                        }
                    }
                    return;
                }
            }
            this.children.Add(other.get_copy());
        }

        public void __multiply_power_with_other(Node other)
        {
            Assert.True((this.type) == (NodeType.POWER));
            List<NodeType> types = new() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER };
            Assert.True(!((types).Contains(other.type)));
            if (this.children[0].equals(other))
            {
                this.children[1].__add_constant(1);
                if (this.children[1].__is_constant(0))
                {
                    this.copy(this.__new_constant_node(1));
                }
                return;
            }
            List<Node> prod_children = new() { this.__get_shallow_copy(), other.get_copy() };
            this.copy(this.__new_node_with_children(NodeType.PRODUCT, prod_children));
        }

        public void __multiply_generic(Node other)
        {
            List<NodeType> types = new() { NodeType.CONSTANT, NodeType.PRODUCT, NodeType.POWER };
            Assert.True(!((types).Contains(this.type)));
            Assert.True(!((types).Contains(other.type)));
            if (this.equals(other))
            {
                List<Node> power_children = new() { this.__get_shallow_copy(), this.__new_constant_node(2) };
                this.copy(this.__new_node_with_children(NodeType.POWER, power_children));
            }
            else
            {
                List<Node> prod_children = new() { this.__get_shallow_copy(), other.get_copy() };
                this.copy(this.__new_node_with_children(NodeType.PRODUCT, prod_children));
            }
        }

        public bool __check_expand_power()
        {
            Assert.True((this.type) == (NodeType.POWER));
            if ((this.children[0].type) != (NodeType.SUM))
            {
                return false;
            }
            var expNode = this.children[1];
            if ((expNode.type) != (NodeType.CONSTANT))
            {
                return false;
            }
            var exp = ((expNode.constant) % (this.__modulus));
            if ((exp) > (MAX_EXPONENT_TO_EXPAND))
            {
                return false;
            }
            this.__expand_power(exp);
            return true;
        }

        public void __expand_power(long _exp)
        {
            var exp = Convert.ToInt32(_exp);
            var _base = this.children[0];
            var node = _base.get_copy();
            Assert.True((node.type) == (NodeType.SUM));
            foreach (var i in Range.Get(1, exp))
            {
                node.__multiply_sum_with_sum(_base, true);
                if (node.__is_constant(0))
                {
                    break;
                }
            }
            if ((node.children.Count()) == (1))
            {
                this.copy(node.children[0]);
            }
            else
            {
                this.copy(node);
            }
        }

        public bool factorize_sums(bool restrictedScope = false)
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
            if ((((this.type) != (NodeType.SUM)) || ((this.children.Count()) <= (1))))
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
            var partition = new Batch(new() { }, new() { }, Range.Get(this.children.Count()).ToHashSet(), nodesToTerms, termsToNodes, nodesTriviality, nodesOrder);
            if (partition.is_trivial())
            {
                return false;
            }
            this.copy(this.__node_from_batch(partition, nodes, termsToNodes));
            return true;
        }

        public (List<Node>, List<(int, HashSet<IndexWithMultitude>)>, List<HashSet<IndexWithMultitude>>) __collect_all_factors_of_sum()
        {
            List<Node> nodes = new() { };
            List<(int, HashSet<IndexWithMultitude>)> nodesToTerms = new() { };
            List<HashSet<IndexWithMultitude>> termsToNodes = new() { };
            foreach (var i in Range.Get(this.children.Count()))
            {
                HashSet<IndexWithMultitude> set = new HashSet<IndexWithMultitude>() { };
                termsToNodes.Add(set);
                var term = this.children[i];
                term.__collect_factors(i, 1, nodes, nodesToTerms, termsToNodes);
            }
            Assert.True((nodes.Count()) == (nodesToTerms.Count()));
            return (nodes, nodesToTerms, termsToNodes);
        }

        public void __collect_factors(int i, long multitude, List<Node> nodes, List<(int, HashSet<IndexWithMultitude>)> nodesToTerms, List<HashSet<IndexWithMultitude>> termsToNodes)
        {
            if ((this.type) == (NodeType.PRODUCT))
            {
                foreach (var factor in this.children)
                {
                    factor.__collect_factors(i, multitude, nodes, nodesToTerms, termsToNodes);
                }
            }
            else
            {
                if ((this.type) == (NodeType.POWER))
                {
                    this.__collect_factors_of_power(i, multitude, nodes, nodesToTerms, termsToNodes);
                }
                else
                {
                    if ((this.type) != (NodeType.CONSTANT))
                    {
                        this.__check_store_factor(i, multitude, nodes, nodesToTerms, termsToNodes);
                    }
                }
            }
        }

        public void __collect_factors_of_power(int i, long multitude, List<Node> nodes, List<(int, HashSet<IndexWithMultitude>)> nodesToTerms, List<HashSet<IndexWithMultitude>> termsToNodes)
        {
            Assert.True((this.type) == (NodeType.POWER));
            var _base = this.children[0];
            var exp = this.children[1];
            if ((exp.type) == (NodeType.CONSTANT))
            {
                _base.__collect_factors(i, ((((exp.constant) * (multitude))) % (this.__modulus)), nodes, nodesToTerms, termsToNodes);
                return;
            }
            if ((exp.type) == (NodeType.SUM))
            {
                var first = exp.children[0];
                if ((first.type) == (NodeType.CONSTANT))
                {
                    _base.__collect_factors(i, ((((first.constant) * (multitude))) % (this.__modulus)), nodes, nodesToTerms, termsToNodes);
                    var node = this.get_copy();
                    node.children[1].children.RemoveAt(0);
                    if ((node.children[1].children.Count()) == (1))
                    {
                        node.children[1] = node.children[1].children[0];
                    }
                    node.__check_store_factor(i, multitude, nodes, nodesToTerms, termsToNodes);
                    return;
                }
            }
            this.__check_store_factor(i, multitude, nodes, nodesToTerms, termsToNodes);
        }

        public void __check_store_factor(int i, long multitude, List<Node> nodes, List<(int, HashSet<IndexWithMultitude>)> nodesToTerms, List<HashSet<IndexWithMultitude>> termsToNodes)
        {
            var idx = this.__get_index_in_list(nodes);
            if ((idx) == (null))
            {
                nodes.Add(this.__get_shallow_copy());
                List<IndexWithMultitude> items = new() { new IndexWithMultitude(i, multitude) };
                var set = new HashSet<IndexWithMultitude>(items);
                nodesToTerms.Add((((nodes.Count()) - (1)), set));
                termsToNodes[i].Add(new IndexWithMultitude(((nodes.Count()) - (1)), multitude));
                return;
            }
            var ntt = nodesToTerms[idx].Item2;
            var res = ntt.Where(p => (p.idx) == (i)).Select(p => p).ToList();
            Assert.True((res.Count()) <= (1));
            if ((res.Count()) == (1))
            {
                res[0].multitude += multitude;
            }
            else
            {
                ntt.Add(new IndexWithMultitude(i, multitude));
            }
            var ttn = termsToNodes[i];
            var res2 = ttn.Where(p => (p.idx) == (idx)).Select(p => p).ToList();
            Assert.True((res2.Count()) <= (1));
            Assert.True(((res2.Count()) == (1)) == ((res.Count()) == (1)));
            if ((res2.Count()) == (1))
            {
                res2[0].multitude += multitude;
            }
            else
            {
                ttn.Add(new IndexWithMultitude(idx, multitude));
            }
        }

        public List<bool> __determine_nodes_triviality(List<Node> nodes)
        {
            return nodes.Where(n => true).Select(n => n.__is_trivial_in_factorization()).ToList();
        }

        public List<int> __determine_nodes_order(List<Node> nodes)
        {
            //var enumNodes = new(enumerate(nodes));
            //enumNodes.Sort();
            var enumNodes = nodes.Enumerate().ToList();
            enumNodes.OrderBy(x => x.Item1);
            return enumNodes.Where(p => true).Select(p => p.Item1).ToList();
        }

        public bool __is_trivial_in_factorization()
        {
            return !(this.__is_bitwise_binop());
        }

        public Node __node_from_batch(Batch batch, List<Node> nodes, List<HashSet<IndexWithMultitude>> termsToNodes)
        {
            var node = this.__new_node(NodeType.SUM);
            Node prod = null;
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
                if ((nodeIndices.Count()) == (0))
                {
                    node.__add_constant(constant);
                    continue;
                }
                if ((((nodeIndices.Count()) == (1)) && ((constant) == (1))))
                {
                    var p = nodeIndices.Pop();
                    node.children.Add(this.__create_node_for_factor(nodes, p));
                    continue;
                }
                prod = this.__new_node(NodeType.PRODUCT);
                if ((constant) != (1))
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
            if ((node.children.Count()) == (1))
            {
                node.copy(node.children[0]);
            }
            if ((batch.factorIndices.Count()) == (0))
            {
                return node;
            }
            prod = this.__new_node(NodeType.PRODUCT);
            foreach (var p in batch.factorIndices)
            {
                prod.children.Add(this.__create_node_for_factor(nodes, p));
            }
            if ((((node.children.Count()) == (1)) && ((node.children[0].type) == (NodeType.CONSTANT))))
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

        public void __reduce_node_set(HashSet<IndexWithMultitude> indicesWithMultitudes, List<IndexWithMultitude> l1, List<IndexWithMultitude> l2)
        {
            foreach (var p in l1.Concat(l2).ToList())
            {
                var m = indicesWithMultitudes.Where(q => (q.idx) == (p.idx)).Select(q => q).ToList();
                Assert.True((m.Count()) == (1));
                Assert.True((m[0].multitude) >= (p.multitude));
                m[0].multitude -= p.multitude;
                if ((m[0].multitude) == (0))
                {
                    indicesWithMultitudes.Remove(m[0]);
                }
            }
        }

        public long __get_const_factor_respecting_powers()
        {
            if ((this.type) == (NodeType.CONSTANT))
            {
                return this.constant;
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                long f = 1;
                foreach (var child in this.children)
                {
                    f = this.__get_reduced_constant(((f) * (child.__get_const_factor_respecting_powers())));
                }
                return f;
            }
            if ((this.type) != (NodeType.POWER))
            {
                return 1;
            }
            var _base = this.children[0];
            if ((_base.type) != (NodeType.PRODUCT))
            {
                return 1;
            }
            if ((_base.children[0].type) != (NodeType.CONSTANT))
            {
                return 1;
            }
            var _const = _base.children[0].constant;
            var exp = this.children[1];
            if ((exp.type) == (NodeType.CONSTANT))
            {
                return this.__power(_const, exp.constant);
            }
            if ((exp.type) != (NodeType.SUM))
            {
                return 1;
            }
            if ((exp.children[0].type) != (NodeType.CONSTANT))
            {
                return 1;
            }
            return this.__power(_const, exp.children[0].constant);
        }

        public Node __create_node_for_factor(List<Node> nodes, IndexWithMultitude indexWithMultitude)
        {
            var exp = indexWithMultitude.multitude;
            Assert.True((exp) > (0));
            var idx = indexWithMultitude.idx;
            if ((exp) == (1))
            {
                return nodes[idx].get_copy();
            }
            List<Node> pow_children = new() { nodes[idx].get_copy(), this.__new_constant_node(exp) };
            return this.__new_node_with_children(NodeType.POWER, pow_children);
        }

        public bool __insert_fixed_in_conj()
        {
            if ((this.type) != (NodeType.CONJUNCTION))
            {
                return false;
            }
            var changed = false;
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child1 = this.children[i];
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((i) == (j))
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

        public bool __check_insert_fixed_true(Node node)
        {
            if (!(this.__is_bitwise_op()))
            {
                return false;
            }
            var changed = false;
            foreach (var child in this.children)
            {
                if (child.equals(node))
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

        public bool __insert_fixed_in_disj()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return false;
            }
            var changed = false;
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child1 = this.children[i];
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((i) == (j))
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

        public bool __check_insert_fixed_false(Node node)
        {
            if (!(this.__is_bitwise_op()))
            {
                return false;
            }
            var changed = false;
            foreach (var child in this.children)
            {
                if (child.equals(node))
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
            if ((this.type) != (NodeType.EXCL_DISJUNCTION))
            {
                return false;
            }
            var c = this.children[0];
            if ((this.children.Count()) == (2))
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
                Assert.True((this.children.Count()) > (2));
                if (c.__is_constant(0))
                {
                    this.children.RemoveAt(0);
                    return true;
                }
            }
            return false;
        }

        public bool __check_xor_same_mult_by_minus_one()
        {
            if (!((this.type) == (NodeType.PRODUCT)))
            {
                return false;
            }
            var first = this.children[0];
            if (!((first.type) == (NodeType.CONSTANT)))
            {
                return false;
            }
            if ((((first.constant) % (2))) != (0))
            {
                return false;
            }
            var changed = false;
            foreach (var i in Range.Get(((this.children.Count()) - (1)), 0, -(1)))
            {
                var child = this.children[i];
                List<NodeType> types = new() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION };
                if (!((types).Contains(child.type)))
                {
                    continue;
                }
                if ((child.children.Count()) != (2))
                {
                    continue;
                }
                var node = child.children[0].get_copy();
                node.__multiply_by_minus_one();
                if (!(node.equals(child.children[1])))
                {
                    continue;
                }
                first.constant /= 2;
                if ((child.type) == (NodeType.CONJUNCTION))
                {
                    first.__set_and_reduce_constant(-(first.constant));
                }
                child.type = NodeType.EXCL_DISJUNCTION;
                changed = true;
                if ((((first.constant) % (2))) != (0))
                {
                    break;
                }
            }
            return changed;
        }

        public bool __check_conj_zero_rule()
        {
            if ((this.type) != (NodeType.CONJUNCTION))
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
            Assert.True((this.type) == (NodeType.CONJUNCTION));
            foreach (var i in Range.Get(((this.children.Count()) - (1))))
            {
                var child1 = this.children[i];
                foreach (var j in Range.Get(((i) + (1)), this.children.Count()))
                {
                    var child2 = this.children[j];
                    var neg2 = child2.get_copy();
                    neg2.__multiply_by_minus_one();
                    if (!(child1.equals(neg2)))
                    {
                        continue;
                    }
                    var double1 = child1.get_copy();
                    double1.__multiply(2);
                    var double2 = child2.get_copy();
                    double2.__multiply(2);
                    foreach (var k in Range.Get(this.children.Count()))
                    {
                        List<int> nums = new() { i, j };
                        if (((nums).Contains(k)))
                        {
                            continue;
                        }
                        var child3 = this.children[k];
                        if (((child3.equals(double1)) || (child3.equals(double2))))
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
            if ((this.type) != (NodeType.CONJUNCTION))
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
            Assert.True((this.type) == (NodeType.CONJUNCTION));
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_neg_xor_same_neg();
                if ((node) == (null))
                {
                    continue;
                }
                var node2 = node.get_copy();
                node2.__multiply_by_minus_one();
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((i) == (j))
                    {
                        continue;
                    }
                    var neg2 = this.children[j].get_copy();
                    neg2.__negate();
                    if (((neg2.__is_double(node)) || (neg2.__is_double(node2))))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Node __get_opt_arg_neg_xor_same_neg()
        {
            if ((this.type) != (NodeType.PRODUCT))
            {
                return null;
            }
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var xor = this.children[1];
            if ((xor.type) != (NodeType.EXCL_DISJUNCTION))
            {
                return null;
            }
            if ((xor.children.Count()) != (2))
            {
                return null;
            }
            var node0 = xor.children[0].get_copy();
            node0.__multiply_by_minus_one();
            if (node0.equals(xor.children[1]))
            {
                return node0;
            }
            return null;
        }

        public bool __check_conj_neg_xor_minus_one_rule()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
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
            Assert.True((this.type) == (NodeType.INCL_DISJUNCTION));
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child1 = this.children[i];
                if ((child1.type) != (NodeType.NEGATION))
                {
                    continue;
                }
                var node = child1.children[0].__get_opt_arg_neg_xor_same_neg();
                if ((node) == (null))
                {
                    continue;
                }
                var node2 = node.get_copy();
                node2.__multiply_by_minus_one();
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((i) == (j))
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if (((child2.__is_double(node)) || (child2.__is_double(node2))))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool __check_conj_negated_xor_zero_rule()
        {
            if ((this.type) != (NodeType.CONJUNCTION))
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
            Assert.True((this.type) == (NodeType.CONJUNCTION));
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_negated_xor_same_neg();
                if ((node) == (null))
                {
                    continue;
                }
                var node2 = node.get_copy();
                node2.__multiply_by_minus_one();
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((i) == (j))
                    {
                        continue;
                    }
                    var child2 = this.children[j].get_copy();
                    if (((child2.__is_double(node)) || (child2.__is_double(node2))))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Node __get_opt_arg_negated_xor_same_neg()
        {
            if ((this.type) != (NodeType.NEGATION))
            {
                return null;
            }
            var xor = this.children[0];
            if ((xor.type) != (NodeType.EXCL_DISJUNCTION))
            {
                return null;
            }
            if ((xor.children.Count()) != (2))
            {
                return null;
            }
            var node0 = xor.children[0].get_copy();
            node0.__multiply_by_minus_one();
            if (node0.equals(xor.children[1]))
            {
                return node0;
            }
            return null;
        }

        public bool __check_conj_xor_identity_rule()
        {
            if ((this.type) != (NodeType.CONJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                if (!(child1.__is_xor_same_neg()))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if (((child2.__is_double(child1.children[0])) || (child2.__is_double(child1.children[1]))))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public bool __is_xor_same_neg()
        {
            if ((this.type) != (NodeType.EXCL_DISJUNCTION))
            {
                return false;
            }
            if ((this.children.Count()) != (2))
            {
                return false;
            }
            var neg = this.children[1].get_copy();
            neg.__multiply_by_minus_one();
            return neg.equals(this.children[0]);
        }

        public bool __is_double(Node node)
        {
            var cpy = node.get_copy();
            cpy.__multiply(2);
            return this.equals(cpy);
        }

        public bool __check_disj_xor_identity_rule()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_xor_disj_xor_identity();
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if (((child2.__is_double(node.children[0])) || (child2.__is_double(node.children[1]))))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public Node __get_opt_xor_disj_xor_identity()
        {
            if ((this.type) != (NodeType.PRODUCT))
            {
                return null;
            }
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var child = this.children[1];
            if ((child.type) != (NodeType.EXCL_DISJUNCTION))
            {
                return null;
            }
            if ((child.children.Count()) != (2))
            {
                return null;
            }
            var neg = child.children[1].get_copy();
            neg.__multiply_by_minus_one();
            return (neg.equals(child.children[0])) ? child : null;
        }

        public bool __check_conj_neg_conj_identity_rule()
        {
            if ((this.type) != (NodeType.CONJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_neg_conj_double();
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    var neg = child2.get_copy();
                    neg.__multiply_by_minus_one();
                    if (neg.equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public Node __get_opt_arg_neg_conj_double()
        {
            var node = this.__get_opt_arg_neg_conj_double_1();
            return ((node) != (null)) ? node : this.__get_opt_arg_neg_conj_double_2();
        }

        public Node __get_opt_arg_neg_conj_double_1()
        {
            if ((this.type) != (NodeType.NEGATION))
            {
                return null;
            }
            var child = this.children[0];
            if ((child.type) != (NodeType.CONJUNCTION))
            {
                return null;
            }
            if ((child.children.Count()) != (2))
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

        public Node __get_opt_arg_neg_conj_double_2()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return null;
            }
            if ((this.children.Count()) != (2))
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

        public bool __check_disj_disj_identity_rule()
        {
            return this.__check_nested_bitwise_identity_rule(NodeType.INCL_DISJUNCTION);
        }

        public bool __check_conj_conj_identity_rule()
        {
            return this.__check_nested_bitwise_identity_rule(NodeType.CONJUNCTION);
        }

        public bool __check_nested_bitwise_identity_rule(NodeType t)
        {
            if ((this.type) != (t))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var nodes = child1.__get_candidates_nested_bitwise_identity(t);
                Assert.True((nodes.Count()) <= (2));
                if ((nodes.Count()) == (0))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    var done = false;
                    foreach (var node in nodes)
                    {
                        if (child2.equals(node))
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
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public List<Node> __get_candidates_nested_bitwise_identity(NodeType t)
        {
            if ((this.type) != (NodeType.PRODUCT))
            {
                return new() { };
            }
            if ((this.children.Count()) != (2))
            {
                return new() { };
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return new() { };
            }
            var bitw = this.children[1];
            if ((bitw.type) != (t))
            {
                return new() { };
            }
            if ((bitw.children.Count()) != (2))
            {
                return new() { };
            }
            var neg = bitw.children[1].get_copy();
            neg.__multiply_by_minus_one();
            if (neg.equals(bitw.children[0]))
            {
                return new() { bitw.children[0], bitw.children[1] };
            }
            var ot = ((t) == (NodeType.INCL_DISJUNCTION)) ? NodeType.CONJUNCTION : NodeType.INCL_DISJUNCTION;
            if ((bitw.children[0].type) == (ot))
            {
                if (bitw.children[0].__has_child(neg))
                {
                    return new() { neg };
                }
            }
            if ((bitw.children[1].type) == (ot))
            {
                neg = bitw.children[0].get_copy();
                neg.__multiply_by_minus_one();
                if (bitw.children[1].__has_child(neg))
                {
                    return new() { neg };
                }
            }
            return (neg.equals(bitw.children[0])) ? new() { bitw } : new() { };
        }

        public bool __check_disj_conj_identity_rule()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_conj_identity();
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    var neg = child2.get_copy();
                    neg.__multiply_by_minus_one();
                    if (neg.equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public Node __get_opt_arg_disj_conj_identity()
        {
            var node = this.__get_opt_arg_disj_conj_identity_1();
            return ((node) != (null)) ? node : this.__get_opt_arg_disj_conj_identity_2();
        }

        public Node __get_opt_arg_disj_conj_identity_1()
        {
            if ((this.type) != (NodeType.CONJUNCTION))
            {
                return null;
            }
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            List<int> nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                var oIdx = ((idx) == (1)) ? 0 : 1;
                var oDiv = this.children[oIdx].__divided(2);
                if ((oDiv) == (null))
                {
                    continue;
                }
                var oDivNeg = oDiv.get_copy();
                oDivNeg.__multiply_by_minus_one();
                Node neg = null;
                var node = this.children[idx];
                if ((node.type) == (NodeType.NEGATION))
                {
                    neg = node.children[0];
                    if (((neg.equals(oDiv)) || (neg.equals(oDivNeg))))
                    {
                        return neg;
                    }
                }
                if ((oDiv.type) == (NodeType.NEGATION))
                {
                    neg = oDiv.children[0];
                    if (neg.equals(node))
                    {
                        return oDiv;
                    }
                }
                if ((oDivNeg.type) == (NodeType.NEGATION))
                {
                    neg = oDivNeg.children[0];
                    if (neg.equals(node))
                    {
                        return oDivNeg;
                    }
                }
                neg = node.__get_opt_transformed_negated();
                if ((neg) != (null))
                {
                    if (((neg.equals(oDiv)) || (neg.equals(oDivNeg))))
                    {
                        return neg;
                    }
                }
                neg = oDiv.__get_opt_transformed_negated();
                if ((neg) != (null))
                {
                    if (neg.equals(node))
                    {
                        return oDiv;
                    }
                }
                neg = oDivNeg.__get_opt_transformed_negated();
                if ((neg) != (null))
                {
                    if (neg.equals(node))
                    {
                        return oDivNeg;
                    }
                }
            }
            return null;
        }

        public Node __get_opt_arg_disj_conj_identity_2()
        {
            if ((this.type) != (NodeType.NEGATION))
            {
                return null;
            }
            var child = this.children[0];
            if ((child.type) != (NodeType.INCL_DISJUNCTION))
            {
                return null;
            }
            if ((child.children.Count()) != (2))
            {
                return null;
            }
            List<int> nums = new() { 0, 1 };
            foreach (var negIdx in nums)
            {
                var ch = child.children[negIdx];
                Node node = null;
                if ((ch.type) == (NodeType.NEGATION))
                {
                    node = ch.children[0];
                }
                else
                {
                    node = ch.__get_opt_transformed_negated();
                }
                if ((node) == (null))
                {
                    continue;
                }
                var oIdx = ((negIdx) == (1)) ? 0 : 1;
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

        public Node __divided(int divisor)
        {
            if ((this.type) == (NodeType.CONSTANT))
            {
                if ((((this.constant) % (divisor))) == (0))
                {
                    return this.__new_constant_node(((this.constant) / (divisor)));
                }
            }
            Node res = null;
            Node node = null;
            if ((this.type) == (NodeType.PRODUCT))
            {
                foreach (var i in Range.Get(this.children.Count()))
                {
                    node = this.children[i].__divided(divisor);
                    if ((node) == (null))
                    {
                        continue;
                    }
                    res = this.get_copy();
                    res.children[i] = node;
                    if (res.children[i].__is_constant(1))
                    {
                        res.children.RemoveAt(i);
                        if ((res.children.Count()) == (1))
                        {
                            return res.children[0];
                        }
                    }
                    return res;
                }
                return null;
            }
            if ((this.type) == (NodeType.SUM))
            {
                res = this.__new_node(NodeType.SUM);
                foreach (var child in this.children)
                {
                    node = child.__divided(divisor);
                    if ((node) == (null))
                    {
                        return null;
                    }
                    res.children.Add(node);
                }
                return res;
            }
            return null;
        }

        public bool __check_disj_conj_identity_rule_2()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_conj_identity_rule_2();
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    if (this.children[j].equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public Node __get_opt_arg_disj_conj_identity_rule_2()
        {
            if ((this.type) != (NodeType.CONJUNCTION))
            {
                return null;
            }
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            int oIdx = 0;
            Node node = null;
            List<int> nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                oIdx = ((idx) == (1)) ? 0 : 1;
                if (this.children[oIdx].__is_double(this.children[idx]))
                {
                    node = this.children[idx].get_copy();
                    node.__multiply_by_minus_one();
                    node.__negate();
                    return node;
                }
            }
            foreach (var idx in nums)
            {
                oIdx = ((idx) == (1)) ? 0 : 1;
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

        public bool __check_disj_neg_disj_identity_rule()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_neg_disj_identity_rule();
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    if (this.children[j].equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public Node __get_opt_arg_disj_neg_disj_identity_rule()
        {
            if ((this.type) != (NodeType.PRODUCT))
            {
                return null;
            }
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return null;
            }
            var disj = this.children[1];
            if ((disj.type) != (NodeType.INCL_DISJUNCTION))
            {
                return null;
            }
            if ((disj.children.Count()) != (2))
            {
                return null;
            }
            int oIdx = 0;
            Node node = null;
            List<int> nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                oIdx = ((idx) == (1)) ? 0 : 1;
                if (disj.children[oIdx].__is_double(disj.children[idx]))
                {
                    node = disj.children[idx].get_copy();
                    node.__multiply_by_minus_one();
                    return node;
                }
            }
            nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                oIdx = ((idx) == (1)) ? 0 : 1;
                node = disj.children[idx].get_copy();
                node.__multiply_by_minus_one();
                if (disj.children[oIdx].__is_double(node))
                {
                    return node;
                }
            }
            return null;
        }

        public bool __check_conj_disj_identity_rule()
        {
            if ((this.type) != (NodeType.CONJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_conj_disj_identity();
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if (child2.equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public Node __get_opt_arg_conj_disj_identity()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return null;
            }
            var node = this.__get_opt_arg_conj_disj_identity_1();
            return ((node) != (null)) ? node : this.__get_opt_arg_conj_disj_identity_2();
        }

        public Node __get_opt_arg_conj_disj_identity_1()
        {
            Assert.True((this.type) == (NodeType.INCL_DISJUNCTION));
            if ((this.children.Count()) != (2))
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

        public Node __get_opt_arg_conj_disj_identity_2()
        {
            Assert.True((this.type) == (NodeType.INCL_DISJUNCTION));
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            List<int> nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                var neg = this.children[idx].get_copy();
                neg.__negate();
                var oIdx = ((idx) == (1)) ? 0 : 1;
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

        public bool __check_disj_sub_disj_identity_rule()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_sub_disj_identity();
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    if (this.children[j].equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public Node __get_opt_arg_disj_sub_disj_identity()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return null;
            }
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            List<int> nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                var child = this.children[idx];
                if ((child.type) != (NodeType.INCL_DISJUNCTION))
                {
                    continue;
                }
                if ((child.children.Count()) != (2))
                {
                    continue;
                }
                var oidx = ((idx) == (1)) ? 0 : 1;
                var neg = this.children[oidx].get_copy();
                neg.__multiply_by_minus_one();
                if (neg.equals(child.children[0]))
                {
                    return child.children[1];
                }
                if (neg.equals(child.children[1]))
                {
                    return child.children[0];
                }
            }
            return null;
        }

        public bool __check_disj_sub_conj_identity_rule()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_disj_sub_conj_identity();
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    if (this.children[j].equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public Node __get_opt_arg_disj_sub_conj_identity()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return null;
            }
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            List<int> nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                var child = this.children[idx];
                if ((child.type) != (NodeType.PRODUCT))
                {
                    continue;
                }
                if ((child.children.Count()) != (2))
                {
                    continue;
                }
                if (!(child.children[0].__is_constant(-(1))))
                {
                    continue;
                }
                var conj = child.children[1];
                if ((conj.type) != (NodeType.CONJUNCTION))
                {
                    continue;
                }
                var oidx = ((idx) == (1)) ? 0 : 1;
                var other = this.children[oidx];
                foreach (var c in conj.children)
                {
                    if (c.equals(other))
                    {
                        return other;
                    }
                }
            }
            return null;
        }

        public bool __check_conj_add_conj_identity_rule()
        {
            if ((this.type) != (NodeType.CONJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var node = child1.__get_opt_arg_conj_add_conj_identity();
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    if (this.children[j].equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public Node __get_opt_arg_conj_add_conj_identity()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return null;
            }
            if ((this.children.Count()) != (2))
            {
                return null;
            }
            List<int> nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                var child = this.children[idx];
                if ((child.type) != (NodeType.CONJUNCTION))
                {
                    continue;
                }
                var oidx = ((idx) == (1)) ? 0 : 1;
                var oneg = this.children[oidx].get_copy();
                oneg.__negate();
                foreach (var c in child.children)
                {
                    if (c.equals(oneg))
                    {
                        return this.children[oidx];
                    }
                }
            }
            return null;
        }

        public bool __check_conj_conj_disj_rule()
        {
            return this.__check_nested_bitwise_rule(NodeType.CONJUNCTION);
        }

        public bool __check_disj_disj_conj_rule()
        {
            return this.__check_nested_bitwise_rule(NodeType.INCL_DISJUNCTION);
        }

        public bool __check_nested_bitwise_rule(NodeType t)
        {
            if ((this.type) != (t))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var (node1, node2) = child1.__get_opt_arg_nested_bitwise(t);
                Assert.True(((node1) == (null)) == ((node2) == (null)));
                if ((node1) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    if (this.children[j].equals(node1))
                    {
                        this.children[i].copy(node2);
                        changed = true;
                        break;
                    }
                }
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public (Node, Node) __get_opt_arg_nested_bitwise(NodeType t)
        {
            if ((this.type) != (NodeType.PRODUCT))
            {
                return (null, null);
            }
            if ((this.children.Count()) != (2))
            {
                return (null, null);
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return (null, null);
            }
            var child = this.children[1];
            if ((child.type) != (t))
            {
                return (null, null);
            }
            if ((child.children.Count()) != (2))
            {
                return (null, null);
            }
            List<int> nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                var oidx = ((idx) == (1)) ? 0 : 1;
                var c = child.children[idx];
                var ot = ((t) == (NodeType.INCL_DISJUNCTION)) ? NodeType.CONJUNCTION : NodeType.INCL_DISJUNCTION;
                if ((c.type) != (ot))
                {
                    continue;
                }
                if ((c.children.Count()) != (2))
                {
                    return (null, null);
                }
                var oneg = child.children[oidx].get_copy();
                oneg.__multiply_by_minus_one();
                if (c.children[0].equals(oneg))
                {
                    return (c.children[1], c.children[0]);
                }
                if (c.children[1].equals(oneg))
                {
                    return (c.children[0], c.children[1]);
                }
            }
            return (null, null);
        }

        public bool __check_disj_disj_conj_rule_2()
        {
            if ((this.type) != (NodeType.INCL_DISJUNCTION))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var child1 = this.children[i];
                var (node, conj) = child1.__get_opt_pair_disj_disj_conj_2();
                Assert.True(((node) == (null)) == ((conj) == (null)));
                if ((node) == (null))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((j) == (i))
                    {
                        continue;
                    }
                    var child2 = this.children[j];
                    if (child2.equals(node))
                    {
                        this.children.RemoveAt(i);
                        changed = true;
                        i -= 1;
                        break;
                    }
                    if ((child2.type) != (NodeType.CONJUNCTION))
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
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return changed;
        }

        public (Node, Node) __get_opt_pair_disj_disj_conj_2()
        {
            if ((this.type) != (NodeType.PRODUCT))
            {
                return (null, null);
            }
            if ((this.children.Count()) != (2))
            {
                return (null, null);
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return (null, null);
            }
            var disj = this.children[1];
            if ((disj.type) != (NodeType.INCL_DISJUNCTION))
            {
                return (null, null);
            }
            if ((disj.children.Count()) != (2))
            {
                return (null, null);
            }
            List<int> nums = new() { 0, 1 };
            foreach (var idx in nums)
            {
                var oIdx = ((idx) == (1)) ? 0 : 1;
                var conj = disj.children[idx];
                if ((conj.type) != (NodeType.CONJUNCTION))
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

        public bool refine_after_substitution()
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

        public bool __check_bitwise_in_sums_cancel_terms()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            if ((this.children.Count()) > (MAX_CHILDREN_TO_TRANSFORM_BITW))
            {
                return false;
            }
            var changed = true;
            var i = 0;
            while (true)
            {
                if ((i) >= (this.children.Count()))
                {
                    return changed;
                }
                var child = this.children[i];
                long factor = 1;
                if ((child.type) == (NodeType.PRODUCT))
                {
                    if ((((child.children.Count()) != (2)) || ((child.children[0].type) != (NodeType.CONSTANT))))
                    {
                        i += 1;
                        continue;
                    }
                    factor = child.children[0].constant;
                    child = child.children[1];
                }
                List<NodeType> types = new() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION };
                if (((!((types).Contains(child.type))) || ((child.children.Count()) != (2))))
                {
                    i += 1;
                    continue;
                }
                var newIdx = this.__check_transform_bitwise_in_sum_cancel(i, child, factor);
                if ((newIdx) == (null))
                {
                    i += 1;
                    continue;
                }
                Assert.True((this.children[newIdx]) == (child));
                if ((this.children.Count()) == (1))
                {
                    this.copy(this.children[0]);
                    return true;
                }
                i = ((newIdx) + (1));
            }
            return changed;
        }

        public NullableI32 __check_transform_bitwise_in_sum_cancel(int idx, Node bitw, long factor)
        {
            var withToXor = (((factor) % (2))) == (0);
            var newIdx = this.__check_transform_bitwise_in_sum_cancel_impl(false, idx, bitw, factor);
            if ((newIdx) != (null))
            {
                return newIdx;
            }
            if (withToXor)
            {
                newIdx = this.__check_transform_bitwise_in_sum_cancel_impl(true, idx, bitw, ((factor) / (2)));
                if ((newIdx) != (null))
                {
                    return newIdx;
                }
            }
            return null;
        }

        public NullableI32 __check_transform_bitwise_in_sum_cancel_impl(bool toXor, int idx, Node bitw, long factor)
        {
            Assert.True((this.type) == (NodeType.SUM));
            Assert.True((idx) < (this.children.Count()));
            var opSum = this.__new_node(NodeType.SUM);
            foreach (var op in bitw.children)
            {
                opSum.children.Add(op.get_copy());
            }
            opSum.__multiply(factor);
            opSum.expand();
            opSum.refine();
            var maxc = Math.Min(opSum.children.Count(), MAX_CHILDREN_SUMMED_UP);
            foreach (var i in Range.Get(1, LongPower(2, ((this.children.Count()) - (1)))))
            {
                if ((popcount(i)) > (maxc))
                {
                    continue;
                }
                var newIdx = this.__check_transform_bitwise_for_comb(toXor, idx, bitw, factor, opSum, i);
                if ((newIdx) != (null))
                {
                    return newIdx;
                }
            }
            return null;
        }

        public NullableI32 __check_transform_bitwise_for_comb(bool toXor, int idx, Node bitw, long factor, Node opSum, int combIdx)
        {
            var n = combIdx;
            var diff = opSum.get_copy();
            List<int> indices = new() { };
            foreach (var j in Range.Get(this.children.Count()))
            {
                if ((j) == (idx))
                {
                    continue;
                }
                if ((((n) & (1))) == (1))
                {
                    indices.Add(j);
                    diff.__add(this.children[j]);
                }
                n = ((n) >> (1));
            }
            diff.expand();
            diff.refine();
            if ((diff.type) != (NodeType.CONSTANT))
            {
                Assert.True((((this.__modulus) % (2))) == (0));
                opSum = this.__new_node(NodeType.SUM);
                foreach (var op in bitw.children)
                {
                    opSum.children.Add(op.get_copy());
                }
                var hmod = ((this.__modulus) / (2));
                opSum.__multiply(-(hmod));
                var diff2 = diff.get_copy();
                diff2.__add(opSum);
                diff2.expand();
                diff2.refine();
                if ((diff2.type) == (NodeType.CONSTANT))
                {
                    diff = diff2;
                    factor += hmod;
                }
            }
            return this.__check_transform_bitwise_for_diff(toXor, idx, bitw, factor, diff, indices);
        }

        public NullableI32 __check_transform_bitwise_for_diff(bool toXor, int idx, Node bitw, long factor, Node diff, List<int> indices)
        {
            var newIdx = this.__check_transform_bitwise_for_diff_full(toXor, idx, bitw, factor, diff, indices);
            if ((newIdx) != (null))
            {
                return newIdx;
            }
            if ((indices.Count()) > (1))
            {
                newIdx = this.__check_transform_bitwise_for_diff_merge(toXor, idx, bitw, factor, diff, indices);
                if ((newIdx) != (null))
                {
                    return newIdx;
                }
            }
            return null;
        }

        public NullableI32 __check_transform_bitwise_for_diff_full(bool toXor, int idx, Node bitw, long factor, Node diff, List<int> indices)
        {
            if ((diff.type) != (NodeType.CONSTANT))
            {
                return null;
            }
            if (((!(toXor)) || ((bitw.type) == (NodeType.CONJUNCTION))))
            {
                factor = -(factor);
            }
            bitw.type = bitw.__get_transformed_bitwise_type(toXor);
            if ((((((factor) - (1))) % (this.__modulus))) != (0))
            {
                bitw.__multiply(factor);
            }
            this.children[idx] = bitw;
            foreach (var j in Range.Get(((indices.Count()) - (1)), -(1), -(1)))
            {
                this.children.RemoveAt(indices[j]);
                if ((indices[j]) < (idx))
                {
                    idx -= 1;
                }
            }
            if (!(diff.__is_constant(0)))
            {
                if ((this.children[0].type) != (NodeType.CONSTANT))
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

        public NullableI32 __check_transform_bitwise_for_diff_merge(bool toXor, int idx, Node bitw, long factor, Node diff, List<int> indices)
        {
            var constN = diff.__get_opt_const_factor();
            foreach (var i in indices)
            {
                var child = this.children[i];
                var constC = child.__get_opt_const_factor();
                if (!(diff.__equals_neglecting_constants(child, (constN) != (null), (constC) != (null))))
                {
                    continue;
                }
                if (((!(toXor)) || ((bitw.type) == (NodeType.CONJUNCTION))))
                {
                    factor = -(factor);
                }
                bitw.type = bitw.__get_transformed_bitwise_type(toXor);
                if ((((((factor) - (1))) % (this.__modulus))) != (0))
                {
                    bitw.__multiply(factor);
                }
                this.children[idx] = bitw;
                if ((constC) != (null))
                {
                    Assert.True((child.type) == (NodeType.PRODUCT));
                    Assert.True((child.children[0].type) == (NodeType.CONSTANT));
                    if ((constN) != (null))
                    {
                        child.children[0].constant = constN;
                    }
                    else
                    {
                        child.children.RemoveAt(0);
                        if ((child.children.Count()) == (1))
                        {
                            child.copy(child.children[0]);
                        }
                    }
                }
                else
                {
                    if ((constN) != (null))
                    {
                        child.__multiply(constN);
                    }
                }
                foreach (var j in Range.Get(((indices.Count()) - (1)), -(1), -(1)))
                {
                    if ((indices[j]) != (i))
                    {
                        this.children.RemoveAt(indices[j]);
                        if ((indices[j]) < (idx))
                        {
                            idx -= 1;
                        }
                    }
                }
                return idx;
            }
            return null;
        }

        public NodeType __get_transformed_bitwise_type(bool toXor)
        {
            List<NodeType> types = new() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION };
            Assert.True(((types).Contains(this.type)));
            if (toXor)
            {
                return NodeType.EXCL_DISJUNCTION;
            }
            if ((this.type) == (NodeType.CONJUNCTION))
            {
                return NodeType.INCL_DISJUNCTION;
            }
            return NodeType.CONJUNCTION;
        }

        public bool __check_bitwise_in_sums_replace_terms()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            if ((this.children.Count()) > (MAX_CHILDREN_TO_TRANSFORM_BITW))
            {
                return false;
            }
            var changed = true;
            var i = 0;
            while (true)
            {
                if ((i) >= (this.children.Count()))
                {
                    return changed;
                }
                var child = this.children[i];
                long factor = 1;
                if ((child.type) == (NodeType.PRODUCT))
                {
                    if ((((child.children.Count()) != (2)) || ((child.children[0].type) != (NodeType.CONSTANT))))
                    {
                        i += 1;
                        continue;
                    }
                    factor = child.children[0].constant;
                    child = child.children[1];
                }
                List<NodeType> types = new() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION };
                if (((!((types).Contains(child.type))) || ((child.children.Count()) != (2))))
                {
                    i += 1;
                    continue;
                }
                var newIdx = this.__check_transform_bitwise_in_sum_replace(i, child, factor);
                if ((newIdx) == (null))
                {
                    i += 1;
                    continue;
                }
                Assert.True((this.children[newIdx]) == (child));
                Assert.True((this.children.Count()) > (1));
                i = ((newIdx) + (1));
            }
            return false;
        }

        public NullableI32 __check_transform_bitwise_in_sum_replace(int idx, Node bitw, long factor)
        {
            var cIdx = bitw.__get_index_of_more_complex_operand();
            if ((cIdx) == (null))
            {
                return null;
            }
            var withToXor = (((factor) % (2))) == (0);
            var newIdx = this.__check_transform_bitwise_in_sum_replace_impl(false, idx, bitw, cIdx, factor);
            if ((newIdx) != (null))
            {
                return newIdx;
            }
            if (withToXor)
            {
                newIdx = this.__check_transform_bitwise_in_sum_replace_impl(true, idx, bitw, cIdx, ((factor) / (2)));
                if ((newIdx) != (null))
                {
                    return newIdx;
                }
            }
            return null;
        }

        public NullableI32 __get_index_of_more_complex_operand()
        {
            Assert.True(this.__is_bitwise_binop());
            Assert.True((this.children.Count()) == (2));
            var (c0, c1) = (this.children[0], this.children[1]);
            c0.mark_linear();
            c1.mark_linear();
            var l0 = c0.is_linear();
            var l1 = c1.is_linear();
            if ((l0) != (l1))
            {
                return (l1) ? 0 : 1;
            }
            var b0 = (c0.state) == (NodeState.BITWISE);
            var b1 = (c1.state) == (NodeState.BITWISE);
            if ((b0) != (b1))
            {
                return (b1) ? 0 : 1;
            }
            else
            {
                return null;
            }
            return null;
        }

        public NullableI32 __check_transform_bitwise_in_sum_replace_impl(bool toXor, int idx, Node bitw, int cIdx, long factor)
        {
            Assert.True((this.type) == (NodeType.SUM));
            Assert.True((idx) < (this.children.Count()));
            var cOp = bitw.children[cIdx].get_copy();
            cOp.__multiply(factor);
            var maxc = MAX_CHILDREN_SUMMED_UP;
            foreach (var i in Range.Get(1, LongPower(2, ((this.children.Count()) - (1)))))
            {
                if ((popcount(i)) > (maxc))
                {
                    continue;
                }
                var newIdx = this.__check_transform_bitwise_replace_for_comb(toXor, idx, bitw, factor, cOp, cIdx, i);
                if ((newIdx) != (null))
                {
                    return newIdx;
                }
            }
            return null;
        }

        public NullableI32 __check_transform_bitwise_replace_for_comb(bool toXor, int idx, Node bitw, long factor, Node cOp, int cIdx, int combIdx)
        {
            var n = combIdx;
            var diff = cOp.get_copy();
            List<int> indices = new() { };
            foreach (var j in Range.Get(this.children.Count()))
            {
                if ((j) == (idx))
                {
                    continue;
                }
                if ((((n) & (1))) == (1))
                {
                    indices.Add(j);
                    diff.__add(this.children[j]);
                }
                n = ((n) >> (1));
            }
            diff.expand();
            diff.refine();
            return this.__check_transform_bitwise_replace_for_diff(toXor, idx, bitw, factor, diff, cIdx, indices);
        }

        public NullableI32 __check_transform_bitwise_replace_for_diff(bool toXor, int idx, Node bitw, long factor, Node diff, int cIdx, List<int> indices)
        {
            return this.__check_transform_bitwise_replace_for_diff_full(toXor, idx, bitw, factor, diff, cIdx, indices);
        }

        public NullableI32 __check_transform_bitwise_replace_for_diff_full(bool toXor, int idx, Node bitw, long factor, Node diff, int cIdx, List<int> indices)
        {
            if ((diff.type) != (NodeType.CONSTANT))
            {
                return null;
            }
            var op = bitw.children[((cIdx) == (1)) ? 0 : 1].get_copy();
            op.__multiply(factor);
            this.children.Add(op);
            if (((!(toXor)) || ((bitw.type) == (NodeType.CONJUNCTION))))
            {
                factor = -(factor);
            }
            bitw.type = bitw.__get_transformed_bitwise_type(toXor);
            if ((((((factor) - (1))) % (this.__modulus))) != (0))
            {
                bitw.__multiply(factor);
            }
            this.children[idx] = bitw;
            foreach (var j in Range.Get(((indices.Count()) - (1)), -(1), -(1)))
            {
                this.children.RemoveAt(indices[j]);
                if ((indices[j]) < (idx))
                {
                    idx -= 1;
                }
            }
            if (!(diff.__is_constant(0)))
            {
                if ((this.children[0].type) != (NodeType.CONSTANT))
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

        public bool __check_disj_involving_xor_in_sums()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            var changed = false;
            foreach (var child in this.children)
            {
                long factor = 1;
                var node = child;
                if ((node.type) == (NodeType.PRODUCT))
                {
                    if ((node.children.Count()) != (2))
                    {
                        continue;
                    }
                    if ((node.children[0].type) != (NodeType.CONSTANT))
                    {
                        continue;
                    }
                    factor = node.children[0].constant;
                    node = node.children[1];
                }
                if ((node.type) != (NodeType.INCL_DISJUNCTION))
                {
                    continue;
                }
                if ((node.children.Count()) != (2))
                {
                    continue;
                }
                NullableI32 xorIdx = null;
                if ((node.children[0].type) == (NodeType.EXCL_DISJUNCTION))
                {
                    xorIdx = 0;
                }
                if ((node.children[1].type) == (NodeType.EXCL_DISJUNCTION))
                {
                    if ((xorIdx) != (null))
                    {
                        continue;
                    }
                    xorIdx = 1;
                }
                if ((xorIdx) == (null))
                {
                    continue;
                }
                var oIdx = ((xorIdx) == (0)) ? 1 : 0;
                var xor = node.children[xorIdx];
                var o = node.children[oIdx];
                if ((xor.children.Count()) != (2))
                {
                    continue;
                }
                if (o.equals(xor.children[0]))
                {
                    List<Node> conj0_children = new() { o.__get_shallow_copy(), xor.children[1].get_copy() };
                    o = this.__new_node_with_children(NodeType.CONJUNCTION, conj0_children);
                }
                else
                {
                    if (o.equals(xor.children[1]))
                    {
                        List<Node> conj1_children = new() { o.__get_shallow_copy(), xor.children[0].get_copy() };
                        o = this.__new_node_with_children(NodeType.CONJUNCTION, conj1_children);
                    }
                    else
                    {
                        if ((o.type) != (NodeType.CONJUNCTION))
                        {
                            continue;
                        }
                        else
                        {
                            var found0 = false;
                            var found1 = false;
                            foreach (var ch in o.children)
                            {
                                if (ch.equals(xor.children[0]))
                                {
                                    found0 = true;
                                }
                                else
                                {
                                    if (ch.equals(xor.children[1]))
                                    {
                                        found1 = true;
                                    }
                                }
                                if (((found0) && (found1)))
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
                if ((factor) == (1))
                {
                    this.children.Add(xor.__get_shallow_copy());
                }
                else
                {
                    List<Node> prod_children = new() { this.__new_constant_node(factor), xor.__get_shallow_copy() };
                    var prod = this.__new_node_with_children(NodeType.PRODUCT, prod_children);
                    this.children.Add(prod);
                }
                node.copy(o);
            }
            return changed;
        }

        public bool __check_xor_involving_disj()
        {
            if ((this.type) != (NodeType.EXCL_DISJUNCTION))
            {
                return false;
            }
            if ((this.children.Count()) != (2))
            {
                return false;
            }
            List<int> nums = new() { 0, 1 };
            foreach (var disjIdx in nums)
            {
                var disj = this.children[disjIdx];
                if ((disj.type) != (NodeType.INCL_DISJUNCTION))
                {
                    continue;
                }
                var oIdx = ((disjIdx) == (0)) ? 1 : 0;
                var other = this.children[oIdx];
                var idx = disj.__get_index_of_child(other);
                if ((idx) == (null))
                {
                    continue;
                }
                other.__negate();
                this.type = NodeType.CONJUNCTION;
                disj.children.RemoveAt(idx);
                if ((disj.children.Count()) == (1))
                {
                    this.children[disjIdx] = disj.children[0];
                }
                return true;
            }
            return false;
        }

        public bool __check_negative_bitw_inverse()
        {
            if ((this.type) != (NodeType.PRODUCT))
            {
                return false;
            }
            if ((this.children.Count()) != (2))
            {
                return false;
            }
            if (!(this.children[0].__is_constant(-(1))))
            {
                return false;
            }
            var node = this.children[1];
            List<NodeType> types = new() { NodeType.CONJUNCTION, NodeType.INCL_DISJUNCTION };
            if (!((types).Contains(node.type)))
            {
                return false;
            }
            if ((node.children.Count()) != (2))
            {
                return false;
            }
            if ((node.children[0].type) == (NodeType.CONSTANT))
            {
                return false;
            }
            var inv = node.children[0].get_copy();
            inv.__multiply_by_minus_one();
            if (!(inv.equals(node.children[1])))
            {
                return false;
            }
            if ((node.type) == (NodeType.CONJUNCTION))
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
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            var l = this.__collect_indices_of_bitw_with_constants_in_sum(NodeType.EXCL_DISJUNCTION);
            if ((l.Count()) == (0))
            {
                return false;
            }
            List<int> toRemove = new() { };
            long _const = 0;
            foreach (var pair in l)
            {
                var factor = pair.Item1;
                var sublist = pair.Item2;
                var done = sublist.Where(x => true).Select(x => false).ToList();
                foreach (var i in Range.Get(((sublist.Count()) - (1)), 0, -(1)))
                {
                    if (done[i])
                    {
                        continue;
                    }
                    foreach (var j in Range.Get(0, i))
                    {
                        if (done[j])
                        {
                            continue;
                        }
                        var firstIdx = sublist[i];
                        var first = this.children[firstIdx];
                        if ((first.type) == (NodeType.PRODUCT))
                        {
                            first = first.children[1];
                        }
                        Assert.True((first.type) == (NodeType.EXCL_DISJUNCTION));
                        var secIdx = sublist[j];
                        var second = this.children[secIdx];
                        if ((second.type) == (NodeType.PRODUCT))
                        {
                            second = second.children[1];
                        }
                        Assert.True((second.type) == (NodeType.EXCL_DISJUNCTION));
                        var firstConst = ((first.children[0].constant) % (this.__modulus));
                        var secConst = ((second.children[0].constant) % (this.__modulus));
                        if ((((firstConst) & (secConst))) != (0))
                        {
                            continue;
                        }
                        var (_, remove, add) = this.__merge_bitwise_terms(firstIdx, secIdx, first, second, factor, first.children[0].constant, second.children[0].constant);
                        _const += add;
                        done[j] = true;
                        Assert.True(remove);
                        if (remove)
                        {
                            toRemove.Add(secIdx);
                        }
                    }
                }
            }
            if ((toRemove.Count()) == (0))
            {
                return false;
            }
            toRemove.Sort();
            foreach (var idx in ListUtil.Reversed(toRemove))
            {
                this.children.RemoveAt(idx);
            }
            if ((this.children[0].type) == (NodeType.CONSTANT))
            {
                this.children[0].__set_and_reduce_constant(((this.children[0].constant) + (_const)));
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(_const));
            }
            if ((((this.children.Count()) > (1)) && (this.children[0].__is_constant(0))))
            {
                this.children.RemoveAt(0);
            }
            return true;
        }

        public List<(long, List<int>)> __collect_indices_of_bitw_with_constants_in_sum(NodeType expType)
        {
            Assert.True((this.type) == (NodeType.SUM));
            List<(long, List<int>)> l = new() { };
            foreach (var i in Range.Get(this.children.Count()))
            {
                var (factor, node) = this.children[i].__get_factor_of_bitw_with_constant(expType);
                Assert.True(((factor) == (null)) == ((node) == (null)));
                if ((factor) == (null))
                {
                    continue;
                }
                var found = false;
                foreach (var pair in l)
                {
                    if ((factor) != (pair.Item1))
                    {
                        continue;
                    }
                    var sublist = pair.Item2;
                    var firstIdx = sublist[0];
                    var first = this.children[firstIdx];
                    if ((first.type) == (NodeType.PRODUCT))
                    {
                        first = first.children[1];
                    }
                    Assert.True((first.type) == (expType));
                    if ((node.children.Count()) != (first.children.Count()))
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
                    l.Add((factor, new() { i }));
                }
            }
            return l;
        }

        public (NullableI64, Node) __get_factor_of_bitw_with_constant(NodeType? expType = null)
        {
            NullableI64 factor = null;
            Node node = null;
            if (this.__is_bitwise_binop())
            {
                if ((((expType) != (null)) && ((this.type) != (expType))))
                {
                    return (null, null);
                }
                factor = 1;
                node = this;
            }
            else
            {
                if ((this.type) != (NodeType.PRODUCT))
                {
                    return (null, null);
                }
                else
                {
                    if ((this.children.Count()) != (2))
                    {
                        return (null, null);
                    }
                    else
                    {
                        if ((this.children[0].type) != (NodeType.CONSTANT))
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
                                if ((((expType) != (null)) && ((this.children[1].type) != (expType))))
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
            if ((node.children[0].type) != (NodeType.CONSTANT))
            {
                return (null, null);
            }
            return (factor, node);
        }

        public bool __check_bitw_pairs_with_constants()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            var changed = false;
            List<bool> bools = new() { true, false };
            foreach (var conj in bools)
            {
                if (this.__check_bitw_pairs_with_constants_impl(conj))
                {
                    changed = true;
                    if ((this.type) != (NodeType.SUM))
                    {
                        return true;
                    }
                }
            }
            return changed;
        }

        public bool __check_bitw_pairs_with_constants_impl(bool conj)
        {
            Assert.True((this.type) == (NodeType.SUM));
            var expType = (conj) ? NodeType.CONJUNCTION : NodeType.INCL_DISJUNCTION;
            var l = this.__collect_indices_of_bitw_with_constants_in_sum(expType);
            if ((l.Count()) == (0))
            {
                return false;
            }
            List<int> toRemove = new() { };
            var changed = false;
            foreach (var pair in l)
            {
                var factor = pair.Item1;
                var sublist = pair.Item2;
                foreach (var i in Range.Get(((sublist.Count()) - (1)), 0, -(1)))
                {
                    foreach (var j in Range.Get(0, i))
                    {
                        var firstIdx = sublist[j];
                        var first = this.children[firstIdx];
                        if ((first.type) == (NodeType.PRODUCT))
                        {
                            first = first.children[1];
                        }
                        Assert.True((first.type) == (expType));
                        var secIdx = sublist[i];
                        var second = this.children[secIdx];
                        if ((second.type) == (NodeType.PRODUCT))
                        {
                            second = second.children[1];
                        }
                        Assert.True((second.type) == (expType));
                        var firstConst = ((first.children[0].constant) % (this.__modulus));
                        var secConst = ((second.children[0].constant) % (this.__modulus));
                        if ((((firstConst) & (secConst))) != (0))
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
            toRemove.Sort();
            foreach (var idx in ListUtil.Reversed(toRemove))
            {
                this.children.RemoveAt(idx);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public bool __check_diff_bitw_pairs_with_constants()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            var l = this.__collect_all_indices_of_bitw_with_constants();
            if ((l.Count()) == (0))
            {
                return false;
            }
            List<int> toRemove = new() { };
            long _const = 0;
            var changed = false;
            foreach (var sublist in l)
            {
                foreach (var i in Range.Get(((sublist.Count()) - (1)), 0, -(1)))
                {
                    foreach (var j in Range.Get(0, i))
                    {
                        var (firstFactor, firstIdx) = sublist[j];
                        var first = this.children[firstIdx];
                        if ((first.type) == (NodeType.PRODUCT))
                        {
                            first = first.children[1];
                        }
                        var (secFactor, secIdx) = sublist[i];
                        var second = this.children[secIdx];
                        if ((second.type) == (NodeType.PRODUCT))
                        {
                            second = second.children[1];
                        }
                        if ((first.type) == (second.type))
                        {
                            continue;
                        }
                        var firstConst = ((first.children[0].constant) % (this.__modulus));
                        var secConst = ((second.children[0].constant) % (this.__modulus));
                        if ((((firstConst) & (secConst))) != (0))
                        {
                            continue;
                        }
                        var factor = this.__get_factor_for_merging_bitwise(firstFactor, secFactor, first.type, second.type);
                        if ((factor) == (null))
                        {
                            continue;
                        }
                        var (bitwFactor, remove, add) = this.__merge_bitwise_terms(firstIdx, secIdx, first, second, factor, firstConst, secConst);
                        _const += add;
                        if (remove)
                        {
                            toRemove.Add(secIdx);
                        }
                        //sublist[j][0] = bitwFactor;
                        sublist[j] = (bitwFactor, sublist[j].Item2);
                        changed = true;
                        break;
                    }
                }
            }
            if (!(changed))
            {
                return false;
            }
            toRemove.Sort();
            foreach (var idx in ListUtil.Reversed(toRemove))
            {
                this.children.RemoveAt(idx);
            }
            if ((this.children[0].type) == (NodeType.CONSTANT))
            {
                this.children[0].__set_and_reduce_constant(((this.children[0].constant) + (_const)));
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(_const));
            }
            if ((((this.children.Count()) > (1)) && (this.children[0].__is_constant(0))))
            {
                this.children.RemoveAt(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public List<List<(long, int)>> __collect_all_indices_of_bitw_with_constants()
        {
            Assert.True((this.type) == (NodeType.SUM));
            List<List<(long, int)>> l = new() { };
            foreach (var i in Range.Get(this.children.Count()))
            {
                var (factor, node) = this.children[i].__get_factor_of_bitw_with_constant();
                Assert.True(((factor) == (null)) == ((node) == (null)));
                if ((factor) == (null))
                {
                    continue;
                }
                Assert.True(node.__is_bitwise_binop());
                var found = false;
                foreach (var sublist in l)
                {
                    var firstIdx = sublist[0];
                    var first = this.children[firstIdx.Item2];
                    if ((first.type) == (NodeType.PRODUCT))
                    {
                        first = first.children[1];
                    }
                    if ((node.children.Count()) == (2))
                    {
                        if ((first.children.Count()) == (2))
                        {
                            if (!(node.children[1].equals(first.children[1])))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if ((node.children[1].type) != (first.type))
                            {
                                continue;
                            }
                            Assert.True((first.children.Count()) > (2));
                            if (!(do_children_match(node.children[1].children, first.children.Slice(1, null, null))))
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if ((first.children.Count()) == (2))
                        {
                            if ((first.children[1].type) != (node.type))
                            {
                                continue;
                            }
                            Assert.True((node.children.Count()) > (2));
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
                    sublist.Add((factor, i));
                    found = true;
                    break;
                }
                if (!(found))
                {
                    l.Add(new() { (factor, i) });
                }
            }
            return l;
        }

        public NullableI64 __get_factor_for_merging_bitwise(long fac1, long fac2, NodeType type1, NodeType type2)
        {
            if ((type1) == (type2))
            {
                if ((((((fac1) - (fac2))) % (this.__modulus))) != (0))
                {
                    return null;
                }
                return fac1;
            }
            if ((type1) == (NodeType.EXCL_DISJUNCTION))
            {
                if ((type2) == (NodeType.CONJUNCTION))
                {
                    if ((((((((2) * (fac1))) + (fac2))) % (this.__modulus))) != (0))
                    {
                        return null;
                    }
                }
                else
                {
                    if ((((((((2) * (fac1))) - (fac2))) % (this.__modulus))) != (0))
                    {
                        return null;
                    }
                }
                return fac1;
            }
            if ((type1) == (NodeType.CONJUNCTION))
            {
                if ((type2) == (NodeType.EXCL_DISJUNCTION))
                {
                    if ((((((fac1) + (((2) * (fac2))))) % (this.__modulus))) != (0))
                    {
                        return null;
                    }
                }
                else
                {
                    if ((((((fac1) + (fac2))) % (this.__modulus))) != (0))
                    {
                        return null;
                    }
                }
                return fac2;
            }
            if ((type2) == (NodeType.EXCL_DISJUNCTION))
            {
                if ((((((-(fac1)) + (((2) * (fac2))))) % (this.__modulus))) != (0))
                {
                    return null;
                }
                return fac2;
            }
            if ((((((fac1) + (fac2))) % (this.__modulus))) != (0))
            {
                return null;
            }
            return fac1;
        }

        public (long, bool, long) __merge_bitwise_terms(int firstIdx, int secIdx, Node first, Node second, long factor, long firstConst, long secConst)
        {
            var (bitwFactor, add, opfac) = this.__merge_bitwise_terms_and_get_opfactor(firstIdx, secIdx, first, second, factor, firstConst, secConst);
            if ((opfac) == (0))
            {
                return (bitwFactor, true, add);
            }
            if ((second.children.Count()) == (2))
            {
                this.children[secIdx] = second.children[1];
            }
            else
            {
                this.children[secIdx] = second.__get_shallow_copy();
                this.children[secIdx].children.RemoveAt(0);
            }
            if ((opfac) != (1))
            {
                this.children[secIdx].__multiply(opfac);
            }
            return (bitwFactor, false, add);
        }

        public (long, long, long) __merge_bitwise_terms_and_get_opfactor(int firstIdx, int secIdx, Node first, Node second, long factor, long firstConst, long secConst)
        {
            var constSum = ((firstConst) + (secConst));
            var constNeg = ((-(constSum)) - (1));
            var bitwFactor = this.__get_bitwise_factor_for_merging_bitwise(factor, first.type, second.type);
            var (opfac, add) = this.__get_operand_factor_and_constant_for_merging_bitwise(factor, first.type, second.type, firstConst, secConst);
            var hasFactor = (this.children[firstIdx].type) == (NodeType.PRODUCT);
            first.children[0].__set_and_reduce_constant(this.__get_const_operand_for_merging_bitwise(constSum, first.type, second.type));
            if ((((first.type) != (second.type)) || ((first.type) != (NodeType.INCL_DISJUNCTION))))
            {
                if ((((first.type) != (NodeType.CONJUNCTION)) && ((first.children.Count()) > (2))))
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
                if ((bitwFactor) == (1))
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
                if ((bitwFactor) != (1))
                {
                    var factorNode = this.__new_constant_node(bitwFactor);
                    var prod = this.__new_node_with_children(NodeType.PRODUCT, new() { factorNode, first.__get_shallow_copy() });
                    this.children[firstIdx].copy(prod);
                }
            }
            return (bitwFactor, add, opfac);
        }

        public long __get_const_operand_for_merging_bitwise(long constSum, NodeType type1, NodeType type2)
        {
            if ((((type1) == (type2)) && ((type1) != (NodeType.EXCL_DISJUNCTION))))
            {
                return constSum;
            }
            return ((-(constSum)) - (1));
        }

        public long __get_bitwise_factor_for_merging_bitwise(long factor, NodeType type1, NodeType type2)
        {
            if ((((type1) == (NodeType.EXCL_DISJUNCTION)) || ((type2) == (NodeType.EXCL_DISJUNCTION))))
            {
                return ((2) * (factor));
            }
            return factor;
        }

        public (long, long) __get_operand_factor_and_constant_for_merging_bitwise(long factor, NodeType type1, NodeType type2, long const1, long const2)
        {
            if ((type1) == (type2))
            {
                if ((type1) == (NodeType.CONJUNCTION))
                {
                    return (0, 0);
                }
                if ((type1) == (NodeType.INCL_DISJUNCTION))
                {
                    return (factor, 0);
                }
                return (0, ((((const1) + (const2))) * (factor)));
            }
            if ((type1) == (NodeType.EXCL_DISJUNCTION))
            {
                if ((type2) == (NodeType.CONJUNCTION))
                {
                    return (-(factor), ((const1) * (factor)));
                }
                return (factor, ((((const1) + (((2) * (const2))))) * (factor)));
            }
            if ((type1) == (NodeType.CONJUNCTION))
            {
                if ((type2) == (NodeType.EXCL_DISJUNCTION))
                {
                    return (-(factor), ((const2) * (factor)));
                }
                return (0, ((const2) * (factor)));
            }
            if ((type2) == (NodeType.EXCL_DISJUNCTION))
            {
                return (factor, ((((((2) * (const1))) + (const2))) * (factor)));
            }
            return (0, ((const1) * (factor)));
        }

        public bool __check_bitw_tuples_with_constants()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            var l = this.__collect_all_indices_of_bitw_with_constants();
            if ((l.Count()) == (0))
            {
                return false;
            }
            List<int> toRemove = new() { };
            long _const = 0;
            var changed = false;
            foreach (var sublist in l)
            {
                foreach (var i in Range.Get(((sublist.Count()) - (1)), 1, -(1)))
                {
                    var add = this.__try_merge_bitwise_with_constants_with_2_others(sublist, i, toRemove);
                    if ((add) != (null))
                    {
                        changed = true;
                        _const += add;
                    }
                }
            }
            if (!(changed))
            {
                return false;
            }
            toRemove.Sort();
            foreach (var idx in ListUtil.Reversed(toRemove))
            {
                this.children.RemoveAt(idx);
            }
            if ((this.children[0].type) == (NodeType.CONSTANT))
            {
                this.children[0].__set_and_reduce_constant(((this.children[0].constant) + (_const)));
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(_const));
            }
            if ((((this.children.Count()) > (1)) && (this.children[0].__is_constant(0))))
            {
                this.children.RemoveAt(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public NullableI64 __try_merge_bitwise_with_constants_with_2_others(List<(long, int)> sublist, int i, List<int> toRemove)
        {
            foreach (var j in Range.Get(1, i))
            {
                foreach (var k in Range.Get(0, j))
                {
                    var add = this.__try_merge_triple_bitwise_with_constants(sublist, i, j, k, toRemove);
                    if ((add) != (null))
                    {
                        return add;
                    }
                }
            }
            return null;
        }

        public NullableI64 __try_merge_triple_bitwise_with_constants(List<(long, int)> sublist, int i, int j, int k, List<int> toRemove)
        {
            List<List<int>> perms = new() { new() { i, j, k }, new() { j, i, k }, new() { k, i, j } };
            foreach (var perm in perms)
            {
                var (mainFactor, mainIdx) = sublist[perm[0]];
                var main = this.children[mainIdx];
                if ((main.type) == (NodeType.PRODUCT))
                {
                    main = main.children[1];
                }
                var mainConst = ((main.children[0].constant) % (this.__modulus));
                var (firstFactor, firstIdx) = sublist[perm[1]];
                var first = this.children[firstIdx];
                if ((first.type) == (NodeType.PRODUCT))
                {
                    first = first.children[1];
                }
                var firstConst = ((first.children[0].constant) % (this.__modulus));
                var (secFactor, secIdx) = sublist[perm[2]];
                var second = this.children[secIdx];
                if ((second.type) == (NodeType.PRODUCT))
                {
                    second = second.children[1];
                }
                var secConst = ((second.children[0].constant) % (this.__modulus));
                var (factor1, factor2) = this.__get_factors_for_merging_triple(first.type, second.type, main.type, firstFactor, secFactor, mainFactor, firstConst, secConst, mainConst);
                Assert.True(((factor1) == (null)) == ((factor2) == (null)));
                if ((factor1) == (null))
                {
                    continue;
                }
                var i1 = perm[1];
                if ((perm[0]) != (i))
                {
                    Assert.True((perm[1]) == (i));
                    //var(sublist[perm[0]], sublist[perm[1]]) = (sublist[perm[1]], sublist[perm[0]]);
                    var oldPerm0 = sublist[perm[0]];
                    sublist[perm[0]] = sublist[perm[1]];
                    sublist[perm[1]] = oldPerm0;
                    i1 = perm[0];
                }
                var (bitwFactor1, add1, opfac1) = this.__merge_bitwise_terms_and_get_opfactor(firstIdx, mainIdx, first, main, factor1, firstConst, mainConst);
                var (bitwFactor2, add2, opfac2) = this.__merge_bitwise_terms_and_get_opfactor(secIdx, mainIdx, second, main, factor2, secConst, mainConst);
                var opfac = ((((opfac1) + (opfac2))) % (this.__modulus));
                if ((opfac) == (null))
                {
                    toRemove.Add(mainIdx);
                }
                else
                {
                    this.children[mainIdx] = main.children[1];
                    if ((opfac) != (1))
                    {
                        this.children[mainIdx].__multiply(opfac);
                    }
                }
                //sublist[i1][0] = bitwFactor1;
                sublist[i1] = (bitwFactor1, sublist[i1].Item2);
                //sublist[perm[2]][0] = bitwFactor2;
                sublist[perm[2]] = (bitwFactor2, sublist[perm[2]].Item2);
                return ((add1) + (add2));
            }
            return null;
        }

        public (NullableI64, NullableI64) __get_factors_for_merging_triple(NodeType type1, NodeType type2, NodeType type0, long fac1, long fac2, long fac0, long const1, long const2, long const0)
        {
            if ((((const1) & (const0))) != (0))
            {
                return (null, null);
            }
            if ((((const2) & (const0))) != (0))
            {
                return (null, null);
            }
            var factor1 = this.__get_possible_factor_for_merging_bitwise(fac1, type1, type0);
            if ((factor1) == (null))
            {
                return (null, null);
            }
            var factor2 = this.__get_possible_factor_for_merging_bitwise(fac2, type2, type0);
            if ((factor2) == (null))
            {
                return (null, null);
            }
            if ((((((((factor1) + (factor2))) - (fac0))) % (this.__modulus))) != (0))
            {
                return (null, null);
            }
            factor1 = this.__get_factor_for_merging_bitwise(fac1, factor1, type1, type0);
            factor2 = this.__get_factor_for_merging_bitwise(fac2, factor2, type2, type0);
            Assert.True((factor1) != (null));
            Assert.True((factor2) != (null));
            return (factor1, factor2);
        }

        public NullableI64 __get_possible_factor_for_merging_bitwise(long fac1, NodeType type1, NodeType type0)
        {
            if ((type1) == (NodeType.EXCL_DISJUNCTION))
            {
                if ((type0) == (NodeType.CONJUNCTION))
                {
                    return ((-(2)) * (fac1));
                }
                if ((type0) == (NodeType.INCL_DISJUNCTION))
                {
                    return ((2) * (fac1));
                }
                return fac1;
            }
            if ((type1) == (NodeType.CONJUNCTION))
            {
                if ((type0) == (NodeType.EXCL_DISJUNCTION))
                {
                    if ((((fac1) % (2))) != (0))
                    {
                        return null;
                    }
                    return ((-(fac1)) / (2));
                }
                if ((type0) == (NodeType.CONJUNCTION))
                {
                    return fac1;
                }
                return -(fac1);
            }
            if ((type0) == (NodeType.EXCL_DISJUNCTION))
            {
                if ((((fac1) % (2))) != (0))
                {
                    return null;
                }
                return ((fac1) / (2));
            }
            if ((type0) == (NodeType.CONJUNCTION))
            {
                return -(fac1);
            }
            return fac1;
        }

        public bool __check_bitw_pairs_with_inverses()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            var changed = false;
            List<NodeType> expTypes = new() { NodeType.CONJUNCTION, NodeType.EXCL_DISJUNCTION, NodeType.INCL_DISJUNCTION };
            foreach (var expType in expTypes)
            {
                if (this.__check_bitw_pairs_with_inverses_impl(expType))
                {
                    changed = true;
                    if ((this.type) != (NodeType.SUM))
                    {
                        return true;
                    }
                }
            }
            return changed;
        }

        public bool __check_bitw_pairs_with_inverses_impl(NodeType expType)
        {
            Assert.True((this.type) == (NodeType.SUM));
            var l = this.__collect_indices_of_bitw_without_constants_in_sum(expType);
            if ((l.Count()) == (0))
            {
                return false;
            }
            List<int> toRemove = new() { };
            var changed = false;
            long _const = 0;
            foreach (var pair in l)
            {
                var factor = pair.Item1;
                var sublist = pair.Item3;
                var done = sublist.Where(x => true).Select(x => false).ToList();
                foreach (var i in Range.Get(((sublist.Count()) - (1)), 0, -(1)))
                {
                    if (done[i])
                    {
                        continue;
                    }
                    foreach (var j in Range.Get(0, i))
                    {
                        if (done[j])
                        {
                            continue;
                        }
                        var firstIdx = sublist[j];
                        var first = this.children[firstIdx];
                        if ((first.type) == (NodeType.PRODUCT))
                        {
                            first = first.children[1];
                        }
                        Assert.True((first.type) == (expType));
                        var secIdx = sublist[i];
                        var second = this.children[secIdx];
                        if ((second.type) == (NodeType.PRODUCT))
                        {
                            second = second.children[1];
                        }
                        Assert.True((second.type) == (expType));
                        var indices = first.__get_only_differing_child_indices(second);
                        if ((indices) == (null))
                        {
                            continue;
                        }
                        if (!(first.children[indices.Value.Item1].equals_negated(second.children[indices.Value.Item2])))
                        {
                            continue;
                        }
                        var (removeFirst, removeSec, add) = this.__merge_inverse_bitwise_terms(firstIdx, secIdx, first, second, factor, indices.Value);
                        _const += add;
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
            if ((toRemove.Count()) > (0))
            {
                toRemove.Sort();
                foreach (var idx in ListUtil.Reversed(toRemove))
                {
                    this.children.RemoveAt(idx);
                }
            }
            if ((this.children.Count()) == (0))
            {
                this.copy(this.__new_constant_node(_const));
                return true;
            }
            if ((this.children[0].type) == (NodeType.CONSTANT))
            {
                this.children[0].__set_and_reduce_constant(((this.children[0].constant) + (_const)));
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(_const));
            }
            if ((((this.children.Count()) > (1)) && (this.children[0].__is_constant(0))))
            {
                this.children.RemoveAt(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public List<(long, int, List<int>)> __collect_indices_of_bitw_without_constants_in_sum(NodeType expType)
        {
            Assert.True((this.type) == (NodeType.SUM));
            List<(long, int, List<int>)> l = new() { };
            foreach (var i in Range.Get(this.children.Count()))
            {
                var (factor, node) = this.children[i].__get_factor_of_bitw_without_constant(expType);
                Assert.True(((factor) == (null)) == ((node) == (null)));
                if ((factor) == (null))
                {
                    continue;
                }
                var opCnt = node.children.Count();
                var found = false;
                foreach (var triple in l)
                {
                    if ((factor) != (triple.Item1))
                    {
                        continue;
                    }
                    if ((opCnt) != (triple.Item2))
                    {
                        continue;
                    }
                    triple.Item3.Add(i);
                    found = true;
                    break;
                }
                if (!(found))
                {
                    l.Add((factor, opCnt, new() { i }));
                }
            }
            return l;
        }

        public (NullableI64, Node) __get_factor_of_bitw_without_constant(NodeType? expType = null)
        {
            NullableI64 factor = null;
            Node node = null;
            if (this.__is_bitwise_binop())
            {
                if ((((expType) != (null)) && ((this.type) != (expType))))
                {
                    return (null, null);
                }
                factor = 1;
                node = this;
            }
            else
            {
                if ((this.type) != (NodeType.PRODUCT))
                {
                    return (null, null);
                }
                else
                {
                    if ((this.children.Count()) != (2))
                    {
                        return (null, null);
                    }
                    else
                    {
                        if ((this.children[0].type) != (NodeType.CONSTANT))
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
                                if ((((expType) != (null)) && ((this.children[1].type) != (expType))))
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
            if ((node.children[0].type) == (NodeType.CONSTANT))
            {
                return (null, null);
            }
            return (factor, node);
        }

        public (int, int)? __get_only_differing_child_indices(Node other)
        {
            if ((this.type) == (other.type))
            {
                if ((this.children.Count()) != (other.children.Count()))
                {
                    return null;
                }
                return this.__get_only_differing_child_indices_same_len(other);
            }
            if ((this.children.Count()) == (other.children.Count()))
            {
                if ((this.children.Count()) != (2))
                {
                    return null;
                }
                return this.__get_only_differing_child_indices_same_len(other);
            }
            if ((this.children.Count()) < (other.children.Count()))
            {
                if ((this.children.Count()) != (2))
                {
                    return null;
                }
                return this.__get_only_differing_child_indices_diff_len(other);
            }
            if ((other.children.Count()) != (2))
            {
                return null;
            }
            var indices = other.__get_only_differing_child_indices_diff_len(this);
            if ((indices) == (null))
            {
                return null;
            }
            return (indices.Value.Item2, indices.Value.Item1);
        }

        public (int, int)? __get_only_differing_child_indices_same_len(Node other)
        {
            Assert.True((((this.type) == (other.type)) || ((this.children.Count()) == (2))));
            Assert.True((this.children.Count()) == (other.children.Count()));
            NullableI32 idx1 = null;
            List<int> oIndices = new(Range.Get(other.children.Count()));
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child = this.children[i];
                var found = false;
                foreach (var j in oIndices.ToList())
                {
                    if (child.equals(other.children[j]))
                    {
                        oIndices.Remove(j);
                        found = true;
                    }
                }
                if (!(found))
                {
                    if ((idx1) == (null))
                    {
                        idx1 = i;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            if ((idx1) == (null))
            {
                return null;
            }
            Assert.True((oIndices.Count()) == (1));
            return (idx1, oIndices[0]);
        }

        public (int, int)? __get_only_differing_child_indices_diff_len(Node other)
        {
            Assert.True((this.type) != (other.type));
            Assert.True((this.children.Count()) == (2));
            Assert.True((other.children.Count()) > (2));
            List<int> nums = new() { 0, 1 };
            foreach (var i in nums)
            {
                var idx = other.__get_index_of_child_negated(this.children[i]);
                if ((idx) == (null))
                {
                    continue;
                }
                var oi = ((i) == (0)) ? 1 : 0;
                if ((this.children[oi].type) != (other.type))
                {
                    continue;
                }
                var op1 = (other.children.Slice(null, idx, null));
                var op2 = (other.children.Slice(((idx) + (1)), null, null));
                if (!(do_children_match(this.children[oi].children, (op1.Concat(op2).ToList()))))
                {
                    continue;
                }
                return (i, idx);
            }
            return null;
        }

        public (bool, bool, long) __merge_inverse_bitwise_terms(int firstIdx, int secIdx, Node first, Node second, long factor, (int, int) indices)
        {
            var type1 = first.type;
            var type2 = second.type;
            var (invOpFac, sameOpFac, add) = this.__get_operand_factors_and_constant_for_merging_inverse_bitwise(factor, type1, type2);
            Node factorNode = null;
            Node prod = null;
            bool hasFactor = false;
            var removeFirst = (sameOpFac) == (0);
            if (!(removeFirst))
            {
                hasFactor = (this.children[firstIdx].type) == (NodeType.PRODUCT);
                if ((first.children.Count()) == (2))
                {
                    List<int> nums = new() { 0, 1 };
                    Assert.True(((nums).Contains(indices.Item1)));
                    var oIdx = ((indices.Item1) == (1)) ? 0 : 1;
                    first.copy(first.children[oIdx]);
                }
                else
                {
                    first.children.RemoveAt(indices.Item1);
                }
                if (hasFactor)
                {
                    if ((sameOpFac) == (1))
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
                    if ((sameOpFac) != (1))
                    {
                        factorNode = this.__new_constant_node(sameOpFac);
                        List<Node> prod0_children = new() { factorNode, first.__get_shallow_copy() };
                        prod = this.__new_node_with_children(NodeType.PRODUCT, prod0_children);
                        this.children[firstIdx].copy(prod);
                    }
                }
                this.children[firstIdx].__flatten();
            }
            var removeSecond = (invOpFac) == (0);
            if (!(removeSecond))
            {
                hasFactor = (this.children[secIdx].type) == (NodeType.PRODUCT);
                second.copy(second.children[indices.Item2]);
                if (this.__must_invert_at_merging_inverse_bitwise(type1, type2))
                {
                    second.__negate();
                }
                if (hasFactor)
                {
                    if ((invOpFac) == (1))
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
                    if ((invOpFac) != (1))
                    {
                        factorNode = this.__new_constant_node(invOpFac);
                        List<Node> prod1_children = new() { factorNode, second.__get_shallow_copy() };
                        prod = this.__new_node_with_children(NodeType.PRODUCT, prod1_children);
                        this.children[secIdx].copy(prod);
                    }
                }
                this.children[secIdx].__flatten();
            }
            return (removeFirst, removeSecond, add);
        }

        public (long, long, long) __get_operand_factors_and_constant_for_merging_inverse_bitwise(long factor, NodeType type1, NodeType type2)
        {
            if ((type1) == (type2))
            {
                if ((type1) == (NodeType.CONJUNCTION))
                {
                    return (0, factor, 0);
                }
                if ((type1) == (NodeType.INCL_DISJUNCTION))
                {
                    return (0, factor, -(factor));
                }
                return (0, 0, -(factor));
            }
            if ((type1) == (NodeType.EXCL_DISJUNCTION))
            {
                if ((type2) == (NodeType.CONJUNCTION))
                {
                    return (factor, -(factor), 0);
                }
                return (-(factor), factor, ((-(2)) * (factor)));
            }
            if ((type1) == (NodeType.CONJUNCTION))
            {
                if ((type2) == (NodeType.EXCL_DISJUNCTION))
                {
                    return (factor, -(factor), 0);
                }
                return (factor, 0, 0);
            }
            if ((type2) == (NodeType.EXCL_DISJUNCTION))
            {
                return (-(factor), factor, ((-(2)) * (factor)));
            }
            return (factor, 0, 0);
        }

        public bool __must_invert_at_merging_inverse_bitwise(NodeType type1, NodeType type2)
        {
            Assert.True((type1) != (type2));
            if ((type1) == (NodeType.EXCL_DISJUNCTION))
            {
                return true;
            }
            if ((type2) == (NodeType.EXCL_DISJUNCTION))
            {
                return false;
            }
            return (type1) == (NodeType.INCL_DISJUNCTION);
        }

        public bool __check_diff_bitw_pairs_with_inverses()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            var l = this.__collect_all_indices_of_bitw_without_constants();
            if ((l.Count()) == (0))
            {
                return false;
            }
            List<int> toRemove = new() { };
            var done = l.Where(x => true).Select(x => false).ToList();
            var changed = false;
            long _const = 0;
            foreach (var i in Range.Get(((l.Count()) - (1)), 0, -(1)))
            {
                if (done[i])
                {
                    continue;
                }
                foreach (var j in Range.Get(0, i))
                {
                    if (done[j])
                    {
                        continue;
                    }
                    var (firstFactor, firstIdx) = l[j];
                    var first = this.children[firstIdx];
                    if ((first.type) == (NodeType.PRODUCT))
                    {
                        first = first.children[1];
                    }
                    var (secFactor, secIdx) = l[i];
                    var second = this.children[secIdx];
                    if ((second.type) == (NodeType.PRODUCT))
                    {
                        second = second.children[1];
                    }
                    if ((first.type) == (second.type))
                    {
                        continue;
                    }
                    var factor = this.__get_factor_for_merging_bitwise(firstFactor, secFactor, first.type, second.type);
                    if ((factor) == (null))
                    {
                        continue;
                    }
                    var indices = first.__get_only_differing_child_indices(second);
                    if ((indices) == (null))
                    {
                        continue;
                    }
                    if (!(first.children[indices.Value.Item1].equals_negated(second.children[indices.Value.Item2])))
                    {
                        continue;
                    }
                    var (removeFirst, removeSec, add) = this.__merge_inverse_bitwise_terms(firstIdx, secIdx, first, second, factor, indices.Value);
                    _const += add;
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
            if ((toRemove.Count()) > (0))
            {
                toRemove.Sort();
                foreach (var idx in ListUtil.Reversed(toRemove))
                {
                    this.children.RemoveAt(idx);
                }
            }
            if ((this.children.Count()) == (0))
            {
                this.copy(this.__new_constant_node(_const));
                return true;
            }
            if ((this.children[0].type) == (NodeType.CONSTANT))
            {
                this.children[0].__set_and_reduce_constant(((this.children[0].constant) + (_const)));
            }
            else
            {
                this.children.Insert(0, this.__new_constant_node(_const));
            }
            if ((((this.children.Count()) > (1)) && (this.children[0].__is_constant(0))))
            {
                this.children.RemoveAt(0);
            }
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public List<(long, int)> __collect_all_indices_of_bitw_without_constants()
        {
            Assert.True((this.type) == (NodeType.SUM));
            List<(long, int)> l = new() { };
            foreach (var i in Range.Get(this.children.Count()))
            {
                var (factor, node) = this.children[i].__get_factor_of_bitw_without_constant();
                Assert.True(((factor) == (null)) == ((node) == (null)));
                if ((factor) == (null))
                {
                    continue;
                }
                l.Add((factor, i));
            }
            return l;
        }

        public bool __check_bitw_and_op_in_sum()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            foreach (var bitwIdx in Range.Get(this.children.Count()))
            {
                var bitw = this.children[bitwIdx];
                var disj = (bitw.type) == (NodeType.INCL_DISJUNCTION);
                if (!(disj))
                {
                    if ((bitw.type) != (NodeType.PRODUCT))
                    {
                        continue;
                    }
                    if (!(bitw.children[0].__is_constant(-(1))))
                    {
                        continue;
                    }
                    bitw = bitw.children[1];
                    if ((bitw.type) != (NodeType.CONJUNCTION))
                    {
                        continue;
                    }
                }
                Node other = null;
                int oIdx = 0;
                if ((this.children.Count()) == (2))
                {
                    oIdx = ((bitwIdx) == (0)) ? 1 : 0;
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
                if ((idx) == (null))
                {
                    continue;
                }
                this.copy(bitw);
                if ((this.children.Count()) > (2))
                {
                    var node = bitw.__get_shallow_copy();
                    node.children.RemoveAt(idx);
                    List<Node> neg0_children = new() { node };
                    var neg = this.__new_node_with_children(NodeType.NEGATION, neg0_children);
                    this.children = new() { this.children[idx].__get_shallow_copy(), neg };
                }
                else
                {
                    oIdx = ((idx) == (0)) ? 1 : 0;
                    this.children[oIdx].__negate();
                }
                if (disj)
                {
                    List<Node> neg1_children = new() { this.__get_shallow_copy() };
                    this.copy(this.__new_node_with_children(NodeType.NEGATION, neg1_children));
                }
                return true;
            }
            return false;
        }

        public bool __check_insert_xor_in_sum()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return false;
            }
            var changed = false;
            var i = -(1);
            while (true)
            {
                i += 1;
                if ((i) >= (this.children.Count()))
                {
                    break;
                }
                var first = this.children[i].get_copy();
                first.__multiply_by_minus_one();
                List<NodeType> firstTypes = new() { NodeType.CONJUNCTION, NodeType.PRODUCT };
                if (!((firstTypes).Contains(first.type)))
                {
                    continue;
                }
                if ((((first.type) != (NodeType.PRODUCT)) && ((first.children.Count()) != (2))))
                {
                    continue;
                }
                foreach (var j in Range.Get(this.children.Count()))
                {
                    if ((i) == (j))
                    {
                        continue;
                    }
                    var disj = this.children[j];
                    List<NodeType> disjTypes = new() { NodeType.INCL_DISJUNCTION, NodeType.PRODUCT };
                    if (!((disjTypes).Contains(disj.type)))
                    {
                        continue;
                    }
                    if (((first.type) == (NodeType.PRODUCT)) != ((disj.type) == (NodeType.PRODUCT)))
                    {
                        continue;
                    }
                    var conj = first;
                    if ((conj.type) == (NodeType.PRODUCT))
                    {
                        var indices = conj.__get_only_differing_child_indices(disj);
                        if ((indices) == (null))
                        {
                            continue;
                        }
                        conj = conj.children[indices.Value.Item1];
                        disj = disj.children[indices.Value.Item2];
                        if ((conj.type) != (NodeType.CONJUNCTION))
                        {
                            continue;
                        }
                        if ((disj.type) != (NodeType.INCL_DISJUNCTION))
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
            if ((this.children.Count()) == (1))
            {
                this.copy(this.children[0]);
            }
            return true;
        }

        public bool is_linear()
        {
            return (((this.state) == (NodeState.BITWISE)) || ((this.state) == (NodeState.LINEAR)));
        }

        public void mark_linear(bool restrictedScope = false)
        {
            foreach (var c in this.children)
            {
                if (((!(restrictedScope)) || ((c.state) == (NodeState.UNKNOWN))))
                {
                    c.mark_linear();
                }
            }
            if ((this.type) == (NodeType.INCL_DISJUNCTION))
            {
                this.__mark_linear_bitwise();
            }
            else
            {
                if ((this.type) == (NodeType.EXCL_DISJUNCTION))
                {
                    this.__mark_linear_bitwise();
                }
                else
                {
                    if ((this.type) == (NodeType.CONJUNCTION))
                    {
                        this.__mark_linear_bitwise();
                    }
                    else
                    {
                        if ((this.type) == (NodeType.SUM))
                        {
                            this.__mark_linear_sum();
                        }
                        else
                        {
                            if ((this.type) == (NodeType.PRODUCT))
                            {
                                this.__mark_linear_product();
                            }
                            else
                            {
                                if ((this.type) == (NodeType.NEGATION))
                                {
                                    this.__mark_linear_bitwise();
                                }
                                else
                                {
                                    if ((this.type) == (NodeType.POWER))
                                    {
                                        this.__mark_linear_power();
                                    }
                                    else
                                    {
                                        if ((this.type) == (NodeType.VARIABLE))
                                        {
                                            this.__mark_linear_variable();
                                        }
                                        else
                                        {
                                            if ((this.type) == (NodeType.CONSTANT))
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
                Assert.True((c.state) != (NodeState.UNKNOWN));
                if ((c.state) != (NodeState.BITWISE))
                {
                    this.state = NodeState.MIXED;
                    return;
                }
            }
            this.state = NodeState.BITWISE;
        }

        public void __mark_linear_sum()
        {
            Assert.True((this.children.Count()) > (1));
            this.state = NodeState.UNKNOWN;
            foreach (var c in this.children)
            {
                Assert.True((c.state) != (NodeState.UNKNOWN));
                if ((c.state) == (NodeState.MIXED))
                {
                    this.state = NodeState.MIXED;
                    return;
                }
                else
                {
                    if ((c.state) == (NodeState.NONLINEAR))
                    {
                        this.state = NodeState.NONLINEAR;
                    }
                }
            }
            if ((this.state) != (NodeState.NONLINEAR))
            {
                this.state = NodeState.LINEAR;
            }
        }

        public void __mark_linear_product()
        {
            Assert.True((this.children.Count()) > (0));
            if ((this.children.Count()) < (2))
            {
                this.state = this.children[0].state;
            }
            foreach (var c in this.children)
            {
                Assert.True((c.state) != (NodeState.UNKNOWN));
                if ((c.state) == (NodeState.MIXED))
                {
                    this.state = NodeState.MIXED;
                    return;
                }
            }
            if ((this.children.Count()) > (2))
            {
                this.state = NodeState.NONLINEAR;
            }
            else
            {
                if ((((this.children[0].type) == (NodeType.CONSTANT)) && (this.children[1].is_linear())))
                {
                    this.state = NodeState.LINEAR;
                }
                else
                {
                    if ((((this.children[1].type) == (NodeType.CONSTANT)) && (this.children[0].is_linear())))
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
                Assert.True((c.state) != (NodeState.UNKNOWN));
                if ((c.state) == (NodeState.MIXED))
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
            if (((this.__is_constant(0)) || (this.__is_constant(-(1)))))
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
            if ((this.type) == (NodeType.POWER))
            {
                return;
            }
            if ((((this.state) != (NodeState.NONLINEAR)) && ((this.state) != (NodeState.MIXED))))
            {
                this.linearEnd = this.children.Count();
                return;
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                this.__reorder_and_determine_linear_end_product();
                return;
            }
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child = this.children[i];
                List<NodeType> types = new() { NodeType.CONJUNCTION, NodeType.EXCL_DISJUNCTION, NodeType.INCL_DISJUNCTION, NodeType.NEGATION };
                var bitwise = ((types).Contains(this.type));
                if ((((child.state) == (NodeState.BITWISE)) || (((!(bitwise)) && ((child.state) == (NodeState.LINEAR))))))
                {
                    if ((this.linearEnd) < (i))
                    {
                        this.children.Remove(child);
                        this.children.Insert(this.linearEnd, child);
                    }
                    this.linearEnd += 1;
                }
            }
        }

        public void __reorder_and_determine_linear_end_product()
        {
            if ((this.children[0].type) != (NodeType.CONSTANT))
            {
                return;
            }
            this.linearEnd = 1;
            foreach (var i in Range.Get(1, this.children.Count()))
            {
                var child = this.children[i];
                if ((((child.state) != (NodeState.NONLINEAR)) && ((child.state) != (NodeState.MIXED))))
                {
                    if ((this.linearEnd) < (i))
                    {
                        this.children.Remove(child);
                        this.children.Insert(this.linearEnd, child);
                    }
                    this.linearEnd += 1;
                    if ((this.linearEnd) == (2))
                    {
                        return;
                    }
                }
            }
        }

        public Node get_node_for_substitution(List<Node> ignoreList)
        {
            if (this.__is_contained(ignoreList))
            {
                return null;
            }
            Node node = null;
            if (this.__is_bitwise_op())
            {
                foreach (var child in this.children)
                {
                    if ((((child.type) == (NodeType.CONSTANT)) || (child.__is_arithm_op())))
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
                            node = child.get_node_for_substitution(ignoreList);
                            if ((node) != (null))
                            {
                                return node;
                            }
                        }
                    }
                }
                return null;
            }
            if ((this.type) == (NodeType.POWER))
            {
                return this.children[0].get_node_for_substitution(ignoreList);
            }
            foreach (var child in this.children)
            {
                node = child.get_node_for_substitution(ignoreList);
                if ((node) != (null))
                {
                    return node;
                }
            }
            return null;
        }

        public bool __is_contained(List<Node> l)
        {
            return (this.__get_index_in_list(l)) != (null);
        }

        public NullableI32 __get_index_in_list(List<Node> l)
        {
            foreach (var i in Range.Get(l.Count()))
            {
                if (this.equals(l[i]))
                {
                    return i;
                }
            }
            return null;
        }

        public bool substitute_all_occurences(Node node, string vname, bool onlyFullMatch = false, bool withMod = true)
        {
            if ((this.type) == (NodeType.POWER))
            {
                return this.children[0].substitute_all_occurences(node, vname, onlyFullMatch, withMod);
            }
            var changed = false;
            var bitwise = this.__is_bitwise_op();
            Node inv = null;
            if (((!(bitwise)) && (!(onlyFullMatch)) && (withMod)))
            {
                inv = node.get_copy();
                inv.__multiply_by_minus_one();
            }
            bool ch = false;
            bool done = false;
            foreach (var child in this.children)
            {
                (ch, done) = child.__try_substitute_node(node, vname, bitwise);
                if (ch)
                {
                    changed = true;
                }
                if (done)
                {
                    continue;
                }
                if (((!(bitwise)) && (!(onlyFullMatch)) && (withMod)))
                {
                    Assert.True((inv) != (null));
                    (ch, done) = child.__try_substitute_node(inv, vname, false, true);
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
                if (((bitwise) && (!(child.__is_bitwise_op()))))
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

        public (bool, bool) __try_substitute_node(Node node, string vname, bool onlyFull, bool inverse = false)
        {
            Node var = null;
            if (this.equals(node))
            {
                var = this.__new_variable_node(vname);
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
            if ((((node.children.Count()) > (1)) && ((node.type) == (this.type)) && ((this.children.Count()) > (node.children.Count()))))
            {
                if (node.__are_all_children_contained(this))
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

        public bool __try_substitute_part_of_sum(Node node, string vname, bool inverse = false)
        {
            if ((((node.type) != (NodeType.SUM)) || ((node.children.Count()) <= (1))))
            {
                return false;
            }
            if ((this.type) == (NodeType.SUM))
            {
                return this.__try_substitute_part_of_sum_in_sum(node, vname, inverse);
            }
            return this.__try_substitute_part_of_sum_term(node, vname, inverse);
        }

        public bool __try_substitute_part_of_sum_in_sum(Node node, string vname, bool inverse)
        {
            Assert.True((this.type) == (NodeType.SUM));
            var common = this.__get_common_children(node);
            if ((common.Count()) == (0))
            {
                return false;
            }
            foreach (var c in common)
            {
                this.children.Remove(c);
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
                    if (c.equals(c2))
                    {
                        common.Remove(c2);
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

        public List<Node> __get_common_children(Node other)
        {
            Assert.True((other.type) == (this.type));
            List<Node> common = new() { };
            List<int> oIndices = new(Range.Get(other.children.Count()));
            foreach (var child in this.children)
            {
                foreach (var i in oIndices)
                {
                    if (child.equals(other.children[i]))
                    {
                        oIndices.Remove(i);
                        common.Add(child);
                        break;
                    }
                }
            }
            return common;
        }

        public bool __try_substitute_part_of_sum_term(Node node, string vname, bool inverse)
        {
            Assert.True((this.type) != (NodeType.SUM));
            if (!(node.__has_child(this)))
            {
                return false;
            }
            var var = this.__new_variable_node(vname);
            if (inverse)
            {
                var.__multiply_by_minus_one();
            }
            List<Node> sum_children = new() { var };
            var sumNode = this.__new_node_with_children(NodeType.SUM, sum_children);
            var found = false;
            foreach (var c in node.children)
            {
                if (((!(found)) && (this.equals(c))))
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

        public int count_nodes(List<NodeType> typeList = null)
        {
            var cnt = 0;
            foreach (var child in this.children)
            {
                cnt += child.count_nodes(typeList);
            }
            if ((((typeList) == (null)) || (((typeList).Contains(this.type)))))
            {
                cnt += 1;
            }
            return cnt;
        }

        public int compute_alternation_linear(bool hasParent = false)
        {
            List<NodeType> types = new() { NodeType.SUM, NodeType.PRODUCT };
            if (((types).Contains(this.type)))
            {
                Assert.True((this.children.Count()) > (0));
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
            return Convert.ToInt32((((this.type) != (NodeType.VARIABLE)) && ((this.type) != (NodeType.CONSTANT))));
        }

        public int compute_alternation(bool? parentBitwise = null)
        {
            if ((this.type) == (NodeType.VARIABLE))
            {
                return 0;
            }
            if ((this.type) == (NodeType.CONSTANT))
            {
                return Convert.ToInt32((((parentBitwise) != (null)) && ((parentBitwise) == (true))));
            }
            var bitw = this.__is_bitwise_op();
            var cnt = Convert.ToInt32((((parentBitwise) != (null)) && ((parentBitwise) != (bitw))));
            foreach (var child in this.children)
            {
                cnt += child.compute_alternation(bitw);
            }
            return cnt;
        }

        public void polish(Node parent = null)
        {
            foreach (var c in this.children)
            {
                c.polish(this);
            }
            this.__reorder_variables();
            this.__resolve_bitwise_negations_in_sums();
            this.__insert_bitwise_negations(parent);
            this.__reorder_variables();
        }

        public void __resolve_bitwise_negations_in_sums()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return;
            }
            var count = 0;
            foreach (var i in Range.Get(this.children.Count()))
            {
                if ((this.children[i].type) != (NodeType.NEGATION))
                {
                    continue;
                }
                this.children[i] = this.children[i].children[0];
                this.children[i].__multiply_by_minus_one();
                count += 1;
            }
            if ((count) != (0))
            {
                if ((this.children[0].type) == (NodeType.CONSTANT))
                {
                    this.children[0].__set_and_reduce_constant(((this.children[0].constant) - (count)));
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
            if ((this.children[0].type) != (NodeType.CONSTANT))
            {
                return;
            }
            var negConst = ((-(this.children[0].constant)) % (this.__modulus));
            if ((this.children.Count()) < (negConst))
            {
                return;
            }
            var countM = this.__count_children_mult_by_minus_one();
            if ((countM) < (negConst))
            {
                return;
            }
            var todo = negConst;
            foreach (var i in Range.Get(this.children.Count()))
            {
                var child = this.children[i];
                if ((todo) == (0))
                {
                    break;
                }
                if ((child.type) != (NodeType.PRODUCT))
                {
                    continue;
                }
                if (!(child.children[0].__is_constant(-(1))))
                {
                    continue;
                }
                child.children.RemoveAt(0);
                Assert.True((child.children.Count()) > (0));
                if ((child.children.Count()) == (1))
                {
                    child.type = NodeType.NEGATION;
                }
                else
                {
                    List<Node> neg_children = new() { child.__get_shallow_copy() };
                    this.children[i] = this.__new_node_with_children(NodeType.NEGATION, neg_children);
                }
                todo -= 1;
            }
            Assert.True((todo) == (0));
            this.children.RemoveAt(0);
        }

        public int __count_children_mult_by_minus_one()
        {
            var count = 0;
            foreach (var child in this.children)
            {
                if ((child.type) != (NodeType.PRODUCT))
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

        public void __insert_bitwise_negations(Node parent)
        {
            var (child, factor) = this.__get_opt_transformed_negated_with_factor();
            Assert.True(((child) != (null)) == ((factor) != (null)));
            if ((child) == (null))
            {
                return;
            }
            this.type = NodeType.NEGATION;
            this.children = new() { child };
            if ((factor) == (1))
            {
                return;
            }
            if ((((parent) != (null)) && ((parent.type) == (NodeType.PRODUCT))))
            {
                parent.__multiply(factor);
            }
            else
            {
                this.__multiply(factor);
            }
        }

        public (Node, NullableI64) __get_opt_transformed_negated_with_factor()
        {
            if ((this.type) != (NodeType.SUM))
            {
                return (null, null);
            }
            if ((this.children.Count()) < (2))
            {
                return (null, null);
            }
            if ((this.children[0].type) != (NodeType.CONSTANT))
            {
                return (null, null);
            }
            var factor = this.children[0].constant;
            var res = this.__new_node(NodeType.SUM);
            foreach (var i in Range.Get(1, this.children.Count()))
            {
                res.children.Add(this.children[i].get_copy());
                var child = res.children[-(1)];
                if ((((((factor) - (1))) % (this.__modulus))) == (0))
                {
                    continue;
                }
                if ((((((factor) + (1))) % (this.__modulus))) == (0))
                {
                    child.__multiply_by_minus_one();
                    continue;
                }
                if ((child.type) != (NodeType.PRODUCT))
                {
                    return (null, null);
                }
                if ((child.children[0].type) != (NodeType.CONSTANT))
                {
                    return (null, null);
                }
                var constNode = child.children[0];
                var c = this.__get_reduced_constant_closer_to_zero(constNode.constant);
                if ((((c) % (factor))) != (0))
                {
                    return (null, null);
                }
                constNode.__set_and_reduce_constant(((c) / (factor)));
                if (constNode.__is_constant(1))
                {
                    res.children[-(1)].children.RemoveAt(0);
                    if ((child.children.Count()) == (1))
                    {
                        res.children[-(1)] = child.children[0];
                    }
                }
            }
            Assert.True((res.children.Count()) > (0));
            if ((res.children.Count()) == (1))
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
            if ((this.type) < (NodeType.PRODUCT))
            {
                return;
            }
            if ((this.children.Count()) <= (1))
            {
                return;
            }
            this.children.Sort();
        }

        public bool __lt__(Node other)
        {
            if ((this.type) == (NodeType.CONSTANT))
            {
                return true;
            }
            if ((other.type) == (NodeType.CONSTANT))
            {
                return false;
            }
            var vn1 = this.__get_extended_variable();
            var vn2 = other.__get_extended_variable();
            if ((vn1) != (null))
            {
                if ((vn2) == (null))
                {
                    return true;
                }
                if ((vn1) != (vn2))
                {
                    //return (vn1) < (vn2);
                    return String.Compare(vn1, vn2) < 0;
                }
                return (this.type) == (NodeType.VARIABLE);
            }
            if ((vn2) != (null))
            {
                return false;
            }
            return ((this.type) != (other.type)) ? (this.type) < (other.type) : (this.children.Count()) < (other.children.Count());
        }

        public string __get_extended_variable()
        {
            if ((this.type) == (NodeType.VARIABLE))
            {
                return this.vname;
            }
            if ((this.type) == (NodeType.NEGATION))
            {
                return ((this.children[0].type) == (NodeType.VARIABLE)) ? this.children[0].vname : null;
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                if ((((this.children.Count()) == (2)) && ((this.children[0].type) == (NodeType.CONSTANT)) && ((this.children[1].type) == (NodeType.VARIABLE))))
                {
                    return this.children[1].vname;
                }
                return null;
            }
            return null;
        }

        public bool check_verify(Node other, int bitCount = 2)
        {
            return true;
        }

        public int count_terms_linear()
        {
            Assert.True(this.is_linear());
            if ((this.type) == (NodeType.SUM))
            {
                var t = 0;
                foreach (var child in this.children)
                {
                    t += child.count_terms_linear();
                }
                return t;
            }
            if ((this.type) == (NodeType.PRODUCT))
            {
                Assert.True((this.children.Count()) == (2));
                if ((this.children[0].type) == (NodeType.CONSTANT))
                {
                    return this.children[1].count_terms_linear();
                }
                Assert.True((this.children[1].type) == (NodeType.CONSTANT));
                return this.children[0].count_terms_linear();
            }
            return 1;
        }

        public void print(int level = 0)
        {
            /*
            var indent = ((2) * (level));
            var prefix = ((((((((indent) * (" "))) + ("["))) + (str(level)))) + ("] "));
            if ((this.type) == (NodeType.CONSTANT))
            {
                print(((((prefix) + ("CONST "))) + (str(this.constant))));
                return;
            }
            if ((this.type) == (NodeType.VARIABLE))
            {
                print(((((((((((prefix) + ("VAR "))) + (this.vname))) + (" [vidx "))) + (str(this.__vidx)))) + ("]")));
                return;
            }
            print(((prefix) + (str(this.type))));
            foreach (var c in this.children)
            {
                c.print(((level) + (1)));
            }
            */
            throw new InvalidOperationException();
        }

    }
}
