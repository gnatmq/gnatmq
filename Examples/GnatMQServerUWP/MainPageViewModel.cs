using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.IntegrationAPI;
using Windows.ApplicationModel;

namespace GnatMQServerUWP
{
    public class MainPageViewModel
    {
        static MqttBroker broker = new MqttBroker();

        ObservableCollection<ClientModel> Clients = new ObservableCollection<ClientModel>(); 

        static MainPageViewModel(){
            broker = new MqttBroker();
            
            Windows.UI.Xaml.Application.Current.Suspending += PauseBroker;
            Windows.UI.Xaml.Application.Current.Resuming += ResumeBroker;
            ResumeBroker(null, null); 
        }

        private static void ResumeBroker(object sender, object e)
        {
            broker.Start();
            //broker.ClientConnected += (ClientModel)=> { }
        }

        private static void PauseBroker(object sender, SuspendingEventArgs e)
        {
            broker.Stop(); 
        }
    }
}
