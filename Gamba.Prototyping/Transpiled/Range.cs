using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiled
{
    public static class Range
    {
        public static List<int> Get(int count) => Enumerable.Range(0, count).ToList();

        public static List<int> Get(long count) => Enumerable.Range(0, (int)count).ToList();

        public static List<int> Get(int start, int stop) => Enumerable.Range(start, stop).ToList();

        public static List<int> Get(int start, long stop) => Enumerable.Range(start, (int)stop).ToList();

        public static List<int> Get(int start, int stop, int step)
        {
            var output = new List<int>();
            for(int i = start; i < stop; i += step)
            {
                output.Add(i);
            }

            return output;
        }
    }
}
