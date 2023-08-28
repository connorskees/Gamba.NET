using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GambaDotnet
{
    public static class Assert
    {
        public static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("Assertio failure.");
        }
    }
}
