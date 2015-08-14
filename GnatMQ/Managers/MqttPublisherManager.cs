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
using System.Text.RegularExpressions;
using System.Threading;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Session;

namespace uPLibrary.Networking.M2Mqtt.Managers
{
    /// <summary>
    /// Manager for publishing messages
    /// </summary>
    public class MqttPublisherManager
    {
        #region Constants ...

        // topic wildcards '+' and '#'
        private const string PLUS_WILDCARD = "+";
        private const string SHARP_WILDCARD = "#";

        // replace for wildcards '+' and '#' for using regular expression on topic match
        private const string PLUS_WILDCARD_REPLACE = @"[^/]+";
        private const string SHARP_WILDCARD_REPLACE = @".*";

        // name for listener thread
        private const string PUBLISH_THREAD_NAME = "MqttPublishThread";

        #endregion

        // queue messages to publish
        private Queue<MqttMsgBase> publishQueue;

        // event for waiting thread end
        private AutoResetEvent publishEventEnd;
        // event for starting publish
        private AutoResetEvent publishQueueWaitHandle;
        private bool isRunning;

        // reference to subscriber manager
        private MqttSubscriberManager subscriberManager;
        // reference to session manager
        private MqttSessionManager sessionManager;

        // retained messages
        private Dictionary<string, MqttMsgPublish> retainedMessages;

        // subscriptions to send retained messages (new subscriber or reconnected client)
        private Queue<MqttSubscription> subscribersForRetained;

        // client id to send outgoing session messages
        private Queue<string> clientsForSession;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="subscriberManager">Reference to subscriber manager</param>
        /// <param name="sessionManager">Reference to session manager</param>
        public MqttPublisherManager(MqttSubscriberManager subscriberManager, MqttSessionManager sessionManager)
        {
            // save reference to subscriber manager
            this.subscriberManager = subscriberManager;
            // save reference to session manager
            this.sessionManager = sessionManager;

            // create empty list for retained messages
            this.retainedMessages = new Dictionary<string, MqttMsgPublish>();

            // create empty list for destination subscribers for retained message
            this.subscribersForRetained = new Queue<MqttSubscription>();

            // create empty list for destination client for outgoing session message
            this.clientsForSession = new Queue<string>();

            // create publish messages queue
            this.publishQueue = new Queue<MqttMsgBase>();
            this.publishQueueWaitHandle = new AutoResetEvent(false);
        }
        
        /// <summary>
        /// Start publish handling
        /// </summary>
        public void Start()
        {
            this.isRunning = true;
            // create and start thread for publishing messages
            Fx.StartThread(this.PublishThread);
        }

        /// <summary>
        /// Stop publish handling
        /// </summary>
        public void Stop()
        {
            this.isRunning = false;
            this.publishQueueWaitHandle.Set();

            // wait for thread
            this.publishEventEnd.WaitOne();
        }

        /// <summary>
        /// Publish message
        /// </summary>
        /// <param name="publish">Message to publish</param>
        public void Publish(MqttMsgPublish publish)
        {
            if (publish.Retain)
            {
                lock (this.retainedMessages)
                {
                    // retained message already exists for the topic
                    if (retainedMessages.ContainsKey(publish.Topic))
                    {
                        // if empty message, remove current retained message
                        if (publish.Message.Length == 0)
                            retainedMessages.Remove(publish.Topic);
                        // set new retained message for the topic
                        else
                            retainedMessages[publish.Topic] = publish;
                    }
                    else
                    {
                        // add new topic with related retained message
                        retainedMessages.Add(publish.Topic, publish);
                    }
                }
            }

            // enqueue
            lock (this.publishQueue)
            {
                this.publishQueue.Enqueue(publish);
            }

            // unlock thread for sending messages to the subscribers
            this.publishQueueWaitHandle.Set();
        }

        /// <summary>
        /// Publish retained message for a topic to a client
        /// </summary>
        /// <param name="topic">Topic to search for a retained message</param>
        /// <param name="clientId">Client Id to send retained message</param>
        public void PublishRetaind(string topic, string clientId)
        {
            lock (this.subscribersForRetained)
            {
                MqttSubscription subscription = this.subscriberManager.GetSubscription(topic, clientId);

                // add subscription to list of subscribers for receiving retained messages
                if (subscription != null)
                {
                    this.subscribersForRetained.Enqueue(subscription);
                }
            }

            // unlock thread for sending messages to the subscribers
            this.publishQueueWaitHandle.Set();
        }

