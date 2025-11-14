using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace messenger.Services
{
    public class UdpDiscoveryService
    {
        public event Action<string>? UserDiscovered;
        private int _port;
        private UdpClient? _listener;

        public UdpDiscoveryService(int port = 8001)
        {
            _port = port;
        }

        public void StartListening()
        {
            _listener = new UdpClient(_port);
            Task.Run(async () =>
            {
                while (true)
                {
                    var result = await _listener.ReceiveAsync();
                    string userName = Encoding.UTF8.GetString(result.Buffer);
                    UserDiscovered?.Invoke(userName);
                }
            });
        }

        public void BroadcastUserName(string userName)
        {
            using var client = new UdpClient();
            client.EnableBroadcast = true;
            byte[] data = Encoding.UTF8.GetBytes(userName);
            client.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, _port));
        }
    }
}
