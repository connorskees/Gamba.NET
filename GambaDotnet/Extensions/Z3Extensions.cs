using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GambaDotnet.Extensions
{
    public static class Z3Extensions
    {
        public static long GetInt64(this BitVecNum expr)
        {
            var ul = (ulong)(expr.BigInteger & ulong.MaxValue);
            var result = unchecked((long)(ul));
            return result;
        }
    }
}
