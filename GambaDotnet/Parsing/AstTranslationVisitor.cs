using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Gamba.Ast;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Parsing
{
    public class AstTranslationVisitor : ExprBaseVisitor<AstNode>
    {
        private readonly uint bitSize;

        public AstTranslationVisitor(uint bitSize)
        {
            this.bitSize = bitSize;
        }

        public override AstNode VisitGamba([NotNull] ExprParser.GambaContext context)
        {
            return Visit(context.expression());
        }

        public override AstNode VisitExpression([NotNull] ExprParser.ExpressionContext context)
        {
            var result = base.VisitExpression(context);
            return result;
        }

        public override AstNode VisitPowExpression([NotNull] ExprParser.PowExpressionContext context) 
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override AstNode VisitMulExpression([NotNull] ExprParser.MulExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override AstNode VisitAddOrSubExpression([NotNull] ExprParser.AddOrSubExpressionContext context) 
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override AstNode VisitShiftExpression([NotNull] ExprParser.ShiftExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override AstNode VisitAndExpression([NotNull] ExprParser.AndExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override AstNode VisitXorExpression([NotNull] ExprParser.XorExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        public override AstNode VisitOrExpression([NotNull] ExprParser.OrExpressionContext context)
            => Binary(context.expression()[0], context.expression()[1], context.children[1].GetText());

        private AstNode Binary(ExprParser.ExpressionContext exp1, ExprParser.ExpressionContext exp2, string text)
        {
            var op1 = Visit(exp1);
            var op2 = Visit(exp2);
            var binaryOperator = text;

            AstNode node = binaryOperator switch
            {
                "**" => new PowerNode(op1, op2),
                "*" => new MulNode(op1, op2),
                // Write "a << b" as "a * 2**b".
                "<<" => new MulNode(op1, new PowerNode(new ConstNode(2, bitSize), op2)),
                "+" => new AddNode(op1, op2),
                "-" => new AddNode(op1, new MulNode(op2, new ConstNode(-1, bitSize))),
                "&" => new AndNode(op1, op2),
                "|" => new OrNode(op1, op2),
                "^" => new XorNode(op1, op2),
                _ => throw new InvalidOperationException($"Unrecognized binary operator: {binaryOperator}")
            };

            return node;
        }

        public override AstNode VisitParenthesizedExpression([NotNull] ExprParser.ParenthesizedExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override AstNode VisitNegativeOrNegationExpression([NotNull] ExprParser.NegativeOrNegationExpressionContext context)
        {
            var op1 = Visit(context.expression());
            var unaryOperator = context.children[0].GetText();

            AstNode node = unaryOperator switch
            {
                "~" => new NegNode(op1),
                // Write "-x" as "x * -1".
                "-" => new MulNode(op1, new ConstNode(-1, bitSize)),
                _ => throw new InvalidOperationException($"Unrecognized unary operator: {unaryOperator}")
            };

            return node;
        }

        public override AstNode VisitNumberExpression([NotNull] ExprParser.NumberExpressionContext context)
        {
            var text = context.NUMBER().GetText();
            var value = ulong.Parse(text.Replace("0x", ""), text.Contains("0x") ? NumberStyles.HexNumber : NumberStyles.Number);
            return new ConstNode(value, bitSize);
        }

        public override AstNode VisitIdExpression([NotNull] ExprParser.IdExpressionContext context)
        {
            var text = context.ID().GetText();
            return new VarNode(text, bitSize);
        }
    }
}
