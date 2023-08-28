using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GambaDotnet
{
    public static class ArbitraryPrecisionInteger
    {
        private static Context context = new();

        public static ulong Smod(ulong n, ulong modulus, uint bitCount)
        {
            var bvN = context.MkBV(n, bitCount);
            var bvMod = context.MkBV(modulus, bitCount);
            var value = (BitVecNum)context.MkBVSMod(bvN, bvMod).Simplify();
            return value.UInt64;
        }
    }
}
