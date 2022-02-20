﻿namespace get_felica_idm
{
    class Program
    {
        static void Main(string[] args)
        {
            StartPolling();

            Console.ReadKey();

            StopPolling();
        }

        static string _connectedReader;

        static string ConnectedReader
        {
            get => _connectedReader;
            set
            {
                if (_connectedReader != value)
                {
                    if (string.IsNullOrEmpty(value) == false)
                    {
                        Console.WriteLine($"FeliCaリーダーに接続しました。({value})");
                    }
                    else
                    {
                        Console.WriteLine("FeliCaリーダーが切断されました。");
                    }
                    _connectedReader = value;
                }
            }
        }

        static string _idm;

        static string IDm
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

        static CancellationTokenSource _cancellationTokenSource;

        static void StartPolling()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (_cancellationTokenSource.Token.IsCancellationRequested == false)
                {
                    using (FeliCa felica = new FeliCa())
                    {
                        ConnectedReader = felica.ConnectedReader;
                        IDm = felica.GetIDm();
                    }

                    Thread.Sleep(250);
                }
            });
        }

        static void StopPolling()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
