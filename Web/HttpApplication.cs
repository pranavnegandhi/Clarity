using Serilog;
using System;
using System.Reflection;

namespace Clarity.Web
{
    public abstract class HttpApplication
    {
        public event EventHandler<EventArgs> BeginRequest;

        public event EventHandler<EventArgs> EndRequest;

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
                    Log.Warning("Could not invoke Application_Start due to an error in the target class {message}", exception.Message);
                }
                catch (TargetInvocationException exception)
                {
                    Log.Error("Could not invoke Application_Start due to an error during the invocation {message}", exception.Message);
                }
                catch (TargetParameterCountException exception)
                {
                    Log.Error("The method signature did not match {message}", exception.Message);
                }
                catch (MethodAccessException exception)
                {
                    Log.Warning("The method could not be invoked due to its protection level {message}", exception.Message);
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