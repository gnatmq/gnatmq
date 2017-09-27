using System;
using System.ServiceProcess;


namespace MqttBrokerService.Framework
{
    /// <summary>
    /// A generic Windows Service that can handle any assembly that
    /// implements IWindowsService (including AbstractWindowsService) 
    /// </summary>
    public partial class WindowsServiceHarness : ServiceBase
    {
        /// <summary>
        /// Get the class implementing the windows service
        /// </summary>
        public IWindowsService ServiceImplementation { get; private set; }

        /// <summary>
        /// Constructor a generic windows service from the given class
        /// </summary>
        /// <param name="serviceImplementation">Service implementation.</param>
        public WindowsServiceHarness(IWindowsService serviceImplementation)
        {
            // make sure service passed in is valid
            if (serviceImplementation == null)
            {
                throw new ArgumentNullException("serviceImplementation",
                    "IWindowsService cannot be null in call to GenericWindowsService");
            }

            // set instance and backward instance
            ServiceImplementation = serviceImplementation;

            // configure our service
            ConfigureServiceFromAttributes(serviceImplementation);
        }

        /// <summary>
        /// Override service control on continue
        /// </summary>
        protected override void OnContinue()
        {
            // perform class specific behavior 
            ServiceImplementation.OnContinue();
        }

        /// <summary>
        /// Called when service is paused
        /// </summary>
        protected override void OnPause()
        {
            // perform class specific behavior 
            ServiceImplementation.OnPause();
        }

        /// <summary>
        /// Called when a custom command is requested
        /// </summary>
        /// <param name="command">Id of custom command</param>
        protected override void OnCustomCommand(int command)
        {
            // perform class specific behavior 
            ServiceImplementation.OnCustomCommand(command);
        }

        /// <summary>
        /// Called when the Operating System is shutting down
        /// </summary>
        protected override void OnShutdown()
        {
            // perform class specific behavior
            ServiceImplementation.OnShutdown();
        }

        /// <summary>
        /// Called when service is requested to start
        /// </summary>
        /// <param name="args">The startup arguments array.</param>
        protected override void OnStart(string[] args)
        {
            ServiceImplementation.OnStart(args);
        }

        /// <summary>
        /// Called when service is requested to stop
        /// </summary>
        protected override void OnStop()
        {
            ServiceImplementation.OnStop();
        }

        /// <summary>
        /// Set configuration data
        /// </summary>
        /// <param name="serviceImplementation">The service with configuration settings.</param>
        private void ConfigureServiceFromAttributes(IWindowsService serviceImplementation)
        {
            var attribute = serviceImplementation.GetType().GetAttribute<WindowsServiceAttribute>();

            if (attribute != null)
            {
                // wire up the event log source, if provided
                if (!string.IsNullOrWhiteSpace(attribute.EventLogSource))
                {
                    // assign to the base service's EventLog property for auto-log events.
                    EventLog.Source = attribute.EventLogSource;
                }

                CanStop = attribute.CanStop;
                CanPauseAndContinue = attribute.CanPauseAndContinue;
                CanShutdown = attribute.CanShutdown;

                // we don't handle: laptop power change event
                CanHandlePowerEvent = false;

                // we don't handle: Term Services session event
                CanHandleSessionChangeEvent = false;

                // always auto-event-log 
                AutoLog = true;
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format("IWindowsService implementer {0} must have a WindowsServiceAttribute.",
                                  serviceImplementation.GetType().FullName));
            }
        }
    }
}
