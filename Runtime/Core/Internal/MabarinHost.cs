using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mabar.Multiplayer.Core.Internal
{
    // Shared MonoBehaviour: routes main-thread actions and runs coroutines
    // for UnityWebRequest (which must be driven from a MonoBehaviour).
    internal class MabarinHost : MonoBehaviour
    {
        private static MabarinHost _instance;
        private static readonly Queue<Action> _queue = new();
        private static readonly object _lock = new();

        private static MabarinHost Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[Mabarin]") { hideFlags = HideFlags.HideAndDontSave };
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<MabarinHost>();
                }
                return _instance;
            }
        }

        // Queue an action to execute on the Unity main thread (next Update).
        internal static void Dispatch(Action action)
        {
            lock (_lock) _queue.Enqueue(action);
            _ = Instance; // ensure host exists
        }

        // Run a coroutine (for UnityWebRequest).
        internal static void Run(IEnumerator coroutine) => Instance.StartCoroutine(coroutine);

        private void Update()
        {
            while (true)
            {
                Action action;
                lock (_lock)
                {
                    if (_queue.Count == 0) break;
                    action = _queue.Dequeue();
                }
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
