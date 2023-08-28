using GambaDotnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiler
{
    public record AstItem(int Indentation);
    public record ItemWithChildren(List<AstItem> Children, int Indentation) : AstItem(Indentation);
    public record FileDefinition(List<AstItem> Children, int InitialIndentation) : ItemWithChildren(Children, InitialIndentation);
    public record ClassDefinition(string Definition, List<AstItem> Children, int Indentation) : ItemWithChildren(Children, Indentation);
    public record Comment(string Text, int Indentation) : AstItem(Indentation);
    public record AssertStatement(string Condition, int Indentation) : AstItem(Indentation);
    public record MethodDefinition(string Definition, List<AstItem> Children, int Indentation) : ItemWithChildren(Children, Indentation);
    public record IfStatement(AstItem Condition, List<AstItem> Children, int Indentation) : ItemWithChildren(Children, Indentation);
    public record ElseIfStatement(AstItem Condition, List<AstItem> Children, int Indentation) : ItemWithChildren(Children, Indentation);
    public record ElseStatement(List<AstItem> Children, int Indentation) : ItemWithChildren(Children, Indentation);
    public record WhileStatement(AstItem Condition, List<AstItem> Children, int Indentation) : ItemWithChildren(Children, Indentation);
    public record ForStatement(AstItem Variable, AstItem Collection, List<AstItem> Children, int Indentation) : ItemWithChildren(Children, Indentation);

    public enum LineKind
    {
        Child,
        NotChild,
    }

    public class PythonParser
    {
        private readonly List<string> lines;

        private int index = 0;

        Queue<ItemWithChildren> astTree = new();

        public PythonParser(List<string> lines)
        {
            this.lines = lines;
            astTree.Enqueue(new FileDefinition(new List<AstItem>(), 0));
        }

        public void Parse()
        {
            while(true)
            {
                var result = PeekNoIndentation();
                index++;
            }
        }

        private (LineKind, string) PeekUntilNonEmptyChildOrExit()
        {
            while(true)
            {
                // If the line is emp
                var line = lines[index];
                if (line == String.Empty)
                    continue;

                // If the line
                var root = astTree.Peek();
                var indentationCount = line.TakeWhile(x => x == ' ').Count();
                if (indentationCount == root.Indentation + 4)
                    return (LineKind.Child, line);

                if (indentationCount > root.Indentation + 4)
                    throw new InvalidOperationException($"Encountered unsupported indentation.");

                return (LineKind.NotChild, line);
            }
        }

        /*
        private string PeekNoIndentation()
        {
            // If the line is empty then return it.
            var line = lines[index];
            if (line == String.Empty)
                return line;

            // If the line is not empty then first we need to make sure that the indentation level is what we expect it to be.
            // If the current "parent" item is a file definition, then the indentation level should be 0).
            var root = astTree.Peek();
            var indentationCount = line.TakeWhile(x => x == ' ').Count();
            if (indentationCount == root.Indentation)
                return line;

            return line;
        }
        */
    }
}
