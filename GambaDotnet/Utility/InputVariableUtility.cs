using Gamba.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Utility
{
    public class InputVariableUtility
    {
        public static void CollectInputVariables(AstNode x, HashSet<VarNode> variables)
        {
            if (x is VarNode varNode)
            {
                variables.Add(varNode);
                return;
            }

            foreach(var child in x.Children)
                CollectInputVariables(child, variables);
        }
    }
}
