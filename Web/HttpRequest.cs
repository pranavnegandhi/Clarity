namespace Clarity.Web
{
    /// <summary>
    /// Implements a type-safe object to access data sent from the client
    /// to the server.
    /// </summary>
    public sealed class HttpRequest
    {
        private readonly WorkerRequest _wr;

        private readonly HttpContext _context;

        internal HttpRequest(WorkerRequest wr, HttpContext context)
        {
            _wr = wr;
            _context = context;
        }
    }
}