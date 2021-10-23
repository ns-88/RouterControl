using System;
using System.Security.Cryptography;
using System.Text;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Services;

namespace RouterControl.Services
{
    internal class CredentialService : ICredentialService
    {
        private const string DpapiEntropy = "h3EvqPl91@mftxrkpS!WKc";

        public static byte[] ComputeSha512Hash(string value)
        {
            using var sha512 = SHA512.Create();
            return sha512.ComputeHash(Encoding.UTF8.GetBytes(value));
        }

        public string DecryptPassword(ReadOnlyMemory<byte> cipher)
        {
            var dpapiEntropyHash = ComputeSha512Hash(DpapiEntropy);
            var data = ProtectedData.Unprotect(cipher.ToArray(), dpapiEntropyHash, DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(data);
        }

        public ReadOnlyMemory<byte> EncryptPassword(string password)
        {
            Guard.ThrowIfNull(password, nameof(password));

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var dpapiEntropyHash = ComputeSha512Hash(DpapiEntropy);

            return ProtectedData.Protect(passwordBytes, dpapiEntropyHash, DataProtectionScope.CurrentUser);
        }
    }
}