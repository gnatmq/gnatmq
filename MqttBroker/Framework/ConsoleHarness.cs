using System;

namespace MqttBrokerService.Framework
{
    public static class ConsoleHarness
    {
        // Run a service from the console given a service implementation
        public static void Run(string[] args, IWindowsService service)
        {
            string serviceName = service.GetType().Name;
            bool isRunning = true;

            // simulate starting the windows service
            service.OnStart(args);

            // let it run as long as Q is not pressed
            while (isRunning)
            {
                WriteToConsole(ConsoleColor.Yellow, "Enter either [Q]uit, [P]ause, [R]esume : ");
                isRunning = HandleConsoleInput(service, Console.ReadLine());
            }

            // stop and shutdown
            service.OnStop();
            service.OnShutdown();
        }


        // Private input handler for console commands.
        private static bool HandleConsoleInput(IWindowsService service, string line)
        {
            bool canContinue = true;

            // check input
            if (line != null)
            {
                switch (line.ToUpper())
                {
                    case "Q":
                        canContinue = false;
                        break;

                    case "P":
                        service.OnPause();
                        break;

                    case "R":
                        service.OnContinue();
                        break;

                    default:
                        WriteToConsole(ConsoleColor.Red, "Did not understand that input, try again.");
                        break;
                }
            }

            return canContinue;
        }


        // Helper method to write a message to the console at the given foreground color.
        internal static void WriteToConsole(ConsoleColor foregroundColor, string format,
            params object[] formatArguments)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;

            Console.WriteLine(format, formatArguments);
            Console.Out.Flush();

            Console.ForegroundColor = originalColor;
        }
    }
}
