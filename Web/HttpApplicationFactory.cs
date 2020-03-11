namespace Clarity.Web
{
    public class HttpApplicationFactory<T> where T : IHttpAsyncHandler, new()
    {
        public IHttpAsyncHandler GetApplicationInstance(HttpContext context)
        {
            IHttpAsyncHandler application = new T();

            return application;
        }
    }
}