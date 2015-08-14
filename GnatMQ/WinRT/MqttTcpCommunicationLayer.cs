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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

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

        #endregion

        #region Properties ...

        /// <summary>
        /// TCP listening port
        /// </summary>
        public int Port { get; private set; }

        #endregion

        // TCP listener for incoming connection requests
        private StreamSocketListener listener;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="port">TCP listening port</param>
        public MqttTcpCommunicationLayer(int port)
        {
            this.Port = port;
        }

        #region IMqttCommunicationLayer ...

        // client connected event
        public event MqttClientConnectedEventHandler ClientConnected;

        /// <summary>
        /// Start communication layer listening
        /// </summary>
        public async void Start()
        {
            this.listener = new StreamSocketListener();
            this.listener.ConnectionReceived += listener_ConnectionReceived;
            await this.listener.BindServiceNameAsync(this.Port.ToString());
        }

        private async void listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            await Task.Factory.StartNew
                (() => 
                {
                    try
                    {
                        // create network channel to accept connection request
                        IMqttNetworkChannel channel = new MqttNetworkChannel(args.Socket);
                        channel.Accept();

                        // handling channel for connected client
                        MqttClient client = new MqttClient(channel);
                        // raise client raw connection event
                        this.OnClientConnected(client);
                    }
                    catch (Exception)
                    {
                        // TODO : check errors/exceptions on accepting connection
                    }
                });
        }

        /// <summary>
        /// Stop communication layer listening
        /// </summary>
        public void Stop()
        {
            this.listener.Dispose();
        }

        #endregion

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
