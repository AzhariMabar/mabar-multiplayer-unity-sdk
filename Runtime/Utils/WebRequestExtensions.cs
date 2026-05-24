using System;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace Mabar.Multiplayer.Utils
{
    public static class WebRequestExtensions
    {
        public static WebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation op)
            => new WebRequestAwaiter(op);

        public struct WebRequestAwaiter : INotifyCompletion
        {
            private readonly UnityWebRequestAsyncOperation _op;

            public WebRequestAwaiter(UnityWebRequestAsyncOperation op) { _op = op; }

            public bool IsCompleted => _op.isDone;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
                => _op.completed += _ => continuation();
        }
    }
}
