using System;
using System.Net;
using RouterControl.Infrastructure.Utilities;
using RouterControl.Interfaces.Models;
using RouterControl.Interfaces.Services;

#nullable disable

namespace RouterControl.Models
{
    internal class SettingsModel : IProgramSettings
    {
        private readonly IPEndPoint _routerAddress;
        private readonly INetworkInterfaces _networkInterfaces;
        private readonly ReadOnlyMemory<byte> _userPasswordCipher;
        private readonly UserPasswordHelper _userPasswordHelper;

        public string UserName { get; set; }
        public string RouterIpAddress { get; set; }
        public string RouterPort { get; set; }
        public string PppoeInterface { get; set; }
        public string EthernetInterface { get; set; }
        ReadOnlyMemory<byte> IProgramSettings.UserPassword => _userPasswordCipher;
        IPEndPoint IProgramSettings.RouterAddress => _routerAddress;
        INetworkInterfaces IProgramSettings.NetworkInterfaces => _networkInterfaces;
        public bool IsModelFilled { get; private set; }

        private SettingsModel(string userName,
                              ReadOnlyMemory<byte> userPassword,
                              IPEndPoint routerAddress,
                              INetworkInterfaces networkInterfaces)
        {
            _userPasswordCipher = userPassword;
            _routerAddress = routerAddress;
            _networkInterfaces = networkInterfaces;

            UserName = userName;
        }

        #region UserPassword
        public string UserPassword
        {
            get => _userPasswordHelper.GetValue();
            set => _userPasswordHelper.SetValue(value);
        }
        #endregion

        public SettingsModel(ICredentialService credentialService)
        {
            _userPasswordHelper = new UserPasswordHelper(credentialService);
        }

        public void FromEntity(IProgramSettings settings)
        {
            try
            {
                Guard.ThrowIfNull(settings, nameof(settings));
                Guard.ThrowIfNull(settings.RouterAddress, nameof(settings.RouterAddress));
                Guard.ThrowIfNull(settings.NetworkInterfaces, nameof(settings.NetworkInterfaces));

                _userPasswordHelper.Initialization(settings.UserPassword);

                UserName = settings.UserName;
                RouterIpAddress = settings.RouterAddress.Address.ToString();
                RouterPort = settings.RouterAddress.Port.ToString();
                PppoeInterface = settings.NetworkInterfaces.PppoeInterface;
                EthernetInterface = settings.NetworkInterfaces.EtherInterface;
            }
            catch
            {
                IsModelFilled = false;
                throw;
            }

            IsModelFilled = true;
        }

        public IProgramSettings ToEntity()
        {
            var routerAddress = new IPEndPoint(IPAddress.Parse(RouterIpAddress), int.Parse(RouterPort));
            var networkInterfaces = new NetworkInterfacesModel(PppoeInterface, EthernetInterface);
            var userPassword = _userPasswordHelper.GetCipher();

            return new SettingsModel(UserName, userPassword, routerAddress, networkInterfaces);
        }

        public bool CheckModel()
        {
            return !(string.IsNullOrWhiteSpace(UserName) ||
                     string.IsNullOrWhiteSpace(UserPassword) ||
                     string.IsNullOrWhiteSpace(RouterIpAddress) ||
                     !IPAddress.TryParse(RouterIpAddress, out _) ||
                     string.IsNullOrWhiteSpace(RouterPort) ||
                     !int.TryParse(RouterPort, out _) ||
                     string.IsNullOrWhiteSpace(PppoeInterface) ||
                     string.IsNullOrWhiteSpace(EthernetInterface));
        }

        #region Nested types

        private class UserPasswordHelper
        {
            private const string FakePassword = "FakePassword";
            private readonly ICredentialService _credentialService;
            private ReadOnlyMemory<byte> _cipher;
            private string _value;

            public UserPasswordHelper(ICredentialService credentialService)
            {
                Guard.ThrowIfNull(credentialService, out _credentialService, nameof(credentialService));

                _credentialService = credentialService;
                _cipher = ReadOnlyMemory<byte>.Empty;
                _value = string.Empty;
            }

            public void Initialization(ReadOnlyMemory<byte> cipher)
            {
                _cipher = cipher;

                if (!_cipher.IsEmpty)
                    _value = FakePassword;
            }

            public ReadOnlyMemory<byte> GetCipher()
            {
                return string.IsNullOrWhiteSpace(_value) || _value.Equals(FakePassword, StringComparison.Ordinal)
                    ? _cipher
                    : _credentialService.EncryptPassword(_value);
            }

            public void SetValue(string value)
            {
                _value = value;
            }

            public string GetValue()
            {
                return string.IsNullOrWhiteSpace(_value)
                    ? _value
                    : FakePassword;
            }
        }

        #endregion
    }
}