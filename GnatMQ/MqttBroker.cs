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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Managers;
using uPLibrary.Networking.M2Mqtt.Communication;
using uPLibrary.Networking.M2Mqtt.Session;
#if SSL
#if !(WINDOWS_APP || WINDOWS_PHONE_APP)
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
#elif (MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3)
using Microsoft.SPOT.Net.Security;
#endif
#endif

namespace uPLibrary.Networking.M2Mqtt
{
    /// <summary>
    /// MQTT broker business logic
    /// </summary>
    public class MqttBroker
    {
        // MQTT broker settings
        private MqttSettings settings;

        // clients connected list
        private MqttClientCollection clients;

        // reference to publisher manager
        private MqttPublisherManager publisherManager;
        
        // reference to subscriber manager
        private MqttSubscriberManager subscriberManager;

        // reference to session manager
        private MqttSessionManager sessionManager;

        // reference to User Access Control manager
        private MqttUacManager uacManager;

        // MQTT communication layer
        private IMqttCommunicationLayer commLayer;

        /// <summary>
        /// User authentication method
        /// </summary>
        public MqttUserAuthenticationDelegate UserAuth
        {
            get { return this.uacManager.UserAuth; }
            set { this.uacManager.UserAuth = value; }
        }

        /// <summary>
        /// Constructor (TCP/IP communication layer on port 1883 and default settings)
        /// </summary>
        public MqttBroker()
            : this(new MqttTcpCommunicationLayer(MqttSettings.MQTT_BROKER_DEFAULT_PORT), MqttSettings.Instance)
        {
        }

#if !(WINDOWS_APP || WINDOWS_PHONE_APP || MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || COMPACT_FRAMEWORK)
        /// <summary>
        /// Constructor (TCP/IP communication layer on port 8883 with SSL/TLS and default settings)
        /// </summary>
        /// <param name="serverCert">X509 Server certificate</param>
        /// <param name="sslProtocol">SSL/TLS protocol versiokn</param>
        public MqttBroker(X509Certificate serverCert, MqttSslProtocols sslProtocol)
            : this(new MqttTcpCommunicationLayer(MqttSettings.MQTT_BROKER_DEFAULT_SSL_PORT, true, serverCert, sslProtocol, null, null), MqttSettings.Instance)
        {
        }
#endif

#if !(WINDOWS_APP || WINDOWS_PHONE_APP || MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || COMPACT_FRAMEWORK)
        /// <summary>
        /// Constructor (TCP/IP communication layer on port 8883 with SSL/TLS and default settings)
        /// </summary>
        /// <param name="serverCert">X509 Server certificate</param>
        /// <param name="sslProtocol">SSL/TLS protocol version</param>
        /// <param name="userCertificateSelectionCallback">A RemoteCertificateValidationCallback delegate responsible for validating the certificate supplied by the remote party</param>
        /// <param name="userCertificateValidationCallback">A LocalCertificateSelectionCallback delegate responsible for selecting the certificate used for authentication</param>
        public MqttBroker(X509Certificate serverCert, MqttSslProtocols sslProtocol,
            RemoteCertificateValidationCallback userCertificateValidationCallback,
            LocalCertificateSelectionCallback userCertificateSelectionCallback)
            : this(new MqttTcpCommunicationLayer(MqttSettings.MQTT_BROKER_DEFAULT_SSL_PORT, true, serverCert, sslProtocol, userCertificateValidationCallback, userCertificateSelectionCallback), MqttSettings.Instance)
        {
        }
#endif

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="commLayer">Communication layer to use (TCP)</param>
        /// <param name="settings">Broker settings</param>
        public MqttBroker(IMqttCommunicationLayer commLayer, MqttSettings settings)
        {
            // MQTT broker settings
            this.settings = settings;

            // MQTT communication layer
            this.commLayer = commLayer;
            this.commLayer.ClientConnected += commLayer_ClientConnected;           

            // create managers (publisher, subscriber, session and UAC)
            this.subscriberManager = new MqttSubscriberManager();
            this.sessionManager = new MqttSessionManager();
            this.publisherManager = new MqttPublisherManager(this.subscriberManager, this.sessionManager);
            this.uacManager = new MqttUacManager();

            this.clients = new MqttClientCollection();
        }

        /// <summary>
        /// Start broker
        /// </summary>
        public void Start()
        {
            this.commLayer.Start();
            this.publisherManager.Start();
        }

        /// <summary>
        /// Stop broker
        /// </summary>
        public void Stop()
        {
            this.commLayer.Stop();
            this.publisherManager.Stop();

            // close connection with all clients
            foreach (MqttClient client in this.clients)
            {
                client.Close();
            }
        }

