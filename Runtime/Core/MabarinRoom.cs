using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Mabar.Multiplayer.Core.Internal;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mabar.Multiplayer.Core
{
    /// <summary>
    /// MabarinRoom — WebSocket room client (no external SDK required).
    ///
    /// Usage:
    ///   room.On&lt;JObject&gt;("event", data =&gt; Debug.Log(data));
    ///   await room.Send("move", new { idx = 4 });
    ///   room.Leave();
    /// </summary>
    public class MabarinRoom
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private TaskCompletionSource<bool> _joinTcs;
        private bool _joined;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, List<Action<JToken>>> _handlers = new();

        public string RoomId            { get; private set; }
        public string SessionId         { get; private set; }
        public string ReconnectionToken { get; private set; }

        internal MabarinRoom() { }

        internal async Task Connect(SeatReservation seat, string serverUrl)
        {
            RoomId    = seat.RoomId;
            SessionId = seat.SessionId;

            var wsBase = serverUrl.TrimEnd('/');
            var uri    = new Uri($"{wsBase}/{seat.ProcessId}/{seat.RoomId}?sessionId={seat.SessionId}");

            _ws      = new ClientWebSocket();
            _cts     = new CancellationTokenSource();
            _joinTcs = new TaskCompletionSource<bool>();

            await _ws.ConnectAsync(uri, _cts.Token);

            _ = Task.Run(ReceiveLoop);

            var timeout = Task.Delay(10_000, _cts.Token);
            var done    = await Task.WhenAny(_joinTcs.Task, timeout);

            if (done != _joinTcs.Task || !_joinTcs.Task.Result)
                throw new TimeoutException("[Mabarin] Join timeout — server did not respond.");
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Register a handler for a server event (raw JToken payload).</summary>
        public MabarinRoom On(string type, Action<JToken> handler)
        {
            if (!_handlers.ContainsKey(type)) _handlers[type] = new List<Action<JToken>>();
            _handlers[type].Add(handler);
            return this;
        }

        /// <summary>Register a typed handler — deserializes payload to T via Newtonsoft.Json.</summary>
        public MabarinRoom On<T>(string type, Action<T> handler)
        {
            return On(type, jt =>
            {
                try
                {
                    T value = jt is T direct ? direct : jt.ToObject<T>();
                    handler(value);
                }
                catch (Exception e) { Debug.LogException(e); }
            });
        }

        /// <summary>Remove all handlers for an event type.</summary>
        public MabarinRoom Off(string type) { _handlers.Remove(type); return this; }

        /// <summary>Send a message to the server.</summary>
        public async Task Send(string type, object data = null)
        {
            if (_ws?.State != WebSocketState.Open) return;

            var bytes = new List<byte> { ColyseusProtocol.ROOM_DATA };
            ColyseusProtocol.WriteSchemaString(bytes, type);
            if (data != null) bytes.AddRange(MsgPack.Encode(data));

            await SendRaw(bytes.ToArray());
        }

        /// <summary>Leave the room and close the WebSocket.</summary>
        public async Task Leave()
        {
            _cts?.Cancel();
            if (_ws?.State == WebSocketState.Open)
            {
                try
                {
                    var bye = new byte[] { ColyseusProtocol.LEAVE_ROOM };
                    await _ws.SendAsync(new ArraySegment<byte>(bye), WebSocketMessageType.Binary, true,
                        CancellationToken.None);
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "leave", CancellationToken.None);
                }
                catch { /* best effort */ }
            }
            _ws?.Dispose();
            _ws = null;
        }

        // ── Internals ──────────────────────────────────────────────────────────

        private async Task ReceiveLoop()
        {
            var buf     = new byte[65536];
            var message = new MemoryStream();
            try
            {
                while (_ws?.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    try { result = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), _cts.Token); }
                    catch (OperationCanceledException) { break; }
                    catch { break; }

                    if (result.MessageType == WebSocketMessageType.Close) break;

                    message.Write(buf, 0, result.Count);

                    if (!result.EndOfMessage) continue;

                    var data = message.ToArray();
                    message.SetLength(0);
                    HandleMessage(data);
                }
            }
            finally
            {
                MabarinHost.Dispatch(() => Emit("disconnected", JValue.CreateNull()));
            }
        }

        private void HandleMessage(byte[] data)
        {
            if (data.Length == 0) return;
            byte code = data[0];

            if (code == ColyseusProtocol.JOIN_ROOM && !_joined)
            {
                int offset = 1;
                ReconnectionToken = ColyseusProtocol.ReadColyseusString(data, ref offset);
                _joined = true;
                _ = SendRaw(new byte[] { ColyseusProtocol.JOIN_ROOM });
                _joinTcs.TrySetResult(true);
                return;
            }

            if (code == ColyseusProtocol.ROOM_DATA)
            {
                int offset = 1;
                string type;
                try { type = ColyseusProtocol.ReadSchemaString(data, ref offset); }
                catch (Exception e) { Debug.LogWarning($"[Mabarin] ROOM_DATA parse error: {e.Message}"); return; }
                JToken payload = offset < data.Length ? MsgPack.Decode(data, offset) : JValue.CreateNull();
                MabarinHost.Dispatch(() => Emit(type, payload));
                return;
            }

            if (code == ColyseusProtocol.LEAVE_ROOM)
            {
                MabarinHost.Dispatch(() => Emit("disconnected", JValue.CreateNull()));
                return;
            }

            if (code == ColyseusProtocol.ERROR)
            {
                Debug.LogWarning("[Mabarin] Server error received.");
                MabarinHost.Dispatch(() => Emit("error", JValue.CreateNull()));
            }
        }

        private async Task SendRaw(byte[] data)
        {
            if (_ws?.State != WebSocketState.Open) return;
            await _sendLock.WaitAsync(_cts?.Token ?? CancellationToken.None);
            try
            {
                await _ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true,
                    _cts?.Token ?? CancellationToken.None);
            }
            finally { _sendLock.Release(); }
        }

        private void Emit(string type, JToken payload)
        {
            if (!_handlers.TryGetValue(type, out var list)) return;
            foreach (var h in list)
                try { h(payload); } catch (Exception e) { Debug.LogException(e); }
        }
    }
}
