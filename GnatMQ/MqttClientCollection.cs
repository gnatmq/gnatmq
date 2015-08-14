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

namespace uPLibrary.Networking.M2Mqtt
{
    /// <summary>
    /// MQTT client collection
    /// </summary>
    public class MqttClientCollection : IList<MqttClient>, IEnumerable
    {
        // clients list
        private List<MqttClient> clients;

        public MqttClientCollection()
        {
            this.clients = new List<MqttClient>();
        }
                
        #region IEnumerable ...

        public IEnumerator GetEnumerator()
        {
            return this.clients.GetEnumerator();
        }

        #endregion

        #region IList<MqttClient> ...

        public int IndexOf(MqttClient item)
        {
            return this.clients.IndexOf(item);
        }

        public void Insert(int index, MqttClient item)
        {
            lock (this.clients)
            {
                this.clients.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (this.clients)
            {
                this.clients.RemoveAt(index);
            }
        }

        public MqttClient this[int index]
        {
            get { return this.clients[index]; }
            set { this.clients[index] = value; }
        }

        public void Add(MqttClient item)
        {
            lock (this.clients)
            {
                this.clients.Add(item);
            }
        }

        public void Clear()
        {
            this.clients.Clear();
        }

        public bool Contains(MqttClient item)
        {
            return this.clients.Contains(item);
        }

        public void CopyTo(MqttClient[] array, int arrayIndex)
        {
            this.clients.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.clients.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(MqttClient item)
        {
            lock (this.clients)
            {
                return this.clients.Remove(item);
            }
        }

        IEnumerator<MqttClient> IEnumerable<MqttClient>.GetEnumerator()
        {
            return this.clients.GetEnumerator();
        }

        #endregion
    }
}
