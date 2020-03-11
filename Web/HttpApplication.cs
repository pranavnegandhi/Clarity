using System;

namespace Clarity.Web
{
    public class HttpApplication : IHttpAsyncHandler
    {
        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            throw new NotImplementedException();
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            throw new NotImplementedException();
        }
    }
}