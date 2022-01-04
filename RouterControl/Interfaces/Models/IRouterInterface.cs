namespace RouterControl.Interfaces.Models
{
    internal interface IRouterInterface
    {
        bool IsEnabled { get; }

        string Name { get; }

        string ClientMacAddress { get; }

        string ClientName { get; }

        string ClientIpAddress { get; }
    }
}