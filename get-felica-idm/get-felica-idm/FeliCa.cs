namespace get_felica_idm
{
    public class FeliCa : IDisposable
    {
        public string ConnectedReader { get; private set; }

        public bool IsInvalid { get; private set; }

        private SafeSCardContext _cardContext;

        private SafeSCardHandle _cardHandle;

        public FeliCa()
        {
            _cardContext = SmartCard.EstablishContext();

            if (_cardContext.IsInvalid == true)
            {
                IsInvalid = true;
                _cardHandle = SafeSCardHandle.Invalid;
                return;
            }
            else
            {
                string readerName = SmartCard.GetReaderFullName(_cardContext, "FeliCa Port/PaSoRi");

                if (string.IsNullOrEmpty(readerName) == true)
                {
                    IsInvalid = true;
                    return;
                }

                ConnectedReader = readerName;
                _cardHandle = SmartCard.Connect(_cardContext, readerName);

                if (_cardHandle.IsInvalid == true)
                {
                    IsInvalid = true;
                    return;
                }
            }

            // NOTE: カード種別のチェックをしていないので、この時点でかざされたカードが FeliCa ではない可能性はある。
        }

        #region IDm取得

        /// <summary>
        /// IDm を取得するリクエストを保持します。
        /// </summary>
        private static readonly byte[] getIDmRequest = new byte[] { 0xff, 0xca, 0x00, 0x00, 0x00 };

        private const int IDM_LENGTH = 8;
        private const int RECV_DATA_LENGTH = IDM_LENGTH + 2;
        private const int INDEX_SW1 = IDM_LENGTH;
        private const int INDEX_SW2 = IDM_LENGTH + 1;

        private const byte SW1_NORMAL = 0x90;
        private const byte SW2_NORMAL = 0x00;

        public string GetIDm()
        {
            if (IsInvalid == true)
            {
                return null;
            }
            else
            {
                byte[] response = SmartCard.Transmit(_cardHandle, getIDmRequest);

                if (response == null)
                {
                    // 送受信エラー
                    //throw new ApplicationException("FeliCaとの通信に失敗しました。");
                    IsInvalid = true;
                    return null;
                }

                if (response.Length != RECV_DATA_LENGTH)
                {
                    // 長さエラー
                    //throw new ApplicationException("FeliCaからの応答長さが異常です。response.Length = " + response.Length);
                    IsInvalid = true;
                    return null;
                }

                if (response[INDEX_SW1] != SW1_NORMAL || response[INDEX_SW2] != SW2_NORMAL)
                {
                    // レスポンスエラー
                    //throw new ApplicationException("FeliCaからの応答が異常です。SW1 = " + response[8] + " SW2 = " + response[9]);
                    IsInvalid = true;
                    return null;
                }

                return BitConverter.ToString(response, 0, IDM_LENGTH).Replace("-", string.Empty);
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
