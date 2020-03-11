using System;

namespace Clarity.Web
{
    public interface IHttpAsyncHandler
    {
        IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, Object extraData);

        void EndProcessRequest(IAsyncResult result);
    }
}