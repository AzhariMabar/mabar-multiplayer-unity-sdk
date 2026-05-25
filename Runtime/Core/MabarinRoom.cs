using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Colyseus;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mabar.Multiplayer.Core
{
    /// <summary>
    /// MabarinRoom — client-side WebSocket room.
    ///
    /// Usage:
    ///   room.On("turn_changed", data => Debug.Log(data));
    ///   await room.Send("end_turn", new { board = myBoard });
    ///   await room.Leave();
    /// </summary>
    public class MabarinRoom
    {
        private readonly ColyseusRoom<object> _room;
        private readonly Dictionary<string, List<Action<JToken>>> _handlers = new();

        /// Room ID — share with other players to join.
        public string RoomId           => _room.RoomId;

        /// Your session ID within this room.
        public string SessionId        => _room.SessionId;

        /// Save this before disconnect to reconnect later.
        public string ReconnectionToken => _room.ReconnectionToken;

        internal MabarinRoom(ColyseusRoom<object> room)
        {
            _room = room;

            // Route all server messages through our handler map
            _room.OnMessage<JToken>("*", (data) =>
            {
                // Colyseus wildcard delivers { type, message } as a JToken
                var type    = data["type"]?.ToString();
                var payload = data["message"];
                if (type != null) Emit(type, payload);
            });

            _room.OnLeave += (code) =>
            {
                Emit("disconnected", JToken.FromObject(new { code }));
            };

            _room.OnError += (code, message) =>
            {
                Debug.LogWarning($"[Mabarin] Room error {code}: {message}");
                Emit("error", JToken.FromObject(new { code, message }));
            };
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Register a handler for a server event. Chain-able.</summary>
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

        /// <summary>Remove all handlers for a given event type.</summary>
        public MabarinRoom Off(string type)
        {
            _handlers.Remove(type);
            return this;
        }

        /// <summary>Send a message to the server.</summary>
        public async Task Send(string type, object data = null)
        {
            await _room.Send(type, data ?? new { });
        }

        /// <summary>Leave the room.</summary>
        public async Task Leave()
        {
            await _room.Leave();
        }

        // ── Internals ──────────────────────────────────────────────────────────

        private void Emit(string type, JToken data)
        {
            if (!_handlers.TryGetValue(type, out var list)) return;
            foreach (var h in list)
            {
                try   { h(data); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
