using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Interop
{
    public struct OpaqueManagedVector<T>
    {
    }

    public class ManagedVector<T>
    {
        public readonly nint Handle;

        private readonly Func<nint, T> convertPtr;

        public int Count => NativeManagedVectorApi.GetVecCount(Handle);

        public IReadOnlyList<T> Items => GetItems();

        public ManagedVector(nint handle, Func<nint, T> convertPtr)
        {
            this.Handle = handle;
            this.convertPtr = convertPtr;
        }

        private IReadOnlyList<T> GetItems()
        {
            List<T> output = new();
            for (int i = 0; i < Count; i++)
            {
                var element = NativeManagedVectorApi.GetVecElementAt(Handle, i);
                output.Add(convertPtr(element));
            }

            return output.AsReadOnly();
        }
    }

    public static class NativeManagedVectorApi
    {
        [DllImport("Gamba.Native", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern int GetVecCount(nint managedVector);

        [DllImport("Gamba.Native", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern nint GetVecElementAt(nint managedVector, int index);
    }
}
