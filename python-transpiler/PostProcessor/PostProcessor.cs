using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostProcessor
{
    public class PostProcessor
    {
        private List<string> lines;

        public PostProcessor(List<string> lines)
        {
            this.lines = lines;
        }
        
        private List<string> Run()
        {
            Replace("base", "temel");
            return lines;
        }

        private void Replace(string oldValue, string newValue)
        {
            lines = lines.Select(x => x.Replace(oldValue, newValue)).ToList();
        }
    }
}
