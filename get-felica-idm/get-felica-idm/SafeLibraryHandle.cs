using Microsoft.Win32.SafeHandles;

namespace get_felica_idm
{
    public class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeLibraryHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.FreeLibrary(handle);
            return true;
        }
    }
}
