using messenger.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace messenger.Services
{
    // Класс P2P для обмена сообщениями через TCP
    public class P2PChatService
    {
        private readonly int _port;
        private TcpListener? _listener;
        public event Action<Message>? OnMessageReceived;

        public P2PChatService(int port)
        {
            _port = port;
        }

        // Запуск TCP-сервера для приема сообщений
        public void StartServer()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        var stream = client.GetStream();
                        using var ms = new System.IO.MemoryStream();
                        await stream.CopyToAsync(ms);
                        string json = Encoding.UTF8.GetString(ms.ToArray());
                        try
                        {
                            var msg = JsonSerializer.Deserialize<Message>(json);
                            if (msg != null)
                                OnMessageReceived?.Invoke(msg);
                        }
                        catch
                        {
                            // Ошибка при десериализации сообщения
                        }
                        client.Close();
                    }
                    catch
                    {
                        // Ошибка приема соединения
                    }
                }
            });
        }

        // Отправить сообщение на указанный IP/порт
        public async Task SendMessageAsync(string ip, int port, Message msg)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, port);
            var stream = client.GetStream();
            string json = JsonSerializer.Serialize(msg);
            byte[] data = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(data, 0, data.Length);
            client.Close();
        }
    }
}
