using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace uPLibrary.Networking.M2Mqtt.IntegrationAPI
{
    /// <summary>
    /// POCO used to expose all the data contained to an external system
    /// </summary>
    public struct ClientModel
    {
        public readonly bool CleanSession;
        public readonly string ClientId;
        public readonly MqttProtocolVersion ProtocolVersion;
        public readonly bool Isconnected;

#if !(WINDOWS_APP || WINDOWS_PHONE_APP)
        public readonly IPEndPoint RemoteEndpoint;
#endif

        public readonly string LastWillMessage;
        public readonly bool LastWillFlag;
        public readonly string LastWillTopic;
        public readonly byte LastWillQosLevel;

        public ClientModel(MqttClient client)
        {
            this.CleanSession = client.CleanSession;
            this.ClientId = client.ClientId;
            this.ProtocolVersion = client.ProtocolVersion;
            this.Isconnected = client.IsConnected;
            this.LastWillFlag = client.WillFlag;
            this.LastWillMessage = client.WillMessage;
            this.LastWillTopic = client.WillTopic;
            this.LastWillQosLevel = client.WillQosLevel;

#if !(WINDOWS_APP || WINDOWS_PHONE_APP)
            this.RemoteEndpoint = new IPEndPoint(client.RemoteEndPoint.Address, client.RemoteEndPoint.Port);
#endif
        }
    }
}
