namespace Clarity.Web
{
    /// <summary>
    /// Implements a type-safe object to access data sent from the client
    /// to the server.
    /// </summary>
    public sealed class HttpRequest
    {
        private readonly WorkerRequest _wr;

        private HttpContext _context;

        private string _httpMethod;

        internal HttpRequest(WorkerRequest wr, HttpContext context)
        {
            _wr = wr;
            _context = context;
        }

        internal HttpContext Context
        {
            get
            {
                return _context;
            }
            set
            {
                _context = value;
            }
        }

        internal HttpResponse Response
        {
            get
            {
                if (null == _context)
                {
                    return null;
                }

                return _context.Response;
            }
        }

        public string HttpMethod
        {
            get
            {
                // Directly from worker request
                if (_httpMethod == null)
                {
                    _httpMethod = _wr.GetHttpVerbName();
                }

                return _httpMethod;
            }
        }

    }
}