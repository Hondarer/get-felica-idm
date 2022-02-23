namespace get_felica_idm
{
    public class FeliCa380 : IDisposable
    {
        private const string READER_NAME = "FeliCa Port/PaSoRi";

        private const ushort SYSTEMCODE_ANY = 0xFFFF;
        private const ushort SYSTEMCODE_FELICALITE = 0x88B4;
        private const ushort SYSTEMCODE_NFC_TYPE3 = 0x12FC;

        private const int SCARD_CTL_CODE_3500 = 0x003136b0;

        /// <summary>
        /// PC / SC 2.02のAPDU用ラッパ
        /// </summary>
        private const byte ESC_CMD_APDU_WRAP = 0xFF;

        private const byte APDU_INS_DATA_EXCHANGE = 0xFE;

        private const byte APDU_P1_THRU = 0x00;

        private const byte APDU_P2_TIMEOUT_50MS = 0x05;

        private const byte EXCHANGE_POLLING_PACKET_SIZE = 0x05;

        private const byte EXCHANGE_POLLING = 0x00;
        private const byte POLLING_REQUEST_SYSTEM_CODE = 0x01;

        private const byte POLLING_TIMESLOT_16 = 0x0F;

        private const byte SW1_NORMAL = 0x90;
        private const byte SW2_NORMAL = 0x00;

        private SafeSCardContext _cardContext;

        private SafeSCardHandle _cardHandle;
        public string ConnectedReader { get; private set; }

        public bool IsReaderConnected
        {
            get => string.IsNullOrEmpty(ConnectedReader) == false;
        }

        public string InvalidReason { get; private set; }

        public bool IsInvalid
        {
            get => string.IsNullOrEmpty(InvalidReason) == false;
        }

        public byte[] IDm { get; private set; } = null;

        public byte[] PMm { get; private set; } = null;

        public ushort SystemCode { get; private set; }

        public FeliCa380() : this(false)
        {
        }

        public FeliCa380(bool connect = false)
        {
            _cardContext = SmartCard.EstablishContext();

            if (_cardContext.IsInvalid == true)
            {
                InvalidReason = _cardContext.InvalidReason;
                _cardHandle = SafeSCardHandle.Invalid;
                return;
            }
            else
            {
                string readerName = SmartCard.GetReaderFullName(_cardContext, READER_NAME);

                if (string.IsNullOrEmpty(readerName) == true)
                {
                    InvalidReason = $"{READER_NAME} が見つかりません。";
                    return; // 通常の操作で起こりうるため、例外はスローしない。
                }

                ConnectedReader = readerName;

                Polling(readerName, SYSTEMCODE_FELICALITE);

                // カードに接続する場合。IDmの取得だけであればポーリングで事足りる。

                if (connect == true && IDm != null)
                {
                    // NOTE: 現状、共有モードのみサポートしているが、拡張の際には占有モードも用意する必要がある。おそらく占有モードだけでよい。占有の場合はリトライも実装のこと。
                    _cardHandle = SmartCard.Connect(_cardContext, readerName);

                    // 接続失敗
                    if (_cardHandle.IsInvalid == true)
                    {
                        InvalidReason = _cardHandle.InvalidReason;
                        return; // 通常の操作で起こりうるため、例外はスローしない。
                    }

                    // ポーリングで補足されたIDmを持つカードではない
                    if (GetIDm().SequenceEqual(IDm) != true)
                    {
                        InvalidReason = "複数のカードがかざされています。";
                        return; // 通常の操作で起こりうるため、例外はスローしない。
                    }
                }
            }

            // NOTE: カード種別のチェックをしていないので、この時点でかざされているカードが FeliCa ではない可能性はある。
        }

        #region ポーリング(直接接続用)

        private void Polling(string readerName, ushort scancode = SYSTEMCODE_ANY)
        {
            using (SafeSCardHandle readerHandle = SmartCard.ConnectDirect(_cardContext, readerName))
            {
                byte scancode_high = (byte)((scancode >> 8) & 0xFF);
                byte scancode_low = (byte)(scancode & 0xFF);

                byte[] lpInBuffer = new byte[] { ESC_CMD_APDU_WRAP, APDU_INS_DATA_EXCHANGE, APDU_P1_THRU, APDU_P2_TIMEOUT_50MS, EXCHANGE_POLLING_PACKET_SIZE, EXCHANGE_POLLING, scancode_high, scancode_low, POLLING_REQUEST_SYSTEM_CODE, POLLING_TIMESLOT_16 };
                byte[] lpOutBuffer = new byte[256];
                int lpBytesReturned = 0;

                uint ret = NativeMethods.SCardControl(readerHandle.DangerousGetHandle(), SCARD_CTL_CODE_3500, lpInBuffer, lpInBuffer.Length, lpOutBuffer, lpOutBuffer.Length, ref lpBytesReturned);
                if (ret != NativeMethods.SCARD_S_SUCCESS)
                {
                    InvalidReason = $"カードからの応答が異常です。ret = {ret}";
                    return;
                }

                //レスポンス解析

                if (lpBytesReturned != 21)
                {
                    InvalidReason = $"カードからの応答長さが異常です。lpBytesReturned = {lpBytesReturned}";
                    return;
                }

                byte sw1 = lpOutBuffer[lpBytesReturned - 2];
                byte sw2 = lpOutBuffer[lpBytesReturned - 1];

                if (sw1 != 0x90 || sw2 != 0x00)
                {
                    if (sw1 == 0x63 && sw2 == 0x00)
                    {
                        InvalidReason = "カードが見つかりません。";
                        return;
                    }
                    InvalidReason = $"カードからの応答が異常です。SW1 = {sw1} SW2 = {sw2}";
                    return;
                }

                byte[] idm = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    idm[i] = lpOutBuffer[i + 1];
                }

                byte[] pmm = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    pmm[i] = lpOutBuffer[i + 9];
                }

                ushort systemcode = (ushort)(((lpOutBuffer[lpBytesReturned - 4] << 8) & 0xFF00) | (lpOutBuffer[lpBytesReturned - 3] & 0xFF));

                IDm = idm;
                PMm = pmm;
                SystemCode = systemcode;
            }
        }

        #endregion

        #region IDm取得

        /// <summary>
        /// IDm を取得するリクエストを保持します。
        /// </summary>
        private static readonly byte[] getIDmRequest = new byte[] { 0xff, 0xca, 0x00, 0x00, 0x00 };

        private const int IDM_LENGTH = 8;
        private const int GET_IDM_RECV_DATA_LENGTH = IDM_LENGTH + 2;
        private const int GET_IDM_INDEX_SW1 = IDM_LENGTH;
        private const int GET_IDM_INDEX_SW2 = IDM_LENGTH + 1;

        public byte[] GetIDm()
        {
            if (IsInvalid == true)
            {
                return null;
            }
            else
            {
                try
                {
                    byte[] response = SmartCard.Transmit(_cardHandle, getIDmRequest);

                    if (response.Length != GET_IDM_RECV_DATA_LENGTH)
                    {
                        // 長さエラー
                        InvalidReason = $"FeliCaからの応答長さが異常です。response.Length = {response.Length}";
                        return null; // 通常の操作で起こりうるため、例外はスローしない。
                    }

                    if (response[GET_IDM_INDEX_SW1] != SW1_NORMAL || response[GET_IDM_INDEX_SW2] != SW2_NORMAL)
                    {
                        // レスポンスエラー
                        InvalidReason = $"FeliCaからの応答が異常です。SW1 = {response[GET_IDM_INDEX_SW1]} SW2 = {response[GET_IDM_INDEX_SW2]}";
                        return null; // 通常の操作で起こりうるため、例外はスローしない。
                    }

                    return response.Take(IDM_LENGTH).ToArray();
                }
                catch (SmartCardException ex)
                {
                    // 送受信エラー
                    InvalidReason = ex.Message;
                    return null; // 通常の操作で起こりうるため、例外はスローしない。
                }
            }
        }

        #endregion

        #region IDisposable

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_cardHandle != null)
                    {
                        _cardHandle.Dispose();
                    }
                    if (_cardContext != null)
                    {
                        _cardContext.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
