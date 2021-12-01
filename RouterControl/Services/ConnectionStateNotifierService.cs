using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using RouterControl.Interfaces.Services;

namespace RouterControl.Services
{
    internal class ConnectionStateNotifierService : IConnectionStateNotifierService
    {
        private readonly Ping _ping;
        private readonly IPAddress _address;
        private bool? _state;
        public event EventHandler<bool>? StateChanged;

        public ConnectionStateNotifierService()
        {
            _ping = new Ping();
            _address = new IPAddress(134744072); //8.8.8.8
            _state = null;
        }

        private void RaiseStateChanged(bool state)
        {
            Volatile.Read(ref StateChanged)?.Invoke(this, state);
        }

        public async Task StartNotificationsAsync(CancellationToken token)
        {
            var timeout = TimeSpan.FromSeconds(1);

            while (true)
            {
                try
                {
                    await Task.Delay(timeout, token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {

                }

                if (token.IsCancellationRequested)
                    return;

                PingReply state;

                try
                {
                    state = await _ping.SendPingAsync(_address).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Команда проверки доступности IP-адреса не была выполнена.\r\nОшибка: {ex.Message}", ex);
                }

                var currentState = state.Status == IPStatus.Success;

                if (_state == null)
                {
                    RaiseStateChanged(currentState);
                }
                else
                {
                    if (currentState)
                    {
                        if (!_state.Value)
                            RaiseStateChanged(currentState);
                    }
                    else
                    {
                        if (_state.Value)
                            RaiseStateChanged(currentState);
                    }
                }

                _state = currentState;
            }
        }

        public async Task<string> GetRemoteIpAddressAsync(CancellationToken token)
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            try
            {
                var ip = await httpClient.GetStringAsync("https://api.my-ip.io/ip", token).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(ip))
                    throw new InvalidOperationException("Значение не получено - пустая строка.");

                if (!IPAddress.TryParse(ip, out _))
                    throw new InvalidOperationException($"Неверный формат. Полученное значение: \"{ip}\".");

                return ip;
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"IP-адрес не был получен.\r\nОшибка: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (_ping != null!)
                _ping.Dispose();
        }
    }
}