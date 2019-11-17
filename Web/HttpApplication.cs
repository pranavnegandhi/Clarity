using System;
using System.Diagnostics;
using System.Reflection;

namespace Clarity.Web
{
    public abstract class HttpApplication
    {
        public event EventHandler<EventArgs> BeginRequest;

        public event EventHandler<EventArgs> EndRequest;

        private HttpContext _initContext;

        private HttpContext _context;

        private delegate void ApplicationStart(object sender);

        private static ApplicationStart _applicationStartInvocation;

        private static ApplicationStart NullApplicationStart = (s) => { };

        static HttpApplication()
        {
            _applicationStartInvocation = OnApplicationStart;
        }

        public HttpApplication()
        {
            _applicationStartInvocation(this);
        }

        public virtual void Init()
        {
            OnBeginRequest();
        }

        public HttpContext Context
        {
            get
            {
                if (null != _context)
                {
                    return _context;
                }

                return _initContext;
            }
        }

        public static void OnApplicationStart(object sender)
        {
            var type = sender.GetType();
            var method = type.GetMethod("Application_Start", BindingFlags.Instance | BindingFlags.NonPublic);

            if (null != method)
            {
                try
                {
                    method.Invoke(sender, null);
                }
                catch (TargetException exception)
                {
                    Trace.TraceInformation("Could not invoke Application_Start due to an error in the target class");
                }
                catch (TargetInvocationException exception)
                {
                    Trace.TraceInformation("Could not invoke Application_Start due to an error during the invocation");
                }
                catch (TargetParameterCountException exception)
                {
                    Trace.TraceInformation("The method signature did not match");
                }
                catch (MethodAccessException exception)
                {
                    Trace.TraceInformation("The method could not be invoked due to its protection level");
                }
            }

            _applicationStartInvocation = NullApplicationStart;
        }

        public void OnBeginRequest()
        {
            var handler = BeginRequest;

            if (null == handler)
            {
                return;
            }

            var e = new EventArgs();
            handler(this, e);
        }

        public void OnEndRequest()
        {
            var handler = EndRequest;

            if (null == handler)
            {
                return;
            }

            var e = new EventArgs();
            handler(this, e);
        }
    }
}