using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace GambaDotnet
{
    public enum NodeType
    {
        CONSTANT = 0,
        VARIABLE = 1,
        POWER = 2,
        NEGATION = 3,
        PRODUCT = 4,
        SUM = 5,
        CONJUNCTION = 6,
        EXCL_DISJUNCTION = 7,
        INCL_DISJUNCTION = 8,
    }

    public enum NodeState
    {
        UNKNOWN = 0,
        BITWISE = 1,
        LINEAR = 2,
        NONLINEAR = 3,
        MIXED = 4
    }

    public class Node
    {
        public NodeType type;

        public ulong modulus;

        public bool modRed;

        public List<Node> children = new();

        public string vname = "";

        public long __vidx = -1;

        public ulong constant = 0;

        public NodeState state = NodeState.UNKNOWN;

        public ulong linearEnd = 0;

        public ulong __MAX_IT = 10;

        /// <summary>
        /// # Reduces the given number modulo given modulus.
        /// </summary>
        public static ulong mod_red(ulong n, ulong modulus, uint bitCount) => ArbitraryPrecisionInteger.Smod(n, modulus, bitCount);

        /// <summary>
        /// # Returns true iff the given lists of children match.
        /// </summary>
        public static bool do_children_match(List<Node> l1, List<Node> l2) => l1.Count == l2.Count && are_all_children_contained(l1, l2);

        /// <summary>
        /// Returns true iff all children contained in l1 are also contained in l2.
        /// </summary>
        public static bool are_all_children_contained(List<Node> l1, List<Node> l2) => l1.Except(l2).Any();

        public Node(NodeType type, ulong modulus, bool modRed)
        {
            this.type = type;
            this.modulus = modulus;
            this.modRed = modRed;
        }

        public Node(NodeType type, ulong modulus, bool modRed, params Node[] nodes)
        {
            this.type = type;
            this.modulus = modulus;
            this.modRed = modRed;
            children = nodes.ToList();
        }

        public override string ToString() => to_string();

        public string to_string(bool withParenthesis = false, int end = -1, Dictionary<long, string> varNames = null)
        {
            var parenthesize = (string s) => withParenthesis ? $"({s})" : s;

            if (end == -1)
                end = children.Count;
            else
                Assert.True(end <= children.Count);

            if (type == NodeType.CONSTANT)
                return ((long)constant).ToString();
            if (type == NodeType.VARIABLE)
                return varNames == null ? vname : varNames[__vidx];

            switch(type)
            {
                case NodeType.POWER:
                    {
                        Assert.True(children.Count == 2);
                        var child1 = children[0];
                        var child2 = children[1];
                        var retPower = child1.to_string(child1.type > NodeType.VARIABLE, -1, varNames) + "**" +
                            child2.to_string(child2.type > NodeType.VARIABLE, -1, varNames);
                        return parenthesize(retPower);
                    }
                case NodeType.NEGATION:
                    {
                        Assert.True(children.Count == 1);
                        var child = children[0];
                        var retNeg = "~" + child.to_string(child.type > NodeType.NEGATION, -1, varNames);
                        return parenthesize(retNeg);
                    }
                case NodeType.PRODUCT:
                    {
                        Assert.True(children.Count > 0);
                        var child1 = children[0];
                        var ret1 = child1.to_string(child1.type > NodeType.PRODUCT, -1, varNames);
                        var ret = ret1;
                        foreach (var child in children.Skip(1))
                            ret += "*" + child.to_string(child.type > NodeType.PRODUCT, -1, varNames);
                        // Rather than multiplying by -1, only use the minus and get rid
                        // of '1*'.
                        if (ret1 == "-1" && children.Count > 1 && end > 1)
                            ret = "-" + ret.Skip(3);

                        return parenthesize(ret);
                    }
                case NodeType.SUM:
                    {
                        Assert.True(children.Count > 0);
                        var child1 = children[0];
                        var ret = child1.to_string(child1.type > NodeType.SUM, -1, varNames);
                        foreach (var child in children.Skip(1))
                        {
                            var s = child.to_string(child.type > NodeType.SUM, -1, varNames);
                            if (s[0] != '-')
                                ret += "+";
                            ret += s;
                        }

                        return parenthesize(ret);
                    }
                case NodeType.CONJUNCTION:
                case NodeType.EXCL_DISJUNCTION:
                case NodeType.INCL_DISJUNCTION:
                    {
                        var op = "&";
                        if (type == NodeType.EXCL_DISJUNCTION)
                            op = "^";
                        else if(type == NodeType.INCL_DISJUNCTION)
                                op = "|";

                        Assert.True(children.Count > 0);
                        var child1 = children[0];
                        var ret = child1.to_string(child1.type > type, -1, varNames);
                        foreach (var child in children.Skip(1))
                            ret += op + child.to_string(child.type > type, -1, varNames);
                        return parenthesize(ret);
                    }
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
