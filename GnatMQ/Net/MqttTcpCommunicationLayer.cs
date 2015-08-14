/*
Copyright (c) 2013, 2014 Paolo Patierno

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Paolo Patierno - initial API and implementation and/or initial documentation
*/

using System;
#if SSL
#if (MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3)
using Microsoft.SPOT.Net.Security;
#else
using System.Net.Security;
#endif
#endif
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace uPLibrary.Networking.M2Mqtt.Communication
{
    /// <summary>
    /// MQTT communication layer
    /// </summary>
    public class MqttTcpCommunicationLayer : IMqttCommunicationLayer
    {
        #region Constants ...

        // name for listener thread
        private const string LISTENER_THREAD_NAME = "MqttListenerThread";
        // option to accept only connection from IPv6 (or IPv4 too)
        private const int IPV6_V6ONLY = 27;

        #endregion

        #region Properties ...

        /// <summary>
        /// TCP listening port
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Secure connection (SSL/TLS)
        /// </summary>
        public bool Secure { get; private set; }

        /// <summary>
        /// X509 Server certificate
        /// </summary>
        public X509Certificate ServerCert { get; private set; }

        /// <summary>
        /// SSL/TLS protocol version
        /// </summary>
        public MqttSslProtocols Protocol { get; private set; }

#if !(MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || COMPACT_FRAMEWORK)
        /// <summary>
        /// A RemoteCertificateValidationCallback delegate responsible for validating the certificate supplied by the remote party
        /// </summary>
        public RemoteCertificateValidationCallback UserCertificateValidationCallback { get; private set; }
        
        /// <summary>
        /// A LocalCertificateSelectionCallback delegate responsible for selecting the certificate used for authentication
        /// </summary>
        public LocalCertificateSelectionCallback UserCertificateSelectionCallback { get; private set; }
#endif

        #endregion

        // TCP listener for incoming connection requests
        private TcpListener listener;

        // TCP listener thread
        private Thread thread;
        private bool isRunning;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="port">TCP listening port</param>
        public MqttTcpCommunicationLayer(int port)
#if !(MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || COMPACT_FRAMEWORK)
            : this(port, false, null, MqttSslProtocols.None, null, null)
#else
            : this(port, false, null, MqttSslProtocols.None)
#endif
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="port">TCP listening port</param>
        /// <param name="secure">Secure connection (SSL/TLS)</param>
        /// <param name="serverCert">X509 server certificate</param>
        /// <param name="protocol">SSL/TLS protocol version</param>
#if !(MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || COMPACT_FRAMEWORK)
        /// <param name="userCertificateSelectionCallback">A RemoteCertificateValidationCallback delegate responsible for validating the certificate supplied by the remote party</param>
        /// <param name="userCertificateValidationCallback">A LocalCertificateSelectionCallback delegate responsible for selecting the certificate used for authentication</param>
        public MqttTcpCommunicationLayer(int port, bool secure, X509Certificate serverCert, MqttSslProtocols protocol,
            RemoteCertificateValidationCallback userCertificateValidationCallback,
            LocalCertificateSelectionCallback userCertificateSelectionCallback)
#else
        public MqttTcpCommunicationLayer(int port, bool secure, X509Certificate serverCert, MqttSslProtocols protocol)
#endif
        {
            if (secure && serverCert == null)
                throw new ArgumentException("Secure connection requested but no server certificate provided");

            this.Port = port;
            this.Secure = secure;
            this.ServerCert = serverCert;
            this.Protocol = protocol;
#if !(MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || COMPACT_FRAMEWORK)
            this.UserCertificateValidationCallback = userCertificateValidationCallback;
            this.UserCertificateSelectionCallback = userCertificateSelectionCallback;
#endif
        }

#region IMqttCommunicationLayer ...

        // client connected event
        public event MqttClientConnectedEventHandler ClientConnected;

        /// <summary>
        /// Start communication layer listening
        /// </summary>
        public void Start()
        {
            this.isRunning = true;

            // create and start listener thread
            this.thread = new Thread(this.ListenerThread);
            this.thread.Name = LISTENER_THREAD_NAME;
            this.thread.Start();
        }

        /// <summary>
        /// Stop communication layer listening
        /// </summary>
        public void Stop()
        {
            this.isRunning = false;

            this.listener.Stop();

            // wait for thread
            this.thread.Join();
        }

#endregion

        /// <summary>
        /// Listener thread for incoming connection requests
        /// </summary>
        private void ListenerThread()
        {
            // create listener...
            this.listener = new TcpListener(IPAddress.IPv6Any, this.Port);
            // set socket option 27 (IPV6_V6ONLY) to false to accept also connection on IPV4 (not only IPV6 as default)
            this.listener.Server.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)IPV6_V6ONLY, false);
            // ...and start it
            this.listener.Start();

            while (this.isRunning)
            {
                try
                {
                    // blocking call to wait for client connection
                    Socket socketClient = this.listener.AcceptSocket();

                    // manage socket client connected
                    if (socketClient.Connected)
                    {
                        // create network channel to accept connection request
                        IMqttNetworkChannel channel = null;
#if SSL
                        if (this.Secure)
                        {
#if !(MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || COMPACT_FRAMEWORK)
                            channel = new MqttNetworkChannel(socketClient, this.Secure, this.ServerCert, this.Protocol, this.UserCertificateValidationCallback, this.UserCertificateSelectionCallback);
#else
                            channel = new MqttNetworkChannel(socketClient, this.Secure, this.ServerCert, this.Protocol);
#endif
                        }
                        else
                        {
                            channel = new MqttNetworkChannel(socketClient);
                        }
#else
                        channel = new MqttNetworkChannel(socketClient);
#endif
                        channel.Accept();

                        // handling channel for connected client
                        MqttClient client = new MqttClient(channel);
                        // raise client raw connection event
                        this.OnClientConnected(client);
                    }
                }
                catch (Exception)
                {
                    if (!this.isRunning)
                        return;
                }
            }
        }

        /// <summary>
        /// Raise client connected event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnClientConnected(MqttClient client)
        {
            if (this.ClientConnected != null)
                this.ClientConnected(this, new MqttClientConnectedEventArgs(client));
        }
    }
}
