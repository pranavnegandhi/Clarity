using System;

namespace Clarity.Web
{
    /// <summary>
    /// Encapsulates all HTTP-communication related context required by
    /// the server to process a request. A reference of this instance
    /// is passed to the <code>IHttpModule</code> and <code>IHttpServer</code>
    /// instances at the time of servicing a request.
    /// </summary>
    public sealed class HttpContext
    {
        private readonly WorkerRequest _wr;

        private HttpRequest _request;

        private HttpResponse _response;

        private DateTime _utcTimeStamp;

        public HttpContext(WorkerRequest wr)
        {
            _wr = wr;
            var request = new HttpRequest(wr, this);
            var response = new HttpResponse(wr, this);
            Init(request, response);
            request.Context = this;
            response.Context = this;
        }

        internal DateTime TimeStamp
        {
            get
            {
                return _utcTimeStamp.ToLocalTime();
            }
        }

        internal DateTime UtcTimeStamp
        {
            get
            {
                return _utcTimeStamp;
            }
        }

        internal WorkerRequest WorkerRequest
        {
            get
            {
                return _wr;
            }
        }

        public HttpRequest Request
        {
            get
            {
                return _request;
            }
        }

        public HttpResponse Response
        {
            get
            {
                return _response;
            }
        }

        private void Init(HttpRequest request, HttpResponse response)
        {
            _request = request;
            _response = response;
            _utcTimeStamp = DateTime.UtcNow;
        }
    }
}