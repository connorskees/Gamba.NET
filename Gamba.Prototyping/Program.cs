
using GambaDotnet.Parsing;
using System.Diagnostics;

var text = "(4+8*(~y&x)+-5-8*(x&~y))*(x^y)";

var ast = AstParser.Parse(text, 64);

Console.WriteLine(ast);

Debugger.Break();