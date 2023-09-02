using Gamba.Prototyping.Transpiled;
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
using static Gamba.Prototyping.Transpiled.Node;

namespace Gamba.Prototyping.Transpiler
{
    public class GambaParser
    {
        string __expr;

        long __modulus;

        bool __modRed;

        int __idx;

        string __error;

        public GambaParser(string expr, long modulus, bool modRed = false)
        {
            this.__expr = expr;
            this.__modulus = modulus;
            this.__modRed = modRed;
            this.__idx = 0;
            this.__error = "";
        }

        public static Node Parse(string expr, int bitCount = 64, bool modRed = false, long? modulus = 0)
        {
            // TODO: Use bit masks instead of modulus.
            var parser = new GambaParser(expr, 0, modRed);

            return parser.parse_expression();
        }

        public Node parse_expression()
        {
            __idx = 0;
            this.__error = "";
            if (this.__has_space())
            {
                this.__skip_space();
            }
            var root = this.__parse_inclusive_disjunction();
            while ((this.__idx < this.__expr.Length && this.__has_space()))
            {
                this.__skip_space();
            }
            if (this.__idx != this.__expr.Length)
            {
                this.__error = "Finished near to " + this.__peek() + " before everything was parsed";
                return null;
            }
            return root;
        }

        public Node __new_node(NodeType t)
        {
            int childCount = 0;
            switch (t)
            {
                case NodeType.CONSTANT:
                case NodeType.VARIABLE:
                    childCount = 0;
                    break;
                case NodeType.NEGATION:
                    childCount = 1;
                    break;
                case NodeType.POWER:
                case NodeType.PRODUCT:
                case NodeType.SUM:
                case NodeType.CONJUNCTION:
                case NodeType.EXCL_DISJUNCTION:
                case NodeType.INCL_DISJUNCTION:
                    childCount = 2;
                    break;
            }

            return new Node(t, this.__modulus, this.__modRed);
        }

        public Node __parse_inclusive_disjunction()
        {
            var child = this.__parse_exclusive_disjunction();
            if (child == null)
            {
                return null;
            }
            if (this.__peek() != '|')
            {
                return child;
            }
            var node = this.__new_node(NodeType.INCL_DISJUNCTION);
            node.children.Add(child);
            while (this.__peek() == '|')
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
            if (this.__peek() != '^')
            {
                return child;
            }
            var node = this.__new_node(NodeType.EXCL_DISJUNCTION);
            node.children.Add(child);
            while (this.__peek() == '^')
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
            if (this.__peek() != '&')
            {
                return child;
            }
            var node = this.__new_node(NodeType.CONJUNCTION);
            node.children.Add(child);
            while (this.__peek() == '&')
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
            if ((this.__peek() != '+' && this.__peek() != '-'))
            {
                return child;
            }
            var node = this.__new_node(NodeType.SUM);
            node.children.Add(child);
            while ((this.__peek() == '+' || this.__peek() == '-'))
            {
                var negative = this.__peek() == '-';
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
            var child = this.__parse_factor();
            if (child == null)
            {
                return null;
            }
            var node = new Node(NodeType.NEGATION, __modulus, __modRed);
            node.children.Add(child);
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
                node.constant = 0 - (node.constant);
                return node;
            }
            if (node.type == NodeType.PRODUCT)
            {
                if (node.children[0].type == NodeType.CONSTANT)
                {
                    node.children[0].constant *= long.MaxValue;
                    return node;
                }
                var minusOne = this.__new_node(NodeType.CONSTANT);
                minusOne.constant = long.MaxValue;
                node.children.Insert(0, minusOne);
                return node;
            }
            var minus1 = this.__new_node(NodeType.CONSTANT);
            minus1.constant = long.MaxValue;
            var prod = this.__new_node(NodeType.PRODUCT);
            prod.children.Add(minus1);
            prod.children.Add(node);
            //prod.children = new List<Node>(2) { minus1, node };
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
            if (this.__peek() == '(')
            {
                this.__get();
                var node = this.__parse_inclusive_disjunction();
                if (node == null)
                {
                    return null;
                }
                if (!(this.__peek() == ')'))
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
            while ((this.__has_decimal_digit() || this.__has_letter() || this.__peek() == '_'))
            {
                this.__get(false);
            }
            if (this.__peek() == '[')
            {
                this.__get(false);
                while (this.__has_decimal_digit())
                {
                    this.__get(false);
                }
                if (this.__peek() == ']')
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
            //node.vname = this.__expr.Slice(start, this.__idx, null).rstrip();
            //node.vname = new String(__expr.Skip(start).Take(this.__idx - start).ToArray());
            node.vname = __expr.Substring(start, Math.Abs(this.__idx - start));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsZeroOrOne(char s) => s == '0' || s == '1';

        public Node __parse_binary_constant()
        {
            this.__get(false);
            this.__get(false);
            if (!(IsZeroOrOne(this.__peek())))
            {
                this.__error = "Invalid binary digit near to " + this.__peek();
                return null;
            }
            var start = this.__idx;
            while ((IsZeroOrOne(this.__peek())))
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

        private bool IsDigit(char c)
        {
            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return true;
                default:
                    return false;
            }
        }

        public Node __parse_decimal_constant()
        {
            var character = this.__peek();

            if (!(IsDigit(character)))
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

        public long __get_constant(int start, int __base)
        {
            //var slice = new String(this.__expr.Skip(start).Take(Math.Abs(start - this.__idx)).ToArray());
            var slice = __expr.Substring(start, Math.Abs(start - this.__idx));
            return Convert.ToInt64(slice);
            //return Convert.ToInt32((this.__expr.Slice(start, this.__idx, null).rstrip(), __base));
        }

        public char __get(bool skipSpace = true)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __skip_space()
        {
            this.__idx += 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char __peek()
        {
            if (this.__idx >= this.__expr.Length)
            {
                return '@';
            }
            return this.__expr[this.__idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_bitwise_negated_expression()
        {
            return this.__peek() == '~';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_negative_expression()
        {
            return this.__peek() == '-';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_multiplicator()
        {
            return (this.__peek() == '*' && this.__peek_next() != '*');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_power()
        {
            return (this.__peek() == '*' && this.__peek_next() == '*');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_lshift()
        {
            return (this.__peek() == '<' && this.__peek_next() == '<');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_binary_constant()
        {
            return (this.__peek() == '0' && this.__peek_next() == 'b');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_hex_constant()
        {
            return (this.__peek() == '0' && this.__peek_next() == 'x');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_hex_digit()
        {
            var c = this.__peek();
            var isHex = ((c >= '0' && c <= '9') ||
                 (c >= 'a' && c <= 'f') ||
                 (c >= 'A' && c <= 'F'));

            return (Char.IsDigit(c) || isHex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_decimal_digit()
        {
            var character = this.__peek();
            return Char.IsDigit(character);
            //return reutil.match("[0-9]", this.__peek().ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_variable()
        {
            return this.__has_letter();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_letter()
        {
            var character = __peek();
            return Char.IsLetter(character);
            //return reutil.match("[a-zA-Z]", this.__peek().ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool __has_space()
        {
            return this.__peek() == ' ';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char __peek_next()
        {
            if (this.__idx >= this.__expr.Length - 1)
            {
                return '@';
            }
            return this.__expr[this.__idx + 1];
        }
    }
}
