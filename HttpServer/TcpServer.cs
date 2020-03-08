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

                    var wr = TcpServerWorkerRequest.CreateWorkerRequest(incomingBuffer.Array);
                    ProcessRequest(wr);
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

        private void ProcessRequest(TcpServerWorkerRequest wr)
        {
            try
            {
                var app = new AsyncProxy();
                app.BeginProcessRequest(wr, _handlerCompletionCallback);
            }
            catch
            {
                try
                {
                    // Send a bad request response if the context cannot
                    // be created for any reason.
                    wr.SendStatus(400, "Bad Request");
                    wr.SendKnownResponseHeader("Content-Type", "text/html; charset=utf8");
                    var body = Encoding.ASCII.GetBytes("<html><body>Bad Request</body></html>");
                    wr.SendResponseFromMemory(body, body.Length);
                    var response = wr.FlushResponse();
                    SendResponse(response);

                    wr.EndOfRequest();
                }
                finally
                {
                }
            }
        }

        private void OnHandlerCompletion(IAsyncResult ar)
        {
            Log.Information($"Request processing completed");

            var wr = (TcpServerWorkerRequest)ar.AsyncState;
            FinishRequest(wr);
        }

        private async void SendResponse(ArraySegment<byte> response)
        {
            var length = await _client.SendAsync(response, SocketFlags.None);
            Log.Information("Sent {length} bytes to client", length);

            _client.Close();
        }

        private void FinishRequest(TcpServerWorkerRequest wr)
        {
            wr.SendStatus(200, "OK");
            wr.SendKnownResponseHeader("Content-Type", "text/html; charset=utf8");
            var body = Encoding.ASCII.GetBytes("<html><body>Hello, world</body></html>");
            wr.SendResponseFromMemory(body, body.Length);
            var response = wr.FlushResponse();
            SendResponse(response);

            wr.EndOfRequest();
        }
    }

    internal class AsyncProxy
    {
        public void BeginProcessRequest(TcpServerWorkerRequest wr, AsyncCallback cb)
        {
            Result = new AsyncResult(cb, wr);

            var t = new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(1000);
                cb.Invoke(Result);
            }));

            t.Start();
        }

        internal IAsyncResult Result
        {
            get;
            private set;
        }
    }

    internal class AsyncResult : IAsyncResult
    {
        private AsyncCallback _callback;

        internal AsyncResult(AsyncCallback cb, object state)
        {
            _callback = cb;
            AsyncState = state;
        }

        public object AsyncState
        {
            get;
            private set;
        }

        public WaitHandle AsyncWaitHandle => null;

        public bool CompletedSynchronously => false;

        public bool IsCompleted
        {
            get;
            private set;
        }
    }
}