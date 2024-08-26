using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HeartBeat
{
    public class UdpReceiver : IDisposable
    {
        private readonly UdpClient _client;  
        private readonly int _port;

        public UdpReceiver(int port)
        {
            _port = port;
            try
            {
                _client = new UdpClient(_port); 
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to bind to port {_port}. Error: {ex.Message}");
                throw; 
            }
        }

        public string ReceiveMessage()
        {
            try
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, _port);
                byte[] bytes = _client.Receive(ref remoteEndPoint);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            _client?.Close();
            _client?.Dispose();
        }
    }
}
