using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Mabar.Multiplayer.Models;
using Mabar.Multiplayer.RPC;

namespace Mabar.Multiplayer.Realtime
{
    public class RealtimeClient
    {
        private readonly string wsUrl;
        private readonly string apiUrl;
        private ClientWebSocket socket;
        private CancellationTokenSource receiveCts;
        private string playerId;
        private string roomId;
        private readonly Dictionary<string, List<Action<RpcPayload>>> rpcListeners = new();

        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;
        public string PlayerId => playerId;
        public string RoomId => roomId;

        public RealtimeClient(string wsUrl, string apiUrl)
        {
            this.wsUrl = wsUrl;
            this.apiUrl = apiUrl;
        }

        public void SetPlayerId(string id)
        {
            playerId = id;
        }

        public void SetRoomId(string id)
        {
            roomId = id;
        }

        public void ClearRoomId()
        {
            roomId = string.Empty;
        }

        public async Task ConnectAsync()
        {
            if (IsConnected)
            {
                return;
            }

            socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
            await SendRawAsync(new { type = "connect", playerId, roomId });

            receiveCts = new CancellationTokenSource();
            _ = ReceiveLoop(receiveCts.Token);
        }

        public async Task SendRpcAsync(RpcPayload payload)
        {
            payload.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await SendRawAsync(new { type = "rpc", payload });
        }

        public void On(string eventName, Action<RpcPayload> callback)
        {
            if (!rpcListeners.ContainsKey(eventName))
            {
                rpcListeners[eventName] = new List<Action<RpcPayload>>();
            }
            rpcListeners[eventName].Add(callback);
        }

        private async Task SendRawAsync(object payload)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Realtime client is not connected.");
            }

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8192];
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", token);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var wrapper = JsonSerializer.Deserialize<RealtimeResponse>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (wrapper?.Type == "rpc" && wrapper.Payload != null)
                    {
                        DispatchRpc(wrapper.Payload);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Realtime receive error: {ex.Message}");
                    break;
                }
            }
        }

        private void DispatchRpc(RpcPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.Event))
            {
                return;
            }

            if (rpcListeners.TryGetValue(payload.Event, out var listeners))
            {
                foreach (var callback in listeners)
                {
                    callback.Invoke(payload);
                }
            }
        }

        public class RealtimeResponse
        {
            public string Type { get; set; }
            public RpcPayload Payload { get; set; }
        }
    }
}
