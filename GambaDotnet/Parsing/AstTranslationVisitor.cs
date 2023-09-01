using Antlr4.Runtime.Misc;
using Gamba.Prototyping.Transpiled;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static Gamba.Prototyping.Transpiled.Node;

namespace GambaDotnet.Parsing
{
    public class AstTranslationVisitor : ExprBaseVisitor<Node>
    {
        private readonly long modulus;

        private readonly bool modRed;

        public AstTranslationVisitor(long modulus, bool modRed)
        {
            this.modulus = modulus;
            this.modRed = modRed;
        }

        public override Node VisitGamba([NotNull] ExprParser.GambaContext context)
        {
            return Visit(context.expression());
        }

        public override Node VisitExpression([NotNull] ExprParser.ExpressionContext context)
        {
            var result = base.VisitExpression(context);
            return result;
        }

        public override Node VisitPowExpression([NotNull] ExprParser.PowExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override Node VisitMulExpression([NotNull] ExprParser.MulExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override Node VisitAddOrSubExpression([NotNull] ExprParser.AddOrSubExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override Node VisitShiftExpression([NotNull] ExprParser.ShiftExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override Node VisitAndExpression([NotNull] ExprParser.AndExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override Node VisitXorExpression([NotNull] ExprParser.XorExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override Node VisitOrExpression([NotNull] ExprParser.OrExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        private Node Binary(ExprParser.ExpressionContext exp1, ExprParser.ExpressionContext exp2, string binaryOperator)
        {
            var op1 = Visit(exp1);
            var op2 = Visit(exp2);

            Node node = binaryOperator switch
            {
                "**" => CreateNode(NodeType.POWER, op1, op2),
                "*" => CreateNode(NodeType.PRODUCT, op1, op2),
                "<<" => CreateNode(NodeType.PRODUCT, op1, CreateNode(NodeType.POWER, CreateConstNode(2), op2)),
                "+" => CreateNode(NodeType.SUM, op1, op2),
                "-" => CreateNode(NodeType.SUM, op1, CreateNode(NodeType.PRODUCT, op2, CreateConstNode(-1))),
                "&" => CreateNode(NodeType.CONJUNCTION, op1, op2),
                "|" => CreateNode(NodeType.INCL_DISJUNCTION, op1, op2),
                "^" => CreateNode(NodeType.EXCL_DISJUNCTION, op1, op2),
                _ => throw new InvalidOperationException($"Unrecognized binary operator: {binaryOperator}")
            };

            return node;
        }

        public override Node VisitParenthesizedExpression([NotNull] ExprParser.ParenthesizedExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override Node VisitNegativeOrNegationExpression([NotNull] ExprParser.NegativeOrNegationExpressionContext context)
        {
            var op1 = Visit(context.expression());
            var unaryOperator = context.children[0].GetText();

            Node node = unaryOperator switch
            {
                "~" => CreateNode(NodeType.NEGATION, op1),
                // Write "-x" as "x * -1".
                "-" => op1.type == NodeType.CONSTANT ? CreateConstNode(long.Parse($"-{op1.constant}")) : CreateNode(NodeType.PRODUCT, op1, CreateConstNode(-1)),
                _ => throw new InvalidOperationException($"Unrecognized unary operator: {unaryOperator}")
            };

            return node;
        }

        public override Node VisitNumberExpression([NotNull] ExprParser.NumberExpressionContext context)
        {
            // TODO: Handle binary literals.
            var text = context.NUMBER().GetText();
            var value = long.Parse(text.Replace("0x", ""), text.Contains("0x") ? NumberStyles.HexNumber : NumberStyles.Number);
            return CreateConstNode(value);
        }

        public override Node VisitIdExpression([NotNull] ExprParser.IdExpressionContext context)
        {
            var text = context.ID().GetText();
            return CreateVar(text);
        }

        private Node CreateConstNode(long constant)
        {
            var node = new Node(NodeType.CONSTANT, modulus, modRed);
            node.constant = constant;
            return node;
        }

        private Node CreateConstNode(ulong constant)
        {
            var node = new Node(NodeType.CONSTANT, modulus, modRed);
            node.constant = (long)constant;
            return node;
        }

        private Node CreateVar(string name)
        {
            var node = new Node(NodeType.VARIABLE, modulus, modRed);
            node.vname = name;
            return node;
        }

        private Node CreateNode(NodeType type, params Node[] children)
        {
            var node = new Node(type, modulus, modRed);
            foreach( var child in children )
            {
                node.children.Add(child);
            }

            return node;
        }
    }
}
