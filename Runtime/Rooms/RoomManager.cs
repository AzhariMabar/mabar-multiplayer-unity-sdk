using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Mabar.Multiplayer.Realtime;
using Mabar.Multiplayer.Models;

namespace Mabar.Multiplayer.Rooms
{
    public static class RoomManager
    {
        private static string apiUrl;
        private static RealtimeClient realtime;
        private static string currentRoomId;

        public static void Initialize(string apiBaseUrl, RealtimeClient realtimeClient)
        {
            apiUrl = apiBaseUrl.TrimEnd('/');
            realtime = realtimeClient;
        }

        public static async Task CreateRoomAsync(string name, int maxPlayers, bool isPrivate)
        {
            var url = $"{apiUrl}/rooms/create";
            var body = new { playerId = realtime.PlayerId, name, maxPlayers, isPrivate };
            var response = await SendPostAsync(url, body);
            var room = JsonSerializer.Deserialize<RoomRecord>(response);
            currentRoomId = room?.Id;
            realtime.SetRoomId(currentRoomId);
        }

        public static async Task JoinRoomAsync(string roomId)
        {
            var url = $"{apiUrl}/rooms/join";
            var body = new { roomId, playerId = realtime.PlayerId };
            var response = await SendPostAsync(url, body);
            var room = JsonSerializer.Deserialize<RoomRecord>(response);
            currentRoomId = room?.Id;
            realtime.SetRoomId(currentRoomId);
        }

        public static async Task LeaveRoomAsync(string roomId)
        {
            var url = $"{apiUrl}/rooms/leave";
            var body = new { roomId, playerId = realtime.PlayerId };
            await SendPostAsync(url, body);
            currentRoomId = string.Empty;
            realtime.ClearRoomId();
        }

        private static async Task<string> SendPostAsync(string url, object body)
        {
            var payload = JsonSerializer.Serialize(body);
            using var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"HTTP request failed: {request.error}");
            }
            return request.downloadHandler.text;
        }
    }
}
