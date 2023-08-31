using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiled
{
    public static class ListUtil
    {
        public static List<T> Reversed<T>(IEnumerable<T> input) => input.ToList().AsEnumerable().Reverse().ToList();
    }
}
