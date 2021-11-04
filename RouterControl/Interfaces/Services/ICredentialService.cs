using System;

namespace RouterControl.Interfaces.Services
{
    internal interface IReadOnlyCredentialService
    {
        string DecryptPassword(ReadOnlyMemory<byte> cipher);
    }

    internal interface ICredentialService : IReadOnlyCredentialService
    {
        ReadOnlyMemory<byte> EncryptPassword(string password);
    }
}