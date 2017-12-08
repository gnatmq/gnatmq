using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uPLibrary.Networking.M2Mqtt;

#if TRACE
// alias needed due to Microsoft.SPOT.Trace in .Net Micro Framework
// (it's ambiguos with uPLibrary.Networking.M2Mqtt.Utility.Trace)
using MqttUtility = uPLibrary.Networking.M2Mqtt.Utility;
#endif

namespace GnatMQServer
{
    class Program
    {
        static void Main(string[] args)
        {
#if TRACE
            MqttUtility.Trace.TraceLevel = MqttUtility.TraceLevel.Verbose | MqttUtility.TraceLevel.Frame;
            MqttUtility.Trace.TraceListener = (f, a) => System.Diagnostics.Trace.WriteLine(System.String.Format(f, a));
#endif

            // create and start broker
            MqttBroker broker = new MqttBroker();
            broker.Start();

            Console.ReadLine();

            broker.Stop();
        }
    }
}
