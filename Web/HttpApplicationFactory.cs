namespace Clarity.Web
{
    public class HttpApplicationFactory<T> where T : HttpApplication, new()
    {
        public HttpApplication Create()
        {
            var application = new T();

            return application;
        }
    }
}