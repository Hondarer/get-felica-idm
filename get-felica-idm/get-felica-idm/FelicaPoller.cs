namespace get_felica_idm
{
    public class FelicaPoller
    {

        private const int RECV_DATA_LENGTH = 8 + 2;
        private const int INDEX_SW1 = 8;
        private const int INDEX_SW2 = 9;

        private const byte SW1_NORMAL = 0x90;
        private const byte SW2_NORMAL = 0x00;

        /// <summary>
        /// IDm を取得するリクエストを保持します。
        /// </summary>
        private static readonly byte[] getIDmRequest = new byte[] { 0xff, 0xca, 0x00, 0x00, 0x00 };


        private string _idm;

        public string IDm
        {
            get => _idm;
            set
            {
                if (_idm != value)
                {
                    Console.WriteLine($"IDm is '{_idm}'->'{value}'");
                    _idm = value;
                }
            }
        }

        public void StartPolling()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    using (SafeSCardContext cardContext = SmartCard.EstablishContext())
                    {
                        if (cardContext.IsInvalid == true)
                        {
                            IDm = null;
                        }
                        else
                        {
                            using (SafeSCardHandle cardHandle = SmartCard.Connect(cardContext, "FeliCa Port/PaSoRi"))
                            {
                                if (cardHandle.IsInvalid == true)
                                {
                                    IDm = null;
                                }
                                else
                                {
                                    byte[] response = SmartCard.Transmit(cardHandle, getIDmRequest);

                                    if (response == null)
                                    {
                                        // 送受信エラー
                                        throw new ApplicationException("FeliCaとの通信に失敗しました。");
                                    }

                                    if (response.Length != RECV_DATA_LENGTH)
                                    {
                                        // 長さエラー
                                        throw new ApplicationException("FeliCaからの応答長さが異常です。response.Length = " + response.Length);
                                    }

                                    if (response[INDEX_SW1] != SW1_NORMAL || response[INDEX_SW2] != SW2_NORMAL)
                                    {
                                        // レスポンスエラー
                                        throw new ApplicationException("FeliCaからの応答が異常です。SW1 = " + response[8] + " SW2 = " + response[9]);
                                    }

                                    string cardId = BitConverter.ToString(response, 0, response.Length - 2).Replace("-", string.Empty);
                                    IDm = cardId;
                                }
                            }
                        }
                    }

                    Thread.Sleep(250);
                }
            });
        }
    }
}
