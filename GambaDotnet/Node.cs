﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace GambaDotnet
{
    public enum OldNodeType
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

    public enum OldNodeState
    {
        UNKNOWN = 0,
        BITWISE = 1,
        LINEAR = 2,
        NONLINEAR = 3,
        MIXED = 4
    }

    public class OldNode
    {
        public OldNodeType type;

        public long modulus;

        public bool modRed;

        public List<OldNode> children;

        public string vname = "";

        public long __vidx = -1;

        public long constant = 0;

        public OldNodeState state = OldNodeState.UNKNOWN;

        public ulong linearEnd = 0;

        public ulong __MAX_IT = 10;

        /// <summary>
        /// # Reduces the given number modulo given modulus.
        /// </summary>
        public static ulong mod_red(ulong n, ulong modulus, uint bitCount) => ArbitraryPrecisionInteger.Smod(n, modulus, bitCount);

        /// <summary>
        /// # Returns true iff the given lists of children match.
        /// </summary>
        public static bool do_children_match(List<OldNode> l1, List<OldNode> l2) => l1.Count == l2.Count && are_all_children_contained(l1, l2);

        /// <summary>
        /// Returns true iff all children contained in l1 are also contained in l2.
        /// </summary>
        public static bool are_all_children_contained(List<OldNode> l1, List<OldNode> l2) => l1.Except(l2).Any();

        public OldNode(OldNodeType type, long modulus, bool modRed, int childCount = 2)
        {
            this.type = type;
            this.modulus = modulus;
            this.modRed = modRed;
            this.children = new(childCount);
        }

        public OldNode(OldNodeType type, long modulus, bool modRed, params OldNode[] nodes)
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

            if (type == OldNodeType.CONSTANT)
                return ((long)constant).ToString();
            if (type == OldNodeType.VARIABLE)
                return varNames == null ? vname : varNames[__vidx];

            switch(type)
            {
                case OldNodeType.POWER:
                    {
                        Assert.True(children.Count == 2);
                        var child1 = children[0];
                        var child2 = children[1];
                        var retPower = child1.to_string(child1.type > OldNodeType.VARIABLE, -1, varNames) + "**" +
                            child2.to_string(child2.type > OldNodeType.VARIABLE, -1, varNames);
                        return parenthesize(retPower);
                    }
                case OldNodeType.NEGATION:
                    {
                        Assert.True(children.Count == 1);
                        var child = children[0];
                        var retNeg = "~" + child.to_string(child.type > OldNodeType.NEGATION, -1, varNames);
                        return parenthesize(retNeg);
                    }
                case OldNodeType.PRODUCT:
                    {
                        Assert.True(children.Count > 0);
                        var child1 = children[0];
                        var ret1 = child1.to_string(child1.type > OldNodeType.PRODUCT, -1, varNames);
                        var ret = ret1;
                        foreach (var child in children.Skip(1))
                            ret += "*" + child.to_string(child.type > OldNodeType.PRODUCT, -1, varNames);
                        // Rather than multiplying by -1, only use the minus and get rid
                        // of '1*'.
                        if (ret1 == "-1" && children.Count > 1 && end > 1)
                            ret = "-" + new String(ret.Skip(3).ToArray());

                        return parenthesize(ret);
                    }
                case OldNodeType.SUM:
                    {
                        Assert.True(children.Count > 0);
                        var child1 = children[0];
                        var ret = child1.to_string(child1.type > OldNodeType.SUM, -1, varNames);
                        foreach (var child in children.Skip(1))
                        {
                            var s = child.to_string(child.type > OldNodeType.SUM, -1, varNames);
                            if (s[0] != '-')
                                ret += "+";
                            ret += s;
                        }

                        return parenthesize(ret);
                    }
                case OldNodeType.CONJUNCTION:
                case OldNodeType.EXCL_DISJUNCTION:
                case OldNodeType.INCL_DISJUNCTION:
                    {
                        var op = "&";
                        if (type == OldNodeType.EXCL_DISJUNCTION)
                            op = "^";
                        else if(type == OldNodeType.INCL_DISJUNCTION)
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
