using Gamba.Ast;
using Gamba.Parsing;
using Gamba.Utility;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Interop
{
    public static class FastSimba
    {
        public unsafe static bool CheckLinear(string expr)
        {
            return CheckLinear(new MarshaledString(StringUtility.RemoveWhitespace(expr)));
        }

        public unsafe static bool CheckLinear(AstNode node)
        {
            return CheckLinear(new MarshaledString(node.ToString()));
        }

        public unsafe static AstNode SimplifyLinearMba(AstNode node, bool checkLinear = true, bool fastCheck = true, bool runParallel = false, bool useZ3 = false)
        {
            // Convert the ast to a string.
            var expr = node.ToString();

            // Try to simplify the linear MBA.
            bool success = SimplifyLinearMba(new MarshaledString(expr), out nint simplifiedExprPtr, node.BitSize, useZ3, checkLinear, fastCheck, runParallel);

            // Throw if simplification failed.
            if (!success)
                throw new InvalidOperationException("Failed to simplify linear MBA.");

            // Convert the simplified string back into an AST.
            var simplifiedStr = StringMarshaler.AcquireString(simplifiedExprPtr);
            return AstParser.Parse(simplifiedStr, node.BitSize);
        }

        [DllImport("Gamba.Native")]
        public unsafe static extern bool CheckLinear(sbyte* exprStr);

        [DllImport("Gamba.Native")]
        public unsafe static extern bool SimplifyLinearMba(sbyte* exprStr, out nint simplifiedExprStr, uint bitCount, bool useZ3, bool checkLinear, bool fastCheck, bool runParallel);
    }

}
