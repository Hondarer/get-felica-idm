using System.Runtime.InteropServices;

namespace get_felica_idm
{
    internal class NativeMethods
    {
        public const uint SCARD_S_SUCCESS = 0;
        public const uint SCARD_E_NO_SERVICE = 0x8010001D;
        public const uint SCARD_E_SHARING_VIOLATION = 0x8010000B;
        public const uint SCARD_W_REMOVED_CARD = 0x80100069;

        public const uint SCARD_SCOPE_USER = 0;

        public const int SCARD_SHARE_SHARED = 0x00000002;
        public const int SCARD_SHARE_DIRECT = 0x00000003;

        public const int SCARD_PROTOCOL_T1 = 2;

        public const int SCARD_LEAVE_CARD = 0;

        [DllImport("winscard.dll")]
        public static extern uint SCardEstablishContext(uint dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext);

        [DllImport("winscard.dll", EntryPoint = "SCardListReadersW", CharSet = CharSet.Unicode)]
        public static extern uint SCardListReaders(
          IntPtr hContext, byte[] mszGroups, byte[] mszReaders, ref uint pcchReaders);

        [DllImport("winscard.dll")]
        public static extern uint SCardReleaseContext(IntPtr phContext);

        [DllImport("winscard.dll", EntryPoint = "SCardConnectW", CharSet = CharSet.Unicode)]
        public static extern uint SCardConnect(IntPtr hContext, string szReader,
             uint dwShareMode, uint dwPreferredProtocols, ref IntPtr phCard,
             ref IntPtr pdwActiveProtocol);

        [DllImport("winscard.dll")]
        public static extern uint SCardDisconnect(IntPtr hCard, int Disposition);

        [StructLayout(LayoutKind.Sequential)]
        internal class SCARD_IO_REQUEST
        {
            internal uint dwProtocol;
            internal int cbPciLength = Marshal.SizeOf(typeof(SCARD_IO_REQUEST));
            public SCARD_IO_REQUEST()
            {
                dwProtocol = 0;
            }
        }

        [DllImport("winscard.dll")]
        public static extern uint SCardControl(IntPtr hCard, int controlCode, byte[] inBuffer, int inBufferLen, byte[] outBuffer, int outBufferLen, ref int bytesReturned);

        [DllImport("winscard.dll")]
        public static extern uint SCardTransmit(IntPtr hCard, IntPtr pioSendRequest, byte[] SendBuff, int SendBuffLen, SCARD_IO_REQUEST pioRecvRequest,
                byte[] RecvBuff, ref int RecvBuffLen);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr handle, string procName);
    }
}
