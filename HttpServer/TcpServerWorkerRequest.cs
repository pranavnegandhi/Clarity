using System;
using System.Collections;
using System.Text;

namespace Clarity.HttpServer
{
    internal class TcpServerWorkerRequest
    {
        private const string HttpVersion = "HTTP/1.1 ";

        private byte[] _segment;

        private StringBuilder _sendStatus = new StringBuilder();

        private bool _statusSet = false;

        private StringBuilder _headers = new StringBuilder();

        private bool _headersSent = false;

        private byte[] _responseStatus;

        private byte[] _responseHeaders;

        private byte[] _responseBody;

        public delegate void SendResponseDelegate(ArraySegment<byte> response);

        public SendResponseDelegate SendResponse = null;

        internal TcpServerWorkerRequest(byte[] segment)
        {
            _segment = new byte[segment.Length];
            segment.CopyTo(_segment, 0);
        }

        public void SendStatus(int statusCode, string statusDescription)
        {
            _sendStatus.Clear();
            _sendStatus.Append(statusCode.ToString());
            _sendStatus.Append(" ");
            _sendStatus.Append(statusDescription);
            _sendStatus.Append("\n");
            _statusSet = true;
        }

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

        private void AddBodyToResponse(byte[] body)
        {
            if (null == _responseBody)
            {
                _responseBody = new byte[body.Length];
            }

            body.CopyTo(_responseBody, 0);
        }

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

            return new TcpServerWorkerRequest(segment);
        }
    }
}