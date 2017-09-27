using System;
using System.ServiceProcess;

namespace MqttBrokerService.Framework
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class WindowsServiceAttribute : Attribute
    {
        /// <summary>
        /// The name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The displayable name that shows in service manager (defaults to Name).
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// A textural description of the service name (defaults to Name).
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The user to run the service under (defaults to null).  A null or empty
        /// UserName field causes the service to run as ServiceAccount.LocalService.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password to run the service under (defaults to null).  Ignored
        /// if the UserName is empty or null, this property is ignored.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Specifies the event log source to set the service's EventLog to.  If this is
        /// empty or null (the default) no event log source is set.  If set, will auto-log
        /// start and stop events.
        /// </summary>
        public string EventLogSource { get; set; }

        /// <summary>
        /// The method to start the service when the machine reboots (defaults to Manual).
        /// </summary>
        public ServiceStartMode StartMode { get; set; }

        /// <summary>
        /// True if service supports pause and continue (defaults to true).
        /// </summary>
        public bool CanPauseAndContinue { get; set; }

        /// <summary>
        /// True if service supports shutdown event (defaults to true).
        /// </summary>
        public bool CanShutdown { get; set; }

        /// <summary>
        /// True if service supports stop event (defaults to true).
        /// </summary>
        public bool CanStop { get; set; }

        /// <summary>
        /// Services the service depends on
        /// </summary>
        public string[] ServiceDependsOn { get; set; }

        /// <summary>
        /// Marks an IWindowsService with configuration and installation attributes.
        /// </summary>
        /// <param name="name">The name of the windows service.</param>
        public WindowsServiceAttribute(string name)
        {
            // Set name and default description and display name to name.
            Name = name;
            Description = name;
            DisplayName = name;

            // Default all other attributes.
            ServiceDependsOn = new string[0];
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = true;
            StartMode = ServiceStartMode.Manual;
            EventLogSource = null;
            Password = null;
            UserName = null;
        }
    }
}