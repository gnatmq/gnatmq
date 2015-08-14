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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Session;

namespace uPLibrary.Networking.M2Mqtt.Managers
{
    /// <summary>
    /// Manager for client session
    /// </summary>
    public class MqttSessionManager
    {
        // subscription info for each client
        private Dictionary<string, MqttBrokerSession> sessions;

        /// <summary>
        /// Constructor
        /// </summary>
        public MqttSessionManager()
        {
            this.sessions = new Dictionary<string, MqttBrokerSession>();
        }

        /// <summary>
        /// Save session for a client (all related subscriptions)
        /// </summary>
        /// <param name="clientId">Client Id to save subscriptions</param>
        /// <param name="clientSession">Client session with inflight messages</param>
        /// <param name="subscriptions">Subscriptions to save</param>
        public void SaveSession(string clientId, MqttClientSession clientSession, List<MqttSubscription> subscriptions)
        {
            MqttBrokerSession session = null;

            // session doesn't exist
            if (!this.sessions.ContainsKey(clientId))
            {
                // create new session
                session = new MqttBrokerSession();
                session.ClientId = clientId;

                // add to sessions list
                this.sessions.Add(clientId, session);
            }
            else
            {
                // get existing session
                session = this.sessions[clientId];
            }

            // null reference to disconnected client
            session.Client = null;

            // update subscriptions
            session.Subscriptions = new List<MqttSubscription>();
            foreach (MqttSubscription subscription in subscriptions)
            {
                session.Subscriptions.Add(new MqttSubscription(subscription.ClientId, subscription.Topic, subscription.QosLevel, null));
            }
            
            // update inflight messages
            session.InflightMessages = new Hashtable();
            foreach (MqttMsgContext msgContext in clientSession.InflightMessages.Values)
            {
                session.InflightMessages.Add(msgContext.Message.MessageId, msgContext);
            }
        }

        /// <summary>
        /// Get session for a client
        /// </summary>
        /// <param name="clientId">Client Id to get subscriptions</param>
        /// <returns>Subscriptions for the client</returns>
        public MqttBrokerSession GetSession(string clientId)
        {
            if (!this.sessions.ContainsKey(clientId))
                return null;
            else
                return this.sessions[clientId];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<MqttBrokerSession> GetSessions()
        {
            // TODO : verificare altro modo
            return new List<MqttBrokerSession>(this.sessions.Values);
        }

        /// <summary>
        /// Clear session for a client (all related subscriptions)
        /// </summary>
        /// <param name="clientId">Client Id to clear session</param>
        public void ClearSession(string clientId)
        {
            if (this.sessions.ContainsKey(clientId))
            {
                // clear and remove client session
                this.sessions[clientId].Clear();
                this.sessions.Remove(clientId);
            }
        }
    }
}
