using System;
using System.Linq;
using System.ServiceProcess;
using MqttBrokerService.Framework;

namespace MqttBrokerService
{
    static class Program
    {
        // The main entry point for the windows service application.
        static void Main(string[] args)
        {
            try
            {
                // If install was a command line flag, then run the installer at runtime.
                if (args.Contains("-install", StringComparer.InvariantCultureIgnoreCase))
                {
                    WindowsServiceInstaller.RuntimeInstall<ServiceImplementation>();
                }

                // If uninstall was a command line flag, run uninstaller at runtime.
                else if (args.Contains("-uninstall", StringComparer.InvariantCultureIgnoreCase))
                {
                    WindowsServiceInstaller.RuntimeUnInstall<ServiceImplementation>();
                }

                // Otherwise, fire up the service as either console or windows service based on UserInteractive property.
                else
                {
                    var implementation = new ServiceImplementation();

                    // If started from console, file explorer, etc, run as console app.
                    if (Environment.UserInteractive)
                    {
                        ConsoleHarness.Run(args, implementation);
                    }

                    // Otherwise run as a windows service
                    else
                    {
                        ServiceBase.Run(new WindowsServiceHarness(implementation));
                    }
                }
            }

            catch (Exception ex)
            {
                ConsoleHarness.WriteToConsole(ConsoleColor.Red, "An exception occurred in Main(): {0}", ex);
            }
        }
    }
}
