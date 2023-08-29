
using Gamba.Prototyping.Transpiler;
using GambaDotnet.Parsing;
using System.Diagnostics;
using System.Text.RegularExpressions;

var input = File.ReadAllLines("parse.py");

//var parser = new PythonParser(input.ToList());



var parser = new Parser("5+10", 256, false);
var result = parser.parse_expression();
Console.WriteLine(result);

//var text = "(4+8*(~y&x)+-5-8*(x&~y))*(x^y)";

//var ast = AstParser.Parse(text, 64);


Debugger.Break();