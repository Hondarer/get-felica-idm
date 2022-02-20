using System.Text;

namespace get_felica_idm
{
    public class SmartCard
    {
        /// <summary>
        /// 受信バッファのサイズを表します。
        /// </summary>
        private const uint RECV_BUFFER_LENGTH = 262;

        private static readonly IntPtr SCARD_PCI_T1 = Library.GetProcAddress("winscard.dll", "g_rgSCardT1Pci");

        public static SafeSCardContext EstablishContext()
        {
            uint ret = NativeMethods.SCardEstablishContext(NativeMethods.SCARD_SCOPE_USER, IntPtr.Zero, IntPtr.Zero, out IntPtr hContext);
            if (ret != NativeMethods.SCARD_S_SUCCESS)
            {
                string message;
                switch (ret)
                {
                    case NativeMethods.SCARD_E_NO_SERVICE:
                        message = "サービスが起動されていません。";
                        break;
                    default:
                        message = $"サービスに接続できません。code = {ret}";
                        break;
                }

                return new SafeSCardContext(IntPtr.Zero, message);
            }

            return new SafeSCardContext(hContext);
        }

        public static string GetReaderFullName(SafeSCardContext cardContext, string readerNamePart)
        {
            uint pcchReaders = 0;

            // 全NFCリーダの文字列バッファのサイズを取得
            uint ret = NativeMethods.SCardListReaders(cardContext.DangerousGetHandle(), null, null, ref pcchReaders);
            if (ret != NativeMethods.SCARD_S_SUCCESS)
            {
                // 検出失敗
                return null;
            }

            // 全NFCリーダの文字列を取得
            byte[] mszReaders = new byte[pcchReaders * 2]; // 1文字2byte
            ret = NativeMethods.SCardListReaders(cardContext.DangerousGetHandle(), null, mszReaders, ref pcchReaders);
            if (ret != NativeMethods.SCARD_S_SUCCESS)
            {
                // 検出失敗
                return null;
            }

            // Felicaリーダーを特定(仮に複数台あったら最初の 1 台)
            string ReaderFullName = Encoding.Unicode.GetString(mszReaders).Split((char)0).Where(r => r.Contains(readerNamePart)).FirstOrDefault();
            if (string.IsNullOrEmpty(ReaderFullName))
            {
                // 検出失敗
                return null;
            }

            return ReaderFullName;
        }

        public static SafeSCardHandle Connect(SafeSCardContext cardContext, string readerNamePart)
        {
            IntPtr hCard = IntPtr.Zero;
            IntPtr activeProtocol = IntPtr.Zero;
            uint ret = NativeMethods.SCardConnect(cardContext.DangerousGetHandle(), GetReaderFullName(cardContext, readerNamePart), NativeMethods.SCARD_SHARE_SHARED, NativeMethods.SCARD_PROTOCOL_T1, ref hCard, ref activeProtocol);
            if (ret != NativeMethods.SCARD_S_SUCCESS)
            {
                return new SafeSCardHandle(IntPtr.Zero, $"FeliCaに接続できません。code = {ret}");
            }

            return new SafeSCardHandle(hCard);
        }

        public static byte[] Transmit(SafeSCardHandle cardHandle, byte[] requestData)
        {
            byte[] recvBuffer = new byte[RECV_BUFFER_LENGTH];
            int pcbRecvLength = (int)RECV_BUFFER_LENGTH;

            uint ret = NativeMethods.SCardTransmit(cardHandle.DangerousGetHandle(), SCARD_PCI_T1, requestData, requestData.Length, new NativeMethods.SCARD_IO_REQUEST(), recvBuffer, ref pcbRecvLength);
            if (ret != NativeMethods.SCARD_S_SUCCESS)
            {
                return null;
            }

            return recvBuffer.Take(pcbRecvLength).ToArray();
        }
    }
}
