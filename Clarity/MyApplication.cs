using Clarity.Web;
using Serilog;

namespace Clarity
{
    public class MyApplication : HttpApplication
    {
        protected void Application_Start()
        {
            Log.Information("Application_Start fired in {GetType}", GetType());
        }
    }
}