namespace get_felica_idm
{
    public class FeliCa380 : IDisposable
    {
        private const string READER_NAME = "FeliCa Port/PaSoRi";

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

        public FeliCa380()
        {
            // NOTE: 現状、共有モードのみサポートしているが、書き込みを伴う拡張の際には占有モードも用意する必要がある。

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
                _cardHandle = SmartCard.Connect(_cardContext, readerName);

                if (_cardHandle.IsInvalid == true)
                {
                    InvalidReason = _cardHandle.InvalidReason;
                    return; // 通常の操作で起こりうるため、例外はスローしない。
                }
            }

            // NOTE: カード種別のチェックをしていないので、この時点でかざされているカードが FeliCa ではない可能性はある。
        }

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
