using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HeartBeat
{
    public class UdpSender
    {
        private static readonly UdpClient _client = new UdpClient();

        public static void SendMessage(string message, string ipAddress, int port)
        {
            try
            {
                var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                _client.Send(bytes, bytes.Length, endPoint);
                Console.WriteLine($"Message sent to {ipAddress}:{port}");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to send message to {ipAddress}:{port}. Error: {ex.Message}");
            }
        }
    }
}
