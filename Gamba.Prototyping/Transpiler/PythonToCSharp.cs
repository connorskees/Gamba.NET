using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiler
{
    public class PythonToCSharp
    {
        private List<string> lines;

        public static string Transpile(string[] lines) => new PythonToCSharp(lines).Transpile();

        public PythonToCSharp(string[] lines)
        {
            this.lines = lines.ToList();
        }

        private string Transpile()
        {
            ReplacePythonSyntax();
            return null;
        }

        private void ReplacePythonSyntax()
        {
            // Convert this pointer access.
            Replace("self.", "this.");

            // Replace null accesses.
            Replace("None", "null");
        }

        private void Replace(string oldValue, string newValue)
        {
            lines = lines.Select(x => x.Replace(oldValue, newValue)).ToList();
        }

    }
}
