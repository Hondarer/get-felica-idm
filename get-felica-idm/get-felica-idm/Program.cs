using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace get_felica_idm
{
    class Program
    {
        static void Main(string[] args)
        {
            FelicaInterface felicaPoller = new FelicaInterface();
            felicaPoller.StartPolling();

            Console.ReadKey();
        }
    }
}
