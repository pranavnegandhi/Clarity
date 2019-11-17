namespace Clarity.Web
{
    public interface IHttpHandler
    {
        void ProcessRequest(HttpContext context);
    }
}