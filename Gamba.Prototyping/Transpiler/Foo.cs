using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiler
{
    public class Foo
    {
        public dynamic Execute(bool input)
        {
            return input ? input : "asdads";
        }
    }
}
