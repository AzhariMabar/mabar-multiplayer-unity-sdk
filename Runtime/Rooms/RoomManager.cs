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
        private static string appKey;
        private static RealtimeClient realtime;
        private static string currentRoomId;

        public static void Initialize(string apiBaseUrl, string appKeyValue, RealtimeClient realtimeClient)
        {
            apiUrl = apiBaseUrl.TrimEnd('/');
            appKey = appKeyValue;
            realtime = realtimeClient;
        }

        public static async Task CreateRoomAsync(string name, int maxPlayers, bool isPrivate)
        {
            var body = new { playerId = realtime.PlayerId, name, maxPlayers, isPrivate };
            var response = await PostAsync($"{apiUrl}/rooms/create", body);
            var room = JsonSerializer.Deserialize<RoomRecord>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            currentRoomId = room?.Id;
            realtime.SetRoomId(currentRoomId);
        }

        public static async Task JoinRoomAsync(string roomId)
        {
            var body = new { roomId, playerId = realtime.PlayerId };
            var response = await PostAsync($"{apiUrl}/rooms/join", body);
            var room = JsonSerializer.Deserialize<RoomRecord>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            currentRoomId = room?.Id;
            realtime.SetRoomId(currentRoomId);
        }

        public static async Task LeaveRoomAsync(string roomId)
        {
            var body = new { roomId, playerId = realtime.PlayerId };
            await PostAsync($"{apiUrl}/rooms/leave", body);
            currentRoomId = string.Empty;
            realtime.ClearRoomId();
        }

        private static async Task<string> PostAsync(string url, object body)
        {
            var payload = JsonSerializer.Serialize(body);
            using var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Mabar-App-Key", appKey);

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"HTTP request failed: {request.error} — {request.downloadHandler.text}");

            return request.downloadHandler.text;
        }
    }
}
