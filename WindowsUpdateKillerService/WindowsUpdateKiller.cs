using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;

namespace WindowsUpdateKillerService
{
    public partial class WindowsUpdateKiller : ServiceBase
    {
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        private const string serviceName = "wuauserv";
        private const int delay = 5000; //in milisecond

        public WindowsUpdateKiller()
        {
            InitializeComponent();
            eventLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("WindowsUpdateKillerSource"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "WindowsUpdateKillerSource", "WindowsUpdateKillerSource");
            }
            eventLog.Source = "WindowsUpdateKillerSource";
            eventLog.Log = "WindowsUpdateKillerSource";
        }

        protected override void OnStart(string[] args)
        {
            eventLog.WriteEntry("In OnStart.");

            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Timer timer = new Timer()
            {
                Interval = delay
            };
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Kill(serviceName);
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("In OnStop.");
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        private void Kill(string serviceName)
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
                        eventLog.WriteEntry($"Success kill service {serviceName}");
                    }
                    catch (Exception err)
                    {
                        eventLog.WriteEntry($"Failed kill service {serviceName}, err: ${err.Message}");
                    }
                }
                else
                {
                    eventLog.WriteEntry($"Service {sc.DisplayName} is not running");
                }
            }
            else
            {
                eventLog.WriteEntry($"Service {serviceName} is not found");
            }
        }
    }
}
