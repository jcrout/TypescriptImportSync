using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TypescriptImportSync;

namespace TypescriptImportSync.ConsoleApp
{
    class Program
    {
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        static int Main(string[] args)
        {
            var monitorPath = "";
            if (args.Length > 0 && Directory.Exists(args[0]))
            {
                monitorPath = args[0];
            }
            else
            {
                Console.Write("Enter path to monitor: ");
                monitorPath = Console.ReadLine();

                while (!Directory.Exists(monitorPath))
                {
                    Console.Write("Invalid path. Please enter a valid path: ");
                    monitorPath = Console.ReadLine();
                }
            }

            Console.WriteLine("Press C and Enter to exit.");

            var config = Configuration.Default;
            var tsObserver = new TSObserver(config);
            var handler = new ConsoleEventDelegate(eventType => {
                if (eventType == 2)
                {
                    if (tsObserver != null)
                    {
                        tsObserver.Dispose();
                    }                   
                }
                return false;
            });

            SetConsoleCtrlHandler(handler, true);           
            tsObserver.Monitor(monitorPath);

            String input;
            while ((input = Console.ReadLine() ?? "").ToLower() != "c")
            {
            }

            tsObserver.Dispose();
            tsObserver = null;

            return 0;
        }    
    }
}
