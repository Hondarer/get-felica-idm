namespace get_felica_idm
{
    public class FelicaInterface
    {
        private bool _readerReady;

        public bool ReaderReady
        {
            get => _readerReady;
            set
            {
                if (_readerReady != value)
                {
                    if (value == true)
                    {
                        Console.WriteLine("FeliCaリーダーに接続しました。");
                    }
                    else
                    {
                        Console.WriteLine("FeliCaリーダーが切断されました。");
                    }
                    _readerReady = value;
                }
            }
        }

        private string _idm;

        public string IDm
        {
            get => _idm;
            set
            {
                if (_idm != value)
                {
                    if (string.IsNullOrEmpty(value) == false)
                    {
                        Console.WriteLine($"FeliCaを検出しました。IDm: {value}");
                    }
                    else
                    {
                        Console.WriteLine("FeliCaが外されました。");
                    }
                    _idm = value;
                }
            }
        }

        #region FeliCaオブジェクト

        private class Felica : IDisposable
        {
            public bool ReaderReady { get; private set; }

            public bool IsInvalid { get; private set; }

            private SafeSCardContext _cardContext;

            public SafeSCardHandle CardHandle { get; }

            public Felica()
            {
                _cardContext = SmartCard.EstablishContext();

                if (_cardContext.IsInvalid == true)
                {
                    IsInvalid = true;
                    CardHandle = SafeSCardHandle.Invalid;
                    return;
                }
                else
                {
                    string readerName = SmartCard.GetReaderFullName(_cardContext, "FeliCa Port/PaSoRi");

                    if(string.IsNullOrEmpty(readerName)==true)
                    {
                        IsInvalid = true;
                        return;
                    }

                    ReaderReady = true;
                    CardHandle = SmartCard.Connect(_cardContext, readerName);

                    if (CardHandle.IsInvalid == true)
                    {
                        IsInvalid = true;
                        return;
                    }
                }
            }

            #region IDisposable

            private bool disposedValue;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        if (CardHandle != null)
                        {
                            CardHandle.Dispose();
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

        #endregion

        #region IDm取得

        /// <summary>
        /// IDm を取得するリクエストを保持します。
        /// </summary>
        private static readonly byte[] getIDmRequest = new byte[] { 0xff, 0xca, 0x00, 0x00, 0x00 };

        private const int IDM_LENGTH = 8;
        private const int RECV_DATA_LENGTH = IDM_LENGTH + 2;
        private const int INDEX_SW1 = IDM_LENGTH;
        private const int INDEX_SW2 = IDM_LENGTH+1;

        private const byte SW1_NORMAL = 0x90;
        private const byte SW2_NORMAL = 0x00;

        private string GetIDm()
        {
            using (Felica felica = new Felica())
            {
                ReaderReady = felica.ReaderReady;

                if (felica.IsInvalid == true)
                {
                    return null;
                }
                else
                {
                    byte[] response = SmartCard.Transmit(felica.CardHandle, getIDmRequest);

                    if (response == null)
                    {
                        // 送受信エラー
                        //throw new ApplicationException("FeliCaとの通信に失敗しました。");
                        return null;
                    }

                    if (response.Length != RECV_DATA_LENGTH)
                    {
                        // 長さエラー
                        //throw new ApplicationException("FeliCaからの応答長さが異常です。response.Length = " + response.Length);
                        return null;
                    }

                    if (response[INDEX_SW1] != SW1_NORMAL || response[INDEX_SW2] != SW2_NORMAL)
                    {
                        // レスポンスエラー
                        //throw new ApplicationException("FeliCaからの応答が異常です。SW1 = " + response[8] + " SW2 = " + response[9]);
                        return null;
                    }

                    return BitConverter.ToString(response, 0, IDM_LENGTH).Replace("-", string.Empty);
                }
            }
        }

        #endregion

        public void StartPolling()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    IDm = GetIDm();
                    Thread.Sleep(250);
                }
            });
        }
    }
}
