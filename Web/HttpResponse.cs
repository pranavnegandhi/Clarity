namespace Clarity.Web
{
    /// <summary>
    /// Implements a type-safe object to set values to dispatch from
    /// the server to the client.
    /// </summary>
    public sealed class HttpResponse
    {
        private readonly WorkerRequest _wr;

        private HttpContext _context;

        public HttpResponse(WorkerRequest wr, HttpContext context)
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
    }
}