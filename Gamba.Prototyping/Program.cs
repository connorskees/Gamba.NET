
using Gamba.Prototyping.Transpiler;
using GambaDotnet.Parsing;
using System.Diagnostics;

var input = File.ReadAllLines("parse.py");

var parser = new PythonParser(input.ToList());

parser.Parse();

var text = "(4+8*(~y&x)+-5-8*(x&~y))*(x^y)";

var ast = AstParser.Parse(text, 64);

Console.WriteLine(ast);

Debugger.Break();