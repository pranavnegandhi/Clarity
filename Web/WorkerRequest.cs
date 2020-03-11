namespace Clarity.Web
{
    /// <summary>
    /// An abstract class to represent a request in the <code>Clarity.Web</code> namespace
    /// that is used by the <code>IHttpModule</code> and <code>IHttpServer</code> types without adding
    /// a reference to the <code>Clarity.HttpServer</code> namespace into this assembly.
    /// </summary>
    public abstract class WorkerRequest
    {
        public abstract string GetHttpVerbName();
    }
}