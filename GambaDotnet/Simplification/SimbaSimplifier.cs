using Gamba.Ast;
using Gamba.Interop;
using Gamba.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Gamba.Simplification
{
    public static class SimbaSimplifier
    {
        private static HttpClient client = new();

        public static AstNode SimplifyLinearExpression(AstNode node)
        {
            var simplified = FastSimba.SimplifyLinearMba(node);
            return simplified;
        }

        private static string InvokeSimba(string expr, uint bitSize, bool checkLinear)
        {
            // Construct the htttp destinations.
            var url = $"http://127.0.0.1:5000/gamba/?expression={HttpUtility.UrlEncode(expr)}&bit_size={bitSize}&check_linear={(checkLinear ? 1 : 0)}";

            var uri = new Uri(url);

            var task = client.PostAsync(uri, null);
    
            var result = task.GetAwaiter().GetResult();

            var text = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            return text;
        }
    }
}
