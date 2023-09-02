using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GambaDotnet.TestingUtils
{
    public static class MbaDatasetLoader
    {
        public static IReadOnlyList<MbaDataset> GetMbaDatasets()
        {
            var paths = new List<string>()
            {
                "loki_tiny.txt",
                "mba_obf_linear.txt",
                "mba_obf_nonlinear.txt",
                "neureduce.txt",
                "qsynth_ea.txt",
                "syntia.txt",
            };

            return paths.Select(x => MbaDataset.From(@"Datasets\" + x, 64)).ToList();
        }
    }
}
