using System;
using System.Threading.Tasks;

namespace Clarity.Web
{
    public class HttpApplication : IHttpAsyncHandler
    {
        private HttpContext _context;

        private AsyncCallback _callback;

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            HttpAsyncResult result;

            _context = context;
            _callback = cb;
            result = new HttpAsyncResult(cb, extraData);
            AsyncResult = result;

            var task = new Task((ar) =>
            {
                Task.Delay(1000).GetAwaiter().GetResult();
                EndProcessRequest(result);
            }, extraData);

            task.RunSynchronously();

            return result;
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            _callback.Invoke(result);
        }

        internal HttpAsyncResult AsyncResult
        {
            get;
            set;
        }
    }
}