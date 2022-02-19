using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace get_felica_idm
{
    public class Library
    {
        public static SafeLibraryHandle LoadLibrary(string fileName)
        {
            return new SafeLibraryHandle(NativeMethods.LoadLibrary(fileName));
        }

        public static IntPtr GetProcAddress(SafeLibraryHandle libraryHandle, string procName)
        {
            if (libraryHandle.IsInvalid == true)
            {
                return IntPtr.Zero;
            }
            return NativeMethods.GetProcAddress(libraryHandle.DangerousGetHandle(), procName);
        }

        public static IntPtr GetProcAddress(string fileName, string procName)
        {
            using (SafeLibraryHandle libraryHandle = LoadLibrary(fileName))
            {
                return GetProcAddress(libraryHandle, procName);
            }
        }
    }
}
