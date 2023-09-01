using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GambaDotnet.Parsing
{
    public static class AstParser
    {
        public static Node Parse(string exprText, uint bitCount, bool modRed = false)
            => Parse(exprText, (long)Math.Pow(2, bitCount), modRed);

        public static Node Parse(string exprText, long modulus, bool modRed = false)
        {
            // Parse the expression AST.
            var charStream = new AntlrInputStream(exprText);
            var lexer = new ExprLexer(charStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new ExprParser(tokenStream);
            parser.BuildParseTree = true;
            var expr = parser.gamba();

            // Throw if ANTLR has any errors.
            var errCount = parser.NumberOfSyntaxErrors;
            if (errCount > 0)
                throw new InvalidOperationException($"Parsing ast failed. Encountered {errCount} errors.");

            // Process the parse tree into a usable AST node.
            var visitor = new AstTranslationVisitor(modulus, modRed);
            var result = visitor.Visit(expr);
            return result;
        }
    }
}
