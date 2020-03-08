using Clarity.Web;
using Serilog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;

namespace Clarity.HttpServer
{
    public class TcpServer
    {
        private string _host = null;

        private int _port = 9091;

        private AsyncCallback _handlerCompletionCallback;

        private Socket _client;

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

                _handlerCompletionCallback = new AsyncCallback(OnHandlerCompletion);

                while (true)
                {
                    _client = await listener.AcceptAsync();

                    var length = await _client.ReceiveAsync(incomingBuffer, SocketFlags.None);
                    message.Append(Encoding.ASCII.GetString(incomingBuffer.Array, 0, length));
                    Log.Verbose(message.ToString());
                    Log.Information("Received {length} bytes from client", length);

                    var app = new AsyncProxy();
                    app.RunAsync(_handlerCompletionCallback);
                }
            }
            catch (SocketException exception)
            {
                Log.Fatal($"Could not start a server at {_host}:{_port}.\r\nError: {exception.Message}");
            }
            catch (SecurityException exception)
            {
                Log.Fatal($"A security violation occurred while starting a server at {_host}:{_port}.\r\nError: {exception.Message}");
            }
        }

        private async void OnHandlerCompletion(IAsyncResult ar)
        {
            Log.Information($"Request processing completed");

            var message = new StringBuilder();

            message.Clear();
            message.Append("HTTP/1.1 200 OK\n\n");
            var length = message.Length;
            var outgoingBuffer = new ArraySegment<byte>(Encoding.ASCII.GetBytes(message.ToString()));

            length = await _client.SendAsync(outgoingBuffer, SocketFlags.None);
            Log.Verbose(message.ToString());
            Log.Information("Sent {length} bytes to client", length);

            _client.Close();
        }
    }

    internal class AsyncProxy
    {
        private AsyncCallback _callback;

        public void RunAsync(AsyncCallback cb)
        {
            _callback = cb;
            var t = new Thread(new ThreadStart(Execute));
            t.Start();
        }

        private void Execute()
        {
            Thread.Sleep(1000);
            _callback.Invoke(null);
        }
    }
}