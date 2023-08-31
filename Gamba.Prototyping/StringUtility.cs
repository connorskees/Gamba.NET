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

        public static string Test(string input)
        {
            if (input == "asd")
            {
                string foobar = input + "asdsa";
                return foobar;
            }

            else
            {

                string foobar = "asdsda";
                return foobar;
            }
        }
    }
}
