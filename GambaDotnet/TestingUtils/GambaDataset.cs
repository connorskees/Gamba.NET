using Gamba.Prototyping.Transpiled;
using Gamba.Prototyping.Transpiler;
using GambaDotnet.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GambaDotnet.TestingUtils
{
    /// <summary>
    /// Class representing a database entry for GAMBA results.
    /// </summary>
    /// <param name="inputExpression">The string representation of the input expression.</param>
    /// <param name="parsedExpression">The ast of the expression after parsing.</param>
    /// <param name="refinedExpression">The ast after being refined by GAMBA.</param>
    /// <param name="simplifiedExpression">The ast after optional simplification of GAMBA. If we don't have a simplification stored for this expression then it's null.</param>
    public record GambaExpression(string InputExpression, Node ParsedExpression, Node RefinedExpression, Node? SimplifiedExpression);

    public class GambaDataset
    {
        public string Name { get; }

        public IReadOnlyList<GambaExpression> GambaExpressions { get; }

        public GambaDataset(string name, IReadOnlyList<GambaExpression> mbaExpressions)
        {
            Name = name;
            GambaExpressions = mbaExpressions;
        }

        public static GambaDataset From(string path, uint bitSize = 64, bool modRed = true)
        {
            List<GambaExpression> expressions = new();
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                // Skip comment lines.
                if (line.Contains('#'))
                    continue;
                var split = line.Split(",", StringSplitOptions.RemoveEmptyEntries);

                // Parse the entry.
                var mba = split[0];
                var parsedExpression = GambaParser.Parse(mba, bitSize, false);
                var refinedExpression = GambaParser.Parse(split[1], bitSize, false);
                var simplifiedExpression = split.Length > 1 ? GambaParser.Parse(split[1], bitSize) : null;
                expressions.Add(new GambaExpression(mba, parsedExpression, refinedExpression, simplifiedExpression));
            }

            return new GambaDataset(Path.GetFileNameWithoutExtension(path), expressions);
        }

        public void To(string path) => File.WriteAllLines(path, Serialize().ToArray());

        public List<string> Serialize()
        {
            List<string> lines = new List<string>();
            foreach(var expr in GambaExpressions)
                lines.Add($"{expr.InputExpression},{expr.ParsedExpression},{expr.RefinedExpression},{expr.SimplifiedExpression}");
            return lines;
        }
    }
}
