using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiler
{
    public static class reutil
    {
        public static bool match(string a, string b) => Regex.IsMatch(b, a);
    }
}