        /// <summary>
        /// Close a client
        /// </summary>
        /// <param name="client">Client to close</param>
        private void CloseClient(MqttClient client)
        {
            if (this.clients.Contains(client))
            {
                // if client is connected and it has a will message
                if (!client.IsConnected && client.WillFlag)
                {
                    // create the will PUBLISH message
                    MqttMsgPublish publish =
                        new MqttMsgPublish(client.WillTopic, Encoding.UTF8.GetBytes(client.WillMessage), false, client.WillQosLevel, false);

                    // publish message through publisher manager
                    this.publisherManager.Publish(publish);
                }

                // if not clean session
                if (!client.CleanSession)
                {
                    List<MqttSubscription> subscriptions = this.subscriberManager.GetSubscriptionsByClient(client.ClientId);

                    if ((subscriptions != null) && (subscriptions.Count > 0))
                    {
                        this.sessionManager.SaveSession(client.ClientId, client.Session, subscriptions);

                        // TODO : persist client session if broker close
                    }
                }

                // delete client from runtime subscription
                this.subscriberManager.Unsubscribe(client);

                // close the client
                client.Close();

                // remove client from the collection
                this.clients.Remove(client);
            }
        }

        void commLayer_ClientConnected(object sender, MqttClientConnectedEventArgs e)
        {
            // register event handlers from client
            e.Client.MqttMsgDisconnected += Client_MqttMsgDisconnected;
            e.Client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            e.Client.MqttMsgConnected += Client_MqttMsgConnected;
            e.Client.MqttMsgSubscribeReceived += Client_MqttMsgSubscribeReceived;
            e.Client.MqttMsgUnsubscribeReceived += Client_MqttMsgUnsubscribeReceived;
            e.Client.ConnectionClosed += Client_ConnectionClosed;

            // add client to the collection
            this.clients.Add(e.Client);

            // start client threads
            e.Client.Open();
        }

        void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            MqttClient client = (MqttClient)sender;

            // create PUBLISH message to publish
            // [v3.1.1] DUP flag from an incoming PUBLISH message is not propagated to subscribers
            //          It should be set in the outgoing PUBLISH message based on transmission for each subscriber
            MqttMsgPublish publish = new MqttMsgPublish(e.Topic, e.Message, false, e.QosLevel, e.Retain);
            
            // publish message through publisher manager
            this.publisherManager.Publish(publish);
        }

        void Client_MqttMsgUnsubscribeReceived(object sender, MqttMsgUnsubscribeEventArgs e)
        {
            MqttClient client = (MqttClient)sender;

            for (int i = 0; i < e.Topics.Length; i++)
            {
                // unsubscribe client for each topic requested
                this.subscriberManager.Unsubscribe(e.Topics[i], client);
            }

            try
            {
                // send UNSUBACK message to the client
                client.Unsuback(e.MessageId);
            }
            catch (MqttCommunicationException)
            {
                this.CloseClient(client);
            }
        }

        void Client_MqttMsgSubscribeReceived(object sender, MqttMsgSubscribeEventArgs e)
        {
            MqttClient client = (MqttClient)sender;

            for (int i = 0; i < e.Topics.Length; i++)
            {
                // TODO : business logic to grant QoS levels based on some conditions ?
                //        now the broker granted the QoS levels requested by client

                // subscribe client for each topic and QoS level requested
                this.subscriberManager.Subscribe(e.Topics[i], e.QoSLevels[i], client);
            }

            try
            {
                // send SUBACK message to the client
                client.Suback(e.MessageId, e.QoSLevels);

                for (int i = 0; i < e.Topics.Length; i++)
                {
                    // publish retained message on the current subscription
                    this.publisherManager.PublishRetaind(e.Topics[i], client.ClientId);
                }
            }
            catch (MqttCommunicationException)
            {
                this.CloseClient(client);
            }
        }

