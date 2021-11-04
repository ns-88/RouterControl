using RouterControl.Interfaces.Models;

namespace RouterControl.Models
{
    internal class NetworkInterfacesModel : INetworkInterfaces
    {
        public string PppoeInterface { get; }
        public string EtherInterface { get; }

        public NetworkInterfacesModel()
        {
            PppoeInterface = string.Empty;
            EtherInterface = string.Empty;
        }

        public NetworkInterfacesModel(string pppoeInterface, string etherInterface)
        {
            PppoeInterface = pppoeInterface;
            EtherInterface = etherInterface;
        }
    }
}