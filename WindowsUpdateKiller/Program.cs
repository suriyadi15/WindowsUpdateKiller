using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace WindowsUpdateKiller
{
    class Program
    {
        private const string serviceName = "wuauserv";
        private const int delay = 5000; //in milisecond
        static void Main(string[] args)
        {
            if (!AdministratorUtil.IsRunAsAdmin())
            {
                Console.WriteLine("This program must be run as an administrator!");
                Console.ReadKey();
                return;
            }

            Thread t = new Thread(new ThreadStart(KillRunner))
            {
                IsBackground = true
            };

            t.Start();

            Console.WriteLine("Windows Update Killer is running...");

            Console.ReadKey();
        }

        private static void KillRunner()
        {
            while (true)
            {
                Kill(serviceName);
                Thread.Sleep(delay);
            }
        }

        private static void Kill(string serviceName)
        {
            ServiceController sc = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == serviceName);

            if (sc != null)
            {
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    try
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped);
                        Console.WriteLine($"Success kill service {serviceName}");
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine($"Failed kill service {serviceName}, err: ${err.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Service {sc.DisplayName} is not running");
                }
            }
            else
            {
                Console.WriteLine($"Service {serviceName} is not found");
            }
        }
    }
}
