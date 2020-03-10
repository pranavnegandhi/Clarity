namespace Clarity.Web
{
    /// <summary>
    /// Implements a type-safe object to set values to dispatch from
    /// the server to the client.
    /// </summary>
    public sealed class HttpResponse
    {
        private readonly WorkerRequest _wr;

        private readonly HttpContext _context;

        public HttpResponse(WorkerRequest wr, HttpContext context)
        {
            _wr = wr;
            _context = context;
        }
    }
}