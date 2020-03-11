using System;
using System.Threading;

namespace Clarity.Web
{
    internal class HttpAsyncResult : IAsyncResult
    {
        private AsyncCallback _callback;

        private Object _asyncState;

        private bool _completed;

        private bool _completedSynchronously;

        private Object _result;

        private Exception _error;

        private Thread _threadWhichStartedOperation;

        internal HttpAsyncResult(AsyncCallback cb, Object state)
        {
            _callback = cb;
            _asyncState = state;
        }

        internal HttpAsyncResult(AsyncCallback cb, Object state, bool completed, Object result, Exception error)
        {
            _callback = cb;
            _asyncState = state;
            _completed = completed;
            _completedSynchronously = completed;
            _result = result;
            _error = error;

            if (_completed && _callback != null)
            {
                _callback(this);
            }
        }

        internal void SetComplete()
        {
            _completed = true;
        }

        internal void Complete(bool synchronous, Object result, Exception error)
        {
            if (Volatile.Read(ref _threadWhichStartedOperation) == Thread.CurrentThread)
            {
                synchronous = true;
            }

            _completed = true;
            _completedSynchronously = synchronous;
            _result = result;
            _error = error;

            if (_callback != null)
            {
                _callback(this);
            }
        }

        internal Object End()
        {
            return _result;
        }

        internal void MarkCallToBeginMethodStarted()
        {
            var originalThread = Interlocked.CompareExchange(ref _threadWhichStartedOperation, Thread.CurrentThread, null);
        }

        internal void MarkCallToBeginMethodCompleted()
        {
            var originalThread = Interlocked.Exchange(ref _threadWhichStartedOperation, null);
        }

        internal Exception Error
        {
            get
            {
                return _error;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return _completed;
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return _completedSynchronously;
            }
        }

        public Object AsyncState
        {
            get
            {
                return _asyncState;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                return null;
            }
        }
    }
}