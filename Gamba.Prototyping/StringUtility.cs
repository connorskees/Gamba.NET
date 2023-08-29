using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping
{
    public static class StringUtility
    {
        public static string RemoveWhitespace(string input) => new string(input.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
    }
}