        void Client_MqttMsgConnected(object sender, MqttMsgConnectEventArgs e)
        {
            // [v3.1.1] session present flag
            bool sessionPresent = false;
            // [v3.1.1] generated client id for client who provides client id zero bytes length
            string clientId = null;

            MqttClient client = (MqttClient)sender;

            // verify message to determine CONNACK message return code to the client
            byte returnCode = this.MqttConnectVerify(e.Message);

            // [v3.1.1] if client id is zero length, the broker assigns a unique identifier to it
            clientId = (e.Message.ClientId.Length != 0) ? e.Message.ClientId : Guid.NewGuid().ToString();

            // connection "could" be accepted
            if (returnCode == MqttMsgConnack.CONN_ACCEPTED)
            {
                // check if there is a client already connected with same client Id
                MqttClient clientConnected = this.GetClient(clientId);

                // force connection close to the existing client (MQTT protocol)
                if (clientConnected != null)
                {
                    this.CloseClient(clientConnected);
                }
            }

            try
            {
                // connection accepted, load (if exists) client session
                if (returnCode == MqttMsgConnack.CONN_ACCEPTED)
                {
                    // check if not clean session and try to recovery a session
                    if (!e.Message.CleanSession)
                    {
                        // create session for the client
                        MqttClientSession clientSession = new MqttClientSession(clientId);

                        // get session for the connected client
                        MqttBrokerSession session = this.sessionManager.GetSession(clientId);

                        // set inflight queue into the client session
                        if (session != null)
                        {
                            clientSession.InflightMessages = session.InflightMessages;
                            // [v3.1.1] session present flag
                            if (client.ProtocolVersion == MqttProtocolVersion.Version_3_1_1)
                                sessionPresent = true;
                        }

                        // send CONNACK message to the client
                        client.Connack(e.Message, returnCode, clientId, sessionPresent);

                        // load/inject session to the client
                        client.LoadSession(clientSession);

                        if (session != null)
                        {
                            // set reference to connected client into the session
                            session.Client = client;

                            // there are saved subscriptions
                            if (session.Subscriptions != null)
                            {
                                // register all subscriptions for the connected client
                                foreach (MqttSubscription subscription in session.Subscriptions)
                                {
                                    this.subscriberManager.Subscribe(subscription.Topic, subscription.QosLevel, client);

                                    // publish retained message on the current subscription
                                    this.publisherManager.PublishRetaind(subscription.Topic, clientId);
                                }
                            }

                            // there are saved outgoing messages
                            if (session.OutgoingMessages.Count > 0)
                            {
                                // publish outgoing messages for the session
                                this.publisherManager.PublishSession(session.ClientId);
                            }
                        }
                    }
                    // requested clean session
                    else
                    {
                        // send CONNACK message to the client
                        client.Connack(e.Message, returnCode, clientId, sessionPresent);

                        this.sessionManager.ClearSession(clientId);
                    }
                }
                else
                {
                    // send CONNACK message to the client
                    client.Connack(e.Message, returnCode, clientId, sessionPresent);
                }
            }
            catch (MqttCommunicationException)
            {
                this.CloseClient(client);
            }
        }

        void Client_MqttMsgDisconnected(object sender, EventArgs e)
        {
            MqttClient client = (MqttClient)sender;

            // close the client
            this.CloseClient(client);
        }

        void Client_ConnectionClosed(object sender, EventArgs e)
        {
            MqttClient client = (MqttClient)sender;

            // close the client
            this.CloseClient(client);
        }

        /// <summary>
        /// Check CONNECT message to accept or not the connection request 
        /// </summary>
        /// <param name="connect">CONNECT message received from client</param>
        /// <returns>Return code for CONNACK message</returns>
        private byte MqttConnectVerify(MqttMsgConnect connect)
        {
            byte returnCode = MqttMsgConnack.CONN_ACCEPTED;

            // unacceptable protocol version
            if ((connect.ProtocolVersion != MqttMsgConnect.PROTOCOL_VERSION_V3_1) &&
                (connect.ProtocolVersion != MqttMsgConnect.PROTOCOL_VERSION_V3_1_1))
                returnCode = MqttMsgConnack.CONN_REFUSED_PROT_VERS;
            else
            {
                // client id length exceeded (only for old MQTT 3.1)
                if  ((connect.ProtocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1) &&
                     (connect.ClientId.Length > MqttMsgConnect.CLIENT_ID_MAX_LENGTH))
                    returnCode = MqttMsgConnack.CONN_REFUSED_IDENT_REJECTED;
                else
                {
                    // [v.3.1.1] client id zero length is allowed but clean session must be true
                    if ((connect.ClientId.Length == 0) && (!connect.CleanSession))
                        returnCode = MqttMsgConnack.CONN_REFUSED_IDENT_REJECTED;
                    else
                    {
                        // check user authentication
                        if (!this.uacManager.UserAuthentication(connect.Username, connect.Password))
                            returnCode = MqttMsgConnack.CONN_REFUSED_USERNAME_PASSWORD;
                        // server unavailable and not authorized ?
                        else
                        {
                            // TODO : other checks on CONNECT message
                        }
                    }
                }
            }

            return returnCode;
        }

        /// <summary>
        /// Return reference to a client with a specified Id is already connected
        /// </summary>
        /// <param name="clientId">Client Id to verify</param>
        /// <returns>Reference to client</returns>
        private MqttClient GetClient(string clientId)
        {
            var query = from c in this.clients
                        where c.ClientId == clientId
                        select c;

            return query.FirstOrDefault();
        }
    }
}
