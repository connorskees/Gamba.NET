﻿using Gamba.Prototyping.Transpiled;
using GambaDotnet.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GambaDotnet.TestingUtils
{
    /// <summary>
    /// Class representing an MBA expression within a given dataset.
    /// This is intended to store the parsed contents of GAMBA and SiMBA's MBA datasets.
    /// </summary>
    /// <param name="Expr">The string representation of the input expression.</param>
    /// <param name="ParsedExpr">The parsed AST of the input expression.</param>
    /// <param name="GroundTruth">The string representation of the ground truth expression.</param>
    /// <param name="ParsedGroundTruth">The parsed ground truth expression.</param>
    /// <param name="ParsedGroundTruth">The parsed ground truth expression.</param>
    public record MbaExpression(string StrExpr, Node ParsedExpr, string StrGroundTruth, Node ParsedGroundTruth);

    public class MbaDataset
    {
        public string Name { get; }

        public IReadOnlyList<MbaExpression> MbaExpressions { get; }

        public MbaDataset(string name, IReadOnlyList<MbaExpression> mbaExpressions)
        {
            Name = name;
            MbaExpressions = mbaExpressions;
        }

        public static MbaDataset From(string path, uint bitSize = 64, bool modRed = true)
        {
            List<MbaExpression> expressions = new();
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                // Skip comment lines.
                if (line.Contains('#'))
                    continue;

                // Throw if the format of the ground truth differs from what we've seen.
                // All of the datasets except for MBA_FLATTEN are in format (#complex,#groundtruth).
                // MBA_FLATTEN contains one section which uses the format (#complex,groundtruth,sub-expression).
                var split = line.Split(",", StringSplitOptions.RemoveEmptyEntries);
                if (split.Length > 3)
                    throw new InvalidOperationException($"Dataset expression {path} does not meet parsing criteria.");

                var mba = split[0];
                var groundTruth = split[1];
                var mbaExpr = new MbaExpression(mba, AstParser.Parse(mba, bitSize), groundTruth, AstParser.Parse(groundTruth, bitSize));
                expressions.Add(mbaExpr);
            }

            return new MbaDataset(Path.GetFileNameWithoutExtension(path), expressions);
        }
    }
}
