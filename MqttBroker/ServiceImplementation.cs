using MqttBrokerService.Framework;
using MqttBrokerService.Properties;
using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using uPLibrary.Networking.M2Mqtt;
using MqttUtility = uPLibrary.Networking.M2Mqtt.Utility;

namespace MqttBrokerService
{
    /// <summary>
    /// The actual implementation of the windows service goes here...
    /// </summary>
    [WindowsService("MqttBroker",
        DisplayName = "MqttBroker",
        Description = "MqttBroker service.",
        EventLogSource = "MqttBroker",
        StartMode = ServiceStartMode.Automatic)]
    public class ServiceImplementation : IWindowsService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private MqttBroker broker;

        private static SqlConnection connection;

        public ServiceImplementation()
        {
#if TRACE
            MqttUtility.Trace.TraceLevel = MqttUtility.TraceLevel.Verbose | MqttUtility.TraceLevel.Frame;
            MqttUtility.Trace.TraceListener = (f, a) => log.DebugFormat(f, a);
#endif
        }

        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            byte[] buffer4;
            if (hashedPassword == null)
            {
                return false;
            }
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            byte[] src = Convert.FromBase64String(hashedPassword);
            if ((src.Length != 0x31) || (src[0] != 0))
            {
                return false;
            }
            byte[] dst = new byte[0x10];
            Buffer.BlockCopy(src, 1, dst, 0, 0x10);
            byte[] buffer3 = new byte[0x20];
            Buffer.BlockCopy(src, 0x11, buffer3, 0, 0x20);
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, dst, 0x3e8))
            {
                buffer4 = bytes.GetBytes(0x20);
            }
            return ByteArraysEqual(buffer3, buffer4);
        }

        public static bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (b1 == b2) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }

        /// <summary>
        /// This method is called when the service gets a request to start.
        /// </summary>
        /// <param name="args">Any command line arguments</param>
        public void OnStart(string[] args)
        {
            try
            {
                if (Settings.Default.SSL)
                {
                    X509Certificate2 serverCert = null;
                    try
                    {
                        //Tested only with cert issued by Letsencrypt with tools https://github.com/Lone-Coder/letsencrypt-win-simple
                        X509Store store = new X509Store("WebHosting", StoreLocation.LocalMachine);
                        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                        serverCert = store.Certificates.Find(X509FindType.FindBySubjectName, Settings.Default.DnsName, false)[0];
                    }
                    catch (Exception ex)
                    {

                        throw new Exception($"Error retriving certificate for {Settings.Default.DnsName}", ex);
                    }

                    broker = new MqttBroker(serverCert, MqttSslProtocols.TLSv1_2, 1000);
                }
                else
                    broker = new MqttBroker();

                broker.UserAuth = (username, password) =>
                {
                    var result = Settings.Default.Username == username && Settings.Default.Password == password;

                    if (result)
                        return true;
                    else
                    {
                        try
                        {
                            using (SqlCommand command = new SqlCommand(String.Format("Select * From [AspNetUsers] where UserName='{0}'", username), connection))
                            {
                                var reader = command.ExecuteReader();

                                if (reader.Read())
                                {
                                    result = VerifyHashedPassword(reader.GetString(11), password);
                                }

                                reader.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }

                        return result;
                    }

                };

                connection = new SqlConnection(Properties.Settings.Default.connectionString);

                connection.StateChange += Connection_StateChange;

                connection.Open();

                broker.Start();

                log.Info($"MQTT broker service started (SSL:{Settings.Default.SSL})");
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);

                throw ex;
            }
        }

        private void Connection_StateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            if (e.CurrentState == System.Data.ConnectionState.Closed)
                connection.Open();
        }


        /// <summary>
        /// This method is called when the service gets a request to stop.
        /// </summary>
        public void OnStop()
        {
            broker.Stop();

            connection.StateChange -= Connection_StateChange;

            connection.Close();

            log.Info("MQTT broker service stopped");
        }


        /// <summary>
        /// This method is called when a service gets a request to pause,
        /// but not stop completely.
        /// </summary>
        public void OnPause()
        {
        }

        /// <summary>
        /// This method is called when a service gets a request to resume 
        /// after a pause is issued.
        /// </summary>
        public void OnContinue()
        {
        }

        /// <summary>
        /// This method is called when the machine the service is running on
        /// is being shutdown.
        /// </summary>
        public void OnShutdown()
        {
        }

        /// <summary>
        /// This method is called when a custom command is issued to the service.
        /// </summary>
        /// <param name="command">The command identifier to execute.</param >
        public void OnCustomCommand(int command)
        {
        }
    }
}
