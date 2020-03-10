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

        public HttpContext(WorkerRequest wr)
        {
            _wr = wr;
            var request = new HttpRequest(wr, this);
            var response = new HttpResponse(wr, this);
        }
    }
}