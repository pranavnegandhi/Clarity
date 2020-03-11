using Clarity.Web;
using Serilog;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clarity.HttpServer
{
    /// <summary>
    /// Receives incoming requests over a binary socket and
    /// initiates their processing through the rest of the
    /// engine.
    /// 
    /// Superficially analoguous to the IISAPI module and
    /// the HttpRuntime class in the ASP.NET pipeline.
    /// </summary>
    public class TcpServer
    {
        /// <summary>
        /// The name of the server where the connection will be exposed from.
        /// </summary>
        private string _host = null;

        /// <summary>
        /// The number of the listening port.
        /// </summary>
        private int _port = 9091;

        /// <summary>
        /// A reference to the socket connection established with the client.
        /// As a toy server, this application can only handle a single client.
        /// A production-grade application should use a pool of these connection
        /// objects (which are also handled by the web server itself, rather than
        /// the framework pipeline).
        /// </summary>
        private Socket _client;

        /// <summary>
        /// A delegate to the method in this class that should be
        /// invoked after the processing engine completes its task.
        /// </summary>
        private AsyncCallback _handlerCompletionCallback;

        /// <summary>
        /// Constructor to instantiate the engine. Called from the Main() method
        /// of the host process.
        /// </summary>
        /// <param name="host">The name of the server to which the client makes a connection request.</param>
        /// <param name="port">The port number on which an incoming connection request is made.</param>
        public TcpServer(string host, int port)
        {
            if (null == host)
            {
                throw new ArgumentNullException($"Parameter {nameof(host)} cannot be null");
            }

            _host = host;
            _port = port;
        }

        /// <summary>
        /// Initiates the incoming connection socket for the network server
        /// and initialises an instance of the HttpApplication.
        /// </summary>
        /// <param name="factory">The factory class that returns an instance of the HttpApplication.</param>
        /// <typeparam name="T">The concrete type of the HttpApplication instance.</typeparam>
        /// <returns></returns>
        public async void Start<T>(HttpApplicationFactory<T> factory) where T : HttpApplication, new()
        {
            try
            {
                // Find an IPv4 address on the host computer and attempt to establish
                // an incoming network socket over it on the specified port number.
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

                // Go into an infinite loop and wait for incoming
                // connections. When a connection is made, the application
                // receives the message sent by the client and pumps it
                // into the processing pipeline.
                while (true)
                {
                    _client = await listener.AcceptAsync();

                    var length = await _client.ReceiveAsync(incomingBuffer, SocketFlags.None);
                    message.Append(Encoding.ASCII.GetString(incomingBuffer.Array, 0, length));
                    Log.Verbose(message.ToString());
                    Log.Information("Received {length} bytes from client", length);

                    // Deserialize the incoming message buffer into a TcpServerWorkerRequest instance
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

        /// <summary>
        /// Begin processing the request by dispatching a <code>BeginProcessRequest</code>
        /// invocation to an instance of an IHttpHandler class. If the processing fails
        /// for any reason, the server responds with HTTP 400 Bad Request.
        /// </summary>
        /// <param name="wr"></param>
        private void ProcessRequest(TcpServerWorkerRequest wr)
        {
            try
            {
                var context = new HttpContext(wr);
                var app = new AsyncProxy();
                app.BeginProcessRequest(context, _handlerCompletionCallback, wr);
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

        /// <summary>
        /// Invoked by the IHttpApplication instance after it completes the
        /// request processing cycle. The request is serialized back into a
        /// byte array and dispatched back to the client.
        /// </summary>
        /// <param name="ar"></param>
        private void OnHandlerCompletion(IAsyncResult ar)
        {
            Log.Information($"Request processing completed");

            var wr = (TcpServerWorkerRequest)ar.AsyncState;
            FinishRequest(wr);
        }

        /// <summary>
        /// Sends the contents of the serialized message to the listening
        /// client over an open socket connection and closes it.
        /// 
        /// This is a proxy for the MgdFlushCore method in the IIS library
        /// that's used by the ASP.NET Framework.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private async void SendResponse(ArraySegment<byte> response)
        {
            var length = await _client.SendAsync(response, SocketFlags.None);
            Log.Information("Sent {length} bytes to client", length);

            _client.Close();
        }

        /// <summary>
        /// Completes the request processing and initiates the process of
        /// sending a response back to the client.
        /// </summary>
        /// <param name="wr"></param>
        private void FinishRequest(TcpServerWorkerRequest wr)
        {
            // The following lines are a temporary stand-in while the rest
            // of the application processing capabilities are being written.
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
        public void BeginProcessRequest(HttpContext context, AsyncCallback cb, WorkerRequest wr)
        {
            var task = new Task((ar) => Task.Delay(1000).GetAwaiter().GetResult(), wr);
            task.RunSynchronously();

            cb.Invoke(task);
        }
    }
}