using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

#if MABAR_COLYSEUS
using System.Collections.Generic;
using Colyseus;
using UnityEngine;
#endif

namespace Mabar.Multiplayer.Core
{
    /// <summary>
    /// MabarinRoom — client-side WebSocket room.
    ///
    /// Requires Colyseus Unity SDK: Package Manager → + → Add package from git URL
    ///   https://github.com/colyseus/colyseus-unity-sdk.git#upm
    ///
    /// Usage:
    ///   room.On&lt;JObject&gt;("event", data =&gt; Debug.Log(data));
    ///   await room.Send("end_turn", new { board = myBoard });
    ///   room.Leave();
    /// </summary>
    public class MabarinRoom
    {
#if MABAR_COLYSEUS
        private readonly ColyseusRoom<object> _room;
        private readonly Dictionary<string, List<Action<JToken>>> _handlers = new();

        public string RoomId            => _room.RoomId;
        public string SessionId         => _room.SessionId;
        public string ReconnectionToken => _room.ReconnectionToken;

        internal MabarinRoom(ColyseusRoom<object> room)
        {
            _room = room;

            _room.OnMessage<JToken>("*", (data) =>
            {
                var type    = data["type"]?.ToString();
                var payload = data["message"];
                if (type != null) Emit(type, payload);
            });

            _room.OnLeave += (code) =>
                Emit("disconnected", JToken.FromObject(new { code }));

            _room.OnError += (code, message) =>
            {
                Debug.LogWarning($"[Mabarin] Room error {code}: {message}");
                Emit("error", JToken.FromObject(new { code, message }));
            };
        }

        /// <summary>Register a handler for a server event (raw JToken payload).</summary>
        public MabarinRoom On(string type, Action<JToken> handler)
        {
            if (!_handlers.ContainsKey(type)) _handlers[type] = new List<Action<JToken>>();
            _handlers[type].Add(handler);
            return this;
        }

        /// <summary>Register a typed handler — deserializes message to T automatically.</summary>
        public MabarinRoom On<T>(string type, Action<T> handler)
        {
            _room.OnMessage<T>(type, handler);
            return this;
        }

        /// <summary>Remove all handlers for an event type.</summary>
        public MabarinRoom Off(string type) { _handlers.Remove(type); return this; }

        /// <summary>Send a message to the server.</summary>
        public async Task Send(string type, object data = null)
            => await _room.Send(type, data ?? new { });

        /// <summary>Leave the room.</summary>
        public async Task Leave() => await _room.Leave();

        private void Emit(string type, JToken data)
        {
            if (!_handlers.TryGetValue(type, out var list)) return;
            foreach (var h in list)
            {
                try   { h(data); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

#else
        // ── Stub: Colyseus SDK not installed ───────────────────────────────────
        // Install via Package Manager → + → Add package from git URL:
        //   https://github.com/colyseus/colyseus-unity-sdk.git#upm

        public string RoomId            => throw Missing();
        public string SessionId         => throw Missing();
        public string ReconnectionToken => throw Missing();

        public MabarinRoom On(string type, Action<JToken> handler) => throw Missing();
        public MabarinRoom On<T>(string type, Action<T> handler)   => throw Missing();
        public MabarinRoom Off(string type)                        => throw Missing();
        public Task Send(string type, object data = null)          => throw Missing();
        public Task Leave()                                        => throw Missing();

        static Exception Missing() => new InvalidOperationException(
            "[Mabarin] Colyseus Unity SDK belum terinstall.\n" +
            "Package Manager → + → Add package from git URL:\n" +
            "https://github.com/colyseus/colyseus-unity-sdk.git#upm");
#endif
    }
}
