using System;
using System.Threading.Tasks;
using Mabar.Multiplayer.Realtime;
using Mabar.Multiplayer.Models;

namespace Mabar.Multiplayer.RPC
{
    public class RpcClient
    {
        private readonly RealtimeClient realtime;

        public RpcClient(RealtimeClient realtimeClient)
        {
            realtime = realtimeClient;
        }

        public async Task SendAsync(string eventName, object payload)
        {
            var rpc = new RpcPayload
            {
                Event = eventName,
                SenderId = realtime.PlayerId,
                RoomId = realtime.RoomId,
                Payload = payload,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            await realtime.SendRpcAsync(rpc);
        }

        public void On(string eventName, Action<RpcPayload> callback)
        {
            realtime.On(eventName, callback);
        }
    }
}
