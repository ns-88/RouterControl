using System;
using System.Net;

namespace RouterControl.Interfaces.Models
{
    internal interface IProgramSettings
    {
        string UserName { get; }
        ReadOnlyMemory<byte> UserPassword { get; }
        IPEndPoint RouterAddress { get; }
        INetworkInterfaces NetworkInterfaces { get; }
        bool IsApplicationAutorun { get; }
    }
}