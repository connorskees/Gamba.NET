using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiled
{
    public class NullableI32
    {
        private readonly int? value;

        public NullableI32(int? value)
        {
            this.value = value;
        }

        public override bool Equals(object? obj) => value.Equals(obj);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static implicit operator int(NullableI32 i) => (int)i.value;

        public static implicit operator NullableI32(int i) => new NullableI32(i);
    }
}
