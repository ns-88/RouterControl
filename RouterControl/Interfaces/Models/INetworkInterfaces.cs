namespace RouterControl.Interfaces.Models
{
    internal interface INetworkInterfaces
    {
        string PppoeInterface { get; }
        string EtherInterface { get; }
    }
}