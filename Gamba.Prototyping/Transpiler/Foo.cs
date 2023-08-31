using Gamba.Prototyping.Extensions;
using GambaDotnet;
using Microsoft.Z3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Gamba.Prototyping.Transpiler
{
    public class Parser
    {
        string __expr;

        long __modulus;

        bool __modRed;

        int __idx;

        string __error;

        public Parser(string expr, long modulus, bool modRed = false)
        {
            this.__expr = expr;
            this.__modulus = modulus;
            this.__modRed = modRed;
            this.__idx = 0;
            this.__error = "";
        }

        public Node parse_expression()
        {
            if (this.__has_space())
            {
                this.__skip_space();
            }
            var root = this.__parse_inclusive_disjunction();
            while ((this.__idx < this.__expr.Count() && this.__has_space()))
            {
                this.__skip_space();
            }
            if (this.__idx != this.__expr.Count())
            {
                this.__error = "Finished near to " + this.__peek() + " before everything was parsed";
                return null;
            }
            return root;
        }

        public Node __new_node(NodeType t)
        {
            return new Node(t, this.__modulus, this.__modRed);
        }

        public Node __parse_inclusive_disjunction()
        {
            var child = this.__parse_exclusive_disjunction();
            if (child == null)
            {
                return null;
            }
            if (this.__peek() != "|")
            {
                return child;
            }
            var node = this.__new_node(NodeType.INCL_DISJUNCTION);
            node.children.Add(child);
            while (this.__peek() == "|")
            {
                this.__get();
                child = this.__parse_exclusive_disjunction();
                if (child == null)
                {
                    return null;
                }
                node.children.Add(child);
            }
            return node;
        }

        public Node __parse_exclusive_disjunction()
        {
            var child = this.__parse_conjunction();
            if (child == null)
            {
                return null;
            }
            if (this.__peek() != "^")
            {
                return child;
            }
            var node = this.__new_node(NodeType.EXCL_DISJUNCTION);
            node.children.Add(child);
            while (this.__peek() == "^")
            {
                this.__get();
                child = this.__parse_conjunction();
                if (child == null)
                {
                    return null;
                }
                node.children.Add(child);
            }
            return node;
        }

        public Node __parse_conjunction()
        {
            var child = this.__parse_shift();
            if (child == null)
            {
                return null;
            }
            if (this.__peek() != "&")
            {
                return child;
            }
            var node = this.__new_node(NodeType.CONJUNCTION);
            node.children.Add(child);
            while (this.__peek() == "&")
            {
                this.__get();
                child = this.__parse_shift();
                if (child == null)
                {
                    return null;
                }
                node.children.Add(child);
            }
            return node;
        }

        public Node __parse_shift()
        {
            var _base = this.__parse_sum();
            if (_base == null)
            {
                return null;
            }
            if (!(this.__has_lshift()))
            {
                return _base;
            }
            var prod = this.__new_node(NodeType.PRODUCT);
            prod.children.Add(_base);
            this.__get();
            this.__get();
            var op = this.__parse_sum();
            if (op == null)
            {
                return null;
            }
            var power = this.__new_node(NodeType.POWER);
            var two = this.__new_node(NodeType.CONSTANT);
            two.constant = 2;
            power.children.Add(two);
            power.children.Add(op);
            prod.children.Add(power);
            if (this.__has_lshift())
            {
                this.__error = "Disallowed nested lshift operator near " + this.__peek();
                return null;
            }
            return prod;
        }

        public Node __parse_sum()
        {
            var child = this.__parse_product();
            if (child == null)
            {
                return null;
            }
            if ((this.__peek() != "+" && this.__peek() != "-"))
            {
                return child;
            }
            var node = this.__new_node(NodeType.SUM);
            node.children.Add(child);
            while ((this.__peek() == "+" || this.__peek() == "-"))
            {
                var negative = this.__peek() == "-";
                this.__get();
                child = this.__parse_product();
                if (child == null)
                {
                    return null;
                }
                if (negative)
                {
                    node.children.Add(this.__multiply_by_minus_one(child));
                }
                else
                {
                    node.children.Add(child);
                }
            }
            return node;
        }

        public Node __parse_product()
        {
            var child = this.__parse_factor();
            if (child == null)
            {
                return null;
            }
            if (!(this.__has_multiplicator()))
            {
                return child;
            }
            var node = this.__new_node(NodeType.PRODUCT);
            node.children.Add(child);
            while (this.__has_multiplicator())
            {
                this.__get();
                child = this.__parse_factor();
                if (child == null)
                {
                    return null;
                }
                node.children.Add(child);
            }
            return node;
        }

        public Node __parse_factor()
        {
            if (this.__has_bitwise_negated_expression())
            {
                return this.__parse_bitwise_negated_expression();
            }
            if (this.__has_negative_expression())
            {
                return this.__parse_negative_expression();
            }
            return this.__parse_power();
        }

        public Node __parse_bitwise_negated_expression()
        {
            this.__get();
            var node = this.__new_node(NodeType.NEGATION);
            var child = this.__parse_factor();
            if (child == null)
            {
                return null;
            }
            node.children = new List<Node>() { child };
            return node;
        }

        public Node __parse_negative_expression()
        {
            this.__get();
            var node = this.__parse_factor();
            if (node == null)
            {
                return null;
            }
            return this.__multiply_by_minus_one(node);
        }

        public Node __multiply_by_minus_one(Node node)
        {
            if (node.type == NodeType.CONSTANT)
            {
                node.constant = -(node.constant);
                return node;
            }
            if (node.type == NodeType.PRODUCT)
            {
                if (node.children[0].type == NodeType.CONSTANT)
                {
                    node.children[0].constant *= -(1);
                    return node;
                }
                this.__new_node(NodeType.CONSTANT).constant = -(1);
                node.children.Insert(0, (Node?)this.__new_node(NodeType.CONSTANT));
                return node;
            }
            var minusOne = this.__new_node(NodeType.CONSTANT);
            minusOne.constant = -(1);
            var prod = this.__new_node(NodeType.PRODUCT);
            prod.children = new List<Node>() { minusOne, node };
            return prod;
        }

        public Node __parse_power()
        {
            var _base = this.__parse_terminal();
            if (_base == null)
            {
                return null;
            }
            if (!(this.__has_power()))
            {
                return _base;
            }
            var node = this.__new_node(NodeType.POWER);
            node.children.Add(_base);
            this.__get();
            this.__get();
            var exp = this.__parse_terminal();
            if (exp == null)
            {
                return null;
            }
            node.children.Add(exp);
            if (this.__has_power())
            {
                this.__error = "Disallowed nested power operator near " + this.__peek();
                return null;
            }
            return node;
        }

        public Node __parse_terminal()
        {
            if (this.__peek() == "(")
            {
                this.__get();
                var node = this.__parse_inclusive_disjunction();
                if (node == null)
                {
                    return null;
                }
                if (!(this.__peek() == ")"))
                {
                    this.__error = "Missing closing parentheses near to " + this.__peek();
                    return null;
                }
                this.__get();
                return node;
            }
            if (this.__has_variable())
            {
                return this.__parse_variable();
            }
            return this.__parse_constant();
        }

        public Node __parse_variable()
        {
            var start = this.__idx;
            this.__get(false);
            while ((this.__has_decimal_digit() || this.__has_letter() || this.__peek() == "_"))
            {
                this.__get(false);
            }
            if (this.__peek() == "[")
            {
                this.__get(false);
                while (this.__has_decimal_digit())
                {
                    this.__get(false);
                }
                if (this.__peek() == "]")
                {
                    this.__get();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                while (this.__has_space())
                {
                    this.__skip_space();
                }
            }
            var node = this.__new_node(NodeType.VARIABLE);
            node.vname = this.__expr.Slice(start, this.__idx, null).rstrip();
            return node;
        }

        public Node __parse_constant()
        {
            if (this.__has_binary_constant())
            {
                return this.__parse_binary_constant();
            }
            if (this.__has_hex_constant())
            {
                return this.__parse_hex_constant();
            }
            return this.__parse_decimal_constant();
        }

        public Node __parse_binary_constant()
        {
            this.__get(false);
            this.__get(false);
            if (!((new List<object>() { "0", "1" }).Contains(this.__peek())))
            {
                this.__error = "Invalid binary digit near to " + this.__peek();
                return null;
            }
            var start = this.__idx;
            while (((new List<object>() { "0", "1" }).Contains(this.__peek())))
            {
                this.__get(false);
            }
            while (this.__has_space())
            {
                this.__skip_space();
            }
            var node = this.__new_node(NodeType.CONSTANT);
            node.constant = this.__get_constant(start, 2);
            return node;
        }

        public Node __parse_hex_constant()
        {
            this.__get(false);
            this.__get(false);
            if (!(this.__has_hex_digit()))
            {
                this.__error = "Invalid hex digit near to " + this.__peek();
                return null;
            }
            var start = this.__idx;
            while (this.__has_hex_digit())
            {
                this.__get(false);
            }
            while (this.__has_space())
            {
                this.__skip_space();
            }
            var node = this.__new_node(NodeType.CONSTANT);
            node.constant = this.__get_constant(start, 16);
            return node;
        }

        public Node __parse_decimal_constant()
        {
            if (!(this.__has_decimal_digit()))
            {
                this.__error = "Expecting constant at " + this.__peek() + ", but no digit around.";
                return null;
            }
            var start = this.__idx;
            while (this.__has_decimal_digit())
            {
                this.__get(false);
            }
            while (this.__has_space())
            {
                this.__skip_space();
            }
            var node = this.__new_node(NodeType.CONSTANT);
            node.constant = this.__get_constant(start, 10);
            return node;
        }

        public long __get_constant(long start, int _base)
        {
            return Convert.ToInt32(this.__expr.Slice(start, this.__idx, null).rstrip(), _base);
        }

        public string __get(bool skipSpace = true)
        {
            var c = this.__peek();
            this.__idx += 1;
            if (skipSpace)
            {
                while (this.__has_space())
                {
                    this.__skip_space();
                }
            }
            return c;
        }

        public void __skip_space()
        {
            this.__idx += 1;
        }

        public string __peek()
        {
            if (this.__idx >= this.__expr.Count())
            {
                return "";
            }
            return this.__expr[this.__idx].ToString();
        }

        public bool __has_bitwise_negated_expression()
        {
            return this.__peek() == "~";
        }

        public bool __has_negative_expression()
        {
            return this.__peek() == "-";
        }

        public bool __has_multiplicator()
        {
            return (this.__peek() == "*" && this.__peek_next() != "*");
        }

        public bool __has_power()
        {
            return (this.__peek() == "*" && this.__peek_next() == "*");
        }

        public bool __has_lshift()
        {
            return (this.__peek() == "<" && this.__peek_next() == "<");
        }

        public bool __has_binary_constant()
        {
            return (this.__peek() == "0" && this.__peek_next() == "b");
        }

        public bool __has_hex_constant()
        {
            return (this.__peek() == "0" && this.__peek_next() == "x");
        }

        public bool __has_hex_digit()
        {
            return (this.__has_decimal_digit() || reutil.match("[a-fA-F]", this.__peek()));
        }

        public bool __has_decimal_digit()
        {
            return reutil.match("[0-9]", this.__peek());
        }

        public bool __has_variable()
        {
            return this.__has_letter();
        }

        public bool __has_letter()
        {
            return reutil.match("[a-zA-Z]", this.__peek());
        }

        public bool __has_space()
        {
            return this.__peek() == " ";
        }

        public string __peek_next()
        {
            if (this.__idx >= this.__expr.Count() - 1)
            {
                return "";
            }
            return this.__expr[this.__idx + 1].ToString();
        }
    }
}
