using Microsoft.Win32.SafeHandles;

namespace get_felica_idm
{
    public class SafeSCardContext : SafeHandleZeroOrMinusOneIsInvalid
    {
        public string InvalidReason;

        internal SafeSCardContext(IntPtr handle, string invalidReason = null) : base(true)
        {
            SetHandle(handle);
            InvalidReason = invalidReason;
        }

        protected override bool ReleaseHandle()
        {
            uint ret = NativeMethods.SCardReleaseContext(handle);
            if (ret != NativeMethods.SCARD_S_SUCCESS)
            {
                return false;
            }

            return true;
        }
    }
}