        /// <summary>
        /// Publish outgoing session messages for a client
        /// </summary>
        /// <param name="clientId">Client Id to send outgoing session messages</param>
        public void PublishSession(string clientId)
        {
            lock (this.clientsForSession)
            {
                this.clientsForSession.Enqueue(clientId);
            }

            // unlock thread for sending messages to the clients
            this.publishQueueWaitHandle.Set();
        }

        /// <summary>
        /// Process the message queue to publish
        /// </summary>
        public void PublishThread()
        {
            int count;
            byte qosLevel;
            MqttMsgPublish publish;

            // create event to signal that current thread is ended
            this.publishEventEnd = new AutoResetEvent(false);

            while (this.isRunning)
            {
                // wait on message queueud to publish
                this.publishQueueWaitHandle.WaitOne();

                // first check new subscribers to send retained messages ...
                lock (this.subscribersForRetained)
                {
                    count = this.subscribersForRetained.Count;

                    // publish retained messages to subscribers (new or reconnected)
                    while (count > 0)
                    {
                        count--;
                        MqttSubscription subscription = this.subscribersForRetained.Dequeue();

                        var query = from p in this.retainedMessages
                                    where (new Regex(subscription.Topic)).IsMatch(p.Key)     // check for topics based also on wildcard with regex
                                    select p.Value;

                        if (query.Count() > 0)
                        {
                            foreach (MqttMsgPublish retained in query)
                            {
                                qosLevel = (subscription.QosLevel < retained.QosLevel) ? subscription.QosLevel : retained.QosLevel;

                                // send PUBLISH message to the current subscriber
                                subscription.Client.Publish(retained.Topic, retained.Message, qosLevel, retained.Retain);
                            }
                        }
                    }
                }

                // ... then check clients to send outgoing session messages
                lock (this.clientsForSession)
                {
                    count = this.clientsForSession.Count;

                    // publish outgoing session messages to clients (reconnected)
                    while (count > 0)
                    {
                        count--;
                        string clientId = this.clientsForSession.Dequeue();

                        MqttBrokerSession session = this.sessionManager.GetSession(clientId);

                        while (session.OutgoingMessages.Count > 0)
                        {
                            MqttMsgPublish outgoingMsg = session.OutgoingMessages.Dequeue();
                            
                            var query = from s in session.Subscriptions
                                where (new Regex(s.Topic)).IsMatch(outgoingMsg.Topic)     // check for topics based also on wildcard with regex
                                select s;

                            MqttSubscription subscription = query.First();

                            if (subscription != null)
                            {
                                qosLevel = (subscription.QosLevel < outgoingMsg.QosLevel) ? subscription.QosLevel : outgoingMsg.QosLevel;

                                session.Client.Publish(outgoingMsg.Topic, outgoingMsg.Message, qosLevel, outgoingMsg.Retain);
                            }
                        }
                    }
                }
                
                // ... then pass to process publish queue
                lock (this.publishQueue)
                {
                    publish = null;

                    count = this.publishQueue.Count;
                    // publish all queued messages
                    while (count > 0)
                    {
                        count--;
                        publish = (MqttMsgPublish)this.publishQueue.Dequeue();

                        if (publish != null)
                        {
                            // get all subscriptions for a topic
                            List<MqttSubscription> subscriptions = this.subscriberManager.GetSubscriptionsByTopic(publish.Topic);

                            if ((subscriptions != null) && (subscriptions.Count > 0))
                            {
                                foreach (MqttSubscription subscription in subscriptions)
                                {
                                    qosLevel = (subscription.QosLevel < publish.QosLevel) ? subscription.QosLevel : publish.QosLevel;

                                    // send PUBLISH message to the current subscriber
                                    subscription.Client.Publish(publish.Topic, publish.Message, qosLevel, publish.Retain);
                                }
                            }

                            // get all sessions
                            List<MqttBrokerSession> sessions = this.sessionManager.GetSessions();

                            if ((sessions != null) && (sessions.Count > 0))
                            {
                                foreach (MqttBrokerSession session in sessions)
                                {
                                    var query = from s in session.Subscriptions
                                                where (new Regex(s.Topic)).IsMatch(publish.Topic)
                                                select s;

                                    MqttSubscriptionComparer comparer = new MqttSubscriptionComparer(MqttSubscriptionComparer.MqttSubscriptionComparerType.OnClientId);

                                    // consider only session active for client disconnected (not online)
                                    if (session.Client == null)
                                    {
                                        foreach (MqttSubscription subscription in query.Distinct(comparer))
                                        {
                                            qosLevel = (subscription.QosLevel < publish.QosLevel) ? subscription.QosLevel : publish.QosLevel;

                                            // save PUBLISH message for client disconnected (not online)
                                            session.OutgoingMessages.Enqueue(publish);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // signal thread end
            this.publishEventEnd.Set();
        }
    }
}
