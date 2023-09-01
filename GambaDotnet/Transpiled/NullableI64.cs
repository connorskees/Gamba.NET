using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiled
{
    public class NullableI64
    {
        private readonly long? value;

        public NullableI64(long? value)
        {
            this.value = value;
        }

        public override bool Equals(object? obj) => value.Equals(obj);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static implicit operator long(NullableI64 i) => (long)i.value;

        public static implicit operator NullableI64(long i) => new NullableI64(i);
    }
}
