using Clarity.Web;
using Serilog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;

namespace Clarity.HttpServer
{
    public class TcpServer
    {
        private string _host = null;

        private int _port = 9091;

        private Type _applicationType;

        private HttpApplication _application;

        public TcpServer(string host, int port)
        {
            if (null == host)
            {
                throw new ArgumentNullException($"Parameter {nameof(host)} cannot be null");
            }

            _host = host;
            _port = port;
        }

        public async void Start<T>(HttpApplicationFactory<T> factory) where T : HttpApplication, new()
        {
            try
            {
                var entries = await Dns.GetHostEntryAsync(_host);
                if (0 == entries.AddressList.Length)
                {
                    throw new Exception($"Could not bind to address on host {_host}");
                }

                IPAddress address = null;
                foreach (var a in entries.AddressList)
                {
                    if (AddressFamily.InterNetwork == a.AddressFamily)
                    {
                        address = a;
                        break;
                    }
                }

                var localEndPoint = new IPEndPoint(address, _port);

                var listener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(localEndPoint);
                listener.Listen(10);

                var incomingBuffer = new ArraySegment<byte>(new byte[1024]);
                var message = new StringBuilder();

                while (true)
                {
                    var client = await listener.AcceptAsync();

                    var application = factory.Create();

                    var length = await client.ReceiveAsync(incomingBuffer, SocketFlags.None);
                    message.Append(Encoding.ASCII.GetString(incomingBuffer.Array, 0, length));
                    Log.Verbose(message.ToString());
                    Log.Information("Received {length} bytes from client", length);

                    message.Clear();
                    message.Append("HTTP/1.1 200 OK\n\n");
                    length = message.Length;
                    var outgoingBuffer = new ArraySegment<byte>(Encoding.ASCII.GetBytes(message.ToString()));

                    length = await client.SendAsync(outgoingBuffer, SocketFlags.None);
                    Log.Verbose(message.ToString());
                    Log.Information("Sent {length} bytes to client", length);

                    client.Close();
                }
            }
            catch (SocketException exception)
            {
                Console.WriteLine($"Could not start a server at {_host}:{_port}.\r\nError: {exception.Message}");
            }
            catch (SecurityException exception)
            {
                Console.WriteLine($"A security violation occurred while starting a server at {_host}:{_port}.\r\nError: {exception.Message}");
            }
        }
    }
}