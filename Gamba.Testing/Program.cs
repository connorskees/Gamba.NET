using Gamba.Ast;
using Gamba.Interop;
using Gamba.LLVMInterop;
using Gamba.Parsing;
using Gamba.Simplification;
using Gamba.Symbolic;
using Gamba.Utility;
using LLVMSharp.Interop;
using Microsoft.Z3;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

var input = "18446744073709551574*(x|~y)*~x+14*(x|~y)**2+18446744073709551598*y*~x+6*y*(x|~y)+18446744073709551609*(x|~y)*(y+18446744073709551615*(x&y))+18446744073709551595*(x|~y)*~(x|y)+14*(x|~y)*(~x|y)+18446744073709551588*(x|~y)+18446744073709551567*(x|~y)*(x&~y)+18446744073709551553*(x|~y)*(x&y)+18446744073709551613*y*(y+18446744073709551615*(x&y))+18446744073709551607*y*~(x|y)+6*y*(~x|y)+18446744073709551604*y+18446744073709551589*y*(x&y)+18446744073709551595*y*(x&~y)";
input = "-4*(x^y)*(x&y)+7*(x^y)*(x&~y)+2*(x^y)*~(x|y)+8*(x^y)*~(x|~y)+1*(x^y)*~y-7*(x^y)*~(x&y)+5*(x^y)*~(x^y)";
input = "(x^y)*(18446744073709551615+3*(x&y)+7*(x&~y)+8*(~x&y)+18446744073709551611*(x^y)+18446744073709551615*y+18446744073709551614*(x|y))";
input = "-4*(x^y)*(x&y)+7*(x^y)*(x&~y)+2*(x^y)*~(x|y)+8*(x^y)*~(x|~y)+(x^y)*~y-7*(x^y)*~(x&y)+5*(x^y)*~(x^y)";
//input = "(x^y)*(-4*(x&y)+7*(x&~y)+2*~(x|y)+8*~(x|~y)+~y-7*~(x&y)+5*~(x^y))";
input = "100*((((x^y)*z)+((x^y)*q)))";
input = "((((x^y)*z)+((x^y)*q)))";
var ast = AstParser.Parse(input, 64);
Console.WriteLine(ast.ToString());

var classification = AstClassifier.Classify(ast);
foreach (var child in classification.Keys)
{
    bool printClassification = true;
    if (printClassification)
    {
        Console.WriteLine($"Subtree: {classification[child]} {child}");
    }

    else
    {
        var isLinear = FastSimba.CheckLinear(child);
        if (isLinear)
        {
            if (AstClassifier.IsLinear(classification[child]) != isLinear)
                throw new InvalidOperationException();
            Console.WriteLine($"Linear subtree: {child}");
        }

        else
        {
            Console.WriteLine($"Nonlinear subtree: {child}");
        }
    }
}

var factorizer = new PolynomialFactorizer();
factorizer.TryFactorizePolynomial(ast);

Console.WriteLine($"Ast before simplification: {ast}");

var simplifier = new AstSimplifier(ast);
var simplified = simplifier.Simplify();

Console.WriteLine($"Ast after simplification: {simplified}");

/*
var converter = new AstToZ3(64);
var toZ3 = converter.Translate(ast, null);

Console.WriteLine($"Before simplification: {toZ3}");

// Simplify.
for (int i = 0; i < 10; i++)
    toZ3 = toZ3.Simplify();

Console.WriteLine($"After simplification: {toZ3.Simplify()}");

foreach(var child in ast.Children)
{
    var isLinear = FastSimba.CheckLinear(child);
    if(isLinear)
    {
        Console.WriteLine($"Linear subtree: {child}");
    }
}


//LinearSubtreeSimplifier.SimplifyLinearSubtrees(ast,);

//Console.WriteLine($"Simplified: {ast}");
*/

Debugger.Break();