using System;
using System.Text;
using Clarity.Web;

namespace Clarity.HttpServer
{
    /// <summary>
    /// Holds deserialized instances of incoming and outgoing messages
    /// in a byte array.
    /// 
    /// Analogous to the ISAPIWorkerRequest used in the ASP.NET pipeline.
    /// </summary>
    internal class TcpServerWorkerRequest : WorkerRequest
    {
        /// <summary>
        /// All requests are always HTTP/1.1 in this implementation.
        /// </summary>
        private const string HttpVersion = "HTTP/1.1 ";

        /// <summary>
        /// Stores the incoming request as a byte array.
        /// </summary>
        private byte[] _segment;

        private ArraySegment<byte> _methodSegment;

        /// <summary>
        /// The ISAPIWorkerRequest class uses a custom class called
        /// MemoryBytes to store many of its internal data structures
        /// due to speed and memory efficiency.
        /// 
        /// For our purposes, a StringBuilder suffices.
        /// </summary>
        /// <returns></returns>
        private StringBuilder _sendStatus = new StringBuilder();

        /// <summary>
        /// Boolean that's switched on when the value of the response status
        /// is changed by the server.
        /// </summary>
        private bool _statusSet = false;

        /// <summary>
        /// Stores the headers in a StringBuilder to be serialized
        /// into a string at the time of dispatching to the client.
        /// </summary>
        /// <returns></returns>
        private StringBuilder _headers = new StringBuilder();

        /// <summary>
        /// Indicates if the headers have been dispatched to the client.
        /// Headers can be sent only once in a single request-response cycle.
        /// </summary>
        private bool _headersSent = false;

        /// <summary>
        /// Values of the response status, headers and body serialized into a byte array.
        /// </summary>
        private byte[] _responseStatus;

        private byte[] _responseHeaders;

        private byte[] _responseBody;

        /// <summary>
        /// Constructor for the TcpServerWorkerRequest class. Not invoked
        /// directly, but through the CreateWorkerRequest static method.
        /// </summary>
        /// <param name="segment">The request serialized into a byte array.</param>
        internal TcpServerWorkerRequest(byte[] segment)
        {
            _segment = new byte[segment.Length];
            segment.CopyTo(_segment, 0);
        }

        internal void Initialize()
        {
            // Walk the _segment until the first space character is encountered
            var i = 0;
            while (_segment[i++] != 32)
            {
            }

            _methodSegment = new ArraySegment<byte>(_segment, 0, i);
        }

        /// <summary>
        /// Sets the status code of the response and saves it in a field.
        /// </summary>
        /// <param name="statusCode">HTTP status code from the standard.</param>
        /// <param name="statusDescription">A text description of the status.</param>
        public void SendStatus(int statusCode, string statusDescription)
        {
            _sendStatus.Clear();
            _sendStatus.Append(statusCode.ToString());
            _sendStatus.Append(" ");
            _sendStatus.Append(statusDescription);
            _sendStatus.Append("\n");
            _statusSet = true;
        }

        /// <summary>
        /// Appends a header key-value pair as a string to the response header.
        /// </summary>
        /// <param name="header">The name of the header.</param>
        /// <param name="value">The value of the header.</param>
        public void SendKnownResponseHeader(string header, string value)
        {
            if (_headersSent)
            {
                throw new InvalidOperationException("Cannot send headers after headers have been sent.");
            }

            _headers.Append(header);
            _headers.Append(": ");
            _headers.Append(value);
            _headers.Append("\r\n");
        }

        /// <summary>
        /// Sends a programmatically generated response (as opposed to reading a file or stream).
        /// </summary>
        /// <param name="data">The data to be sent to the client in the body of the response.</param>
        /// <param name="length">The length of the body.</param>
        public void SendResponseFromMemory(byte[] data, int length)
        {
            if (!_headersSent)
            {
                SendHeaders();
            }

            if (length > 0)
            {
                AddBodyToResponse(data);
            }
        }

        /// <summary>
        /// Serializes the response status and headers into a byte array.
        /// </summary>
        private void SendHeaders()
        {
            if (!_headersSent)
            {
                if (_statusSet)
                {
                    _headers.Append("\r\n");

                    var status = _sendStatus.ToString();
                    var headers = _headers.ToString();

                    _responseStatus = Encoding.UTF8.GetBytes(status);
                    _responseHeaders = Encoding.UTF8.GetBytes(headers);

                    _headersSent = true;
                }
            }
        }

        /// <summary>
        /// Makes a copy of the byte array containing the body of the response into an internal field.
        /// </summary>
        /// <param name="body"></param>
        private void AddBodyToResponse(byte[] body)
        {
            if (null == _responseBody)
            {
                _responseBody = new byte[body.Length];
            }

            body.CopyTo(_responseBody, 0);
        }

        /// <summary>
        /// Concatenates all the different fragments of the response (HTTP version identifier,
        /// status, headers and body) into a single byte array and returns it to the caller
        /// as an <code>ArraySegment&lt;byte&gt;</code>.
        /// </summary>
        /// <returns></returns>
        public ArraySegment<byte> FlushResponse()
        {
            if (!_headersSent)
            {
                SendHeaders();
            }

            var versionBytes = Encoding.UTF8.GetBytes(HttpVersion);
            var offset = 0;
            var numFragments = versionBytes.Length + _responseStatus.Length + _responseHeaders.Length + _responseBody.Length;
            var response = new byte[numFragments];

            versionBytes.CopyTo(response, offset);
            offset += versionBytes.Length;

            _responseStatus.CopyTo(response, offset);
            offset += _responseStatus.Length;

            _responseHeaders.CopyTo(response, offset);
            offset += _responseHeaders.Length;

            _responseBody.CopyTo(response, offset);

            var fragments = new ArraySegment<byte>(response);
            var temp = Encoding.UTF8.GetString(response);
            System.Diagnostics.Trace.TraceInformation(temp);

            return fragments;
        }

        /// <summary>
        /// Cleans up the cached fragments of the response and resets the flags
        /// in preparation for cleanup of this instance.
        /// </summary>
        public void EndOfRequest()
        {
            if (null != _headers)
            {
                _headers = null;
            }

            if (null != _sendStatus)
            {
                _sendStatus = null;
            }
        }

        /// <summary>
        /// Factory to create instances of the <code>TcpServerWorkerRequest</code> class.
        /// </summary>
        /// <param name="segment">An serialized byte array of the incoming request.</param>
        /// <returns></returns>
        internal static TcpServerWorkerRequest CreateWorkerRequest(byte[] segment)
        {
            if (null == segment)
            {
                throw new ArgumentNullException(nameof(segment));
            }

            if (0 == segment.Length)
            {
                throw new ArgumentException(nameof(segment.Length));
            }

            var wr = new TcpServerWorkerRequest(segment);
            if (null != wr)
            {
                wr.Initialize();
            }

            return wr;
        }
    }
}