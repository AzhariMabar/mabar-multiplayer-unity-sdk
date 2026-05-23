using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mabar.Multiplayer.Models;
using Mabar.Multiplayer.Utils;
using UnityEngine.Networking;

namespace Mabar.Multiplayer.Rooms
{
    public static class RoomManager
    {
        private static string apiUrl;
        private static string appKey;
        private static string token;

        public static string CurrentRoomId { get; private set; }

        public static void Initialize(string apiBaseUrl, string appKeyValue)
        {
            apiUrl = apiBaseUrl.TrimEnd('/');
            appKey = appKeyValue;
        }

        public static void SetToken(string sessionToken)
        {
            token = sessionToken;
        }

        // ─── Room ops ──────────────────────────────────────────────────────

        public static async Task<RoomRecord> CreateRoomAsync(string name, int maxPlayers, bool isPrivate)
        {
            var body = new { name, maxPlayers, isPrivate };
            var json = await PostAsync("/rooms/create", body);
            var room = JsonUtil.Deserialize<RoomRecord>(json);
            CurrentRoomId = room?.Id;
            return room;
        }

        public static async Task<RoomRecord> JoinRoomAsync(string roomId, string inviteCode = null)
        {
            var body = inviteCode != null
                ? (object)new { roomId, inviteCode }
                : new { roomId };
            var json = await PostAsync("/rooms/join", body);
            var room = JsonUtil.Deserialize<RoomRecord>(json);
            CurrentRoomId = room?.Id;
            return room;
        }

        public static async Task<RoomRecord> LeaveRoomAsync(string roomId)
        {
            var json = await PostAsync("/rooms/leave", new { roomId });
            CurrentRoomId = null;
            return JsonUtil.Deserialize<RoomRecord>(json);
        }

        public static async Task<RoomRecord> GetRoomAsync(string roomId)
        {
            var json = await GetAsync($"/rooms/{roomId}");
            return JsonUtil.Deserialize<RoomRecord>(json);
        }

        public static async Task<RoomRecord> SubmitTurnAsync(string roomId, object state, string nextTurn = null)
        {
            var body = nextTurn != null
                ? (object)new { roomId, state, nextTurn }
                : new { roomId, state };
            var json = await PostAsync("/rooms/turn", body);
            return JsonUtil.Deserialize<RoomRecord>(json);
        }

        // ─── HTTP helpers ──────────────────────────────────────────────────

        private static async Task<string> PostAsync(string path, object body)
        {
            EnsureAuth();
            var payload = JsonUtil.Serialize(body);
            using var req = new UnityWebRequest($"{apiUrl}{path}", "POST")
            {
                uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Mabar-App-Key", appKey);
            req.SetRequestHeader("Authorization", $"Bearer {token}");

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"[MabarSDK] {path} failed: {req.error} — {req.downloadHandler.text}");

            return req.downloadHandler.text;
        }

        private static async Task<string> GetAsync(string path)
        {
            EnsureAuth();
            using var req = UnityWebRequest.Get($"{apiUrl}{path}");
            req.SetRequestHeader("X-Mabar-App-Key", appKey);
            req.SetRequestHeader("Authorization", $"Bearer {token}");

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"[MabarSDK] GET {path} failed: {req.error} — {req.downloadHandler.text}");

            return req.downloadHandler.text;
        }

        private static void EnsureAuth()
        {
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("[MabarSDK] Not authenticated. Call Multiplayer.LoginGuest() first.");
        }
    }
}
