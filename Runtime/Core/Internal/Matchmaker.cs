using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mabar.Multiplayer.Models;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Mabar.Multiplayer.Core.Internal
{
    internal struct SeatReservation
    {
        public string SessionId;
        public string RoomId;
        public string ProcessId;
    }

    internal static class Matchmaker
    {
        internal static Task<SeatReservation> Create(
            string serverUrl, string roomType, string appKey, string name,
            Dictionary<string, object> options = null)
            => Post(serverUrl, $"matchmake/create/{roomType}", appKey, name, options);

        internal static Task<SeatReservation> JoinById(
            string serverUrl, string roomId, string appKey, string name,
            Dictionary<string, object> options = null)
            => Post(serverUrl, $"matchmake/joinById/{roomId}", appKey, name, options);

        internal static Task<SeatReservation> JoinOrCreate(
            string serverUrl, string roomType, string appKey, string name,
            Dictionary<string, object> options = null)
            => Post(serverUrl, $"matchmake/join/{roomType}", appKey, name, options);

        internal static Task<SeatReservation> Reconnect(string serverUrl, string reconnectionToken)
            => Post(serverUrl, $"matchmake/reconnect", null, null, new Dictionary<string, object>
                { { "reconnectionToken", reconnectionToken } });

        internal static Task<List<RoomInfo>> GetRooms(string serverUrl, string roomType, string appKey)
        {
            var tcs = new TaskCompletionSource<List<RoomInfo>>();
            MabarinHost.Run(DoGet(serverUrl, roomType, appKey, tcs));
            return tcs.Task;
        }

        // ── POST (matchmake) ───────────────────────────────────────────────────

        private static Task<SeatReservation> Post(
            string serverUrl, string path, string appKey, string name,
            Dictionary<string, object> options)
        {
            var tcs = new TaskCompletionSource<SeatReservation>();
            MabarinHost.Run(DoPost(serverUrl, path, appKey, name, options, tcs));
            return tcs.Task;
        }

        private static IEnumerator DoPost(
            string serverUrl, string path, string appKey, string name,
            Dictionary<string, object> options,
            TaskCompletionSource<SeatReservation> tcs)
        {
            var httpBase = HttpBase(serverUrl);

            var body = new JObject();
            if (appKey != null) body["appKey"] = appKey;
            if (name   != null) body["name"]   = name;
            if (options != null)
                foreach (var kv in options)
                    body[kv.Key] = kv.Value != null ? JToken.FromObject(kv.Value) : JValue.CreateNull();

            var bodyBytes = Encoding.UTF8.GetBytes(body.ToString(Newtonsoft.Json.Formatting.None));

            using var req = new UnityWebRequest($"{httpBase}/{path}", "POST")
            {
                uploadHandler   = new UploadHandlerRaw(bodyBytes),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept",       "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                tcs.SetException(new Exception(
                    $"[Mabarin] Matchmake failed: {req.error} — {req.downloadHandler?.text}"));
                yield break;
            }

            try
            {
                var json = JObject.Parse(req.downloadHandler.text);
                if (json["error"] != null)
                {
                    tcs.SetException(new Exception($"[Mabarin] Matchmake error: {json["error"]}"));
                    yield break;
                }
                tcs.SetResult(new SeatReservation
                {
                    SessionId = json["sessionId"]?.ToString()           ?? "",
                    RoomId    = json["room"]?["roomId"]?.ToString()     ?? "",
                    ProcessId = json["room"]?["processId"]?.ToString()  ?? "",
                });
            }
            catch (Exception e) { tcs.SetException(e); }
        }

        // ── GET (room listing) ─────────────────────────────────────────────────

        private static IEnumerator DoGet(
            string serverUrl, string roomType, string appKey,
            TaskCompletionSource<List<RoomInfo>> tcs)
        {
            var url = $"{HttpBase(serverUrl)}/api/rooms" +
                      $"?type={UnityWebRequest.EscapeURL(roomType)}" +
                      $"&key={UnityWebRequest.EscapeURL(appKey)}";

            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                tcs.SetException(new Exception($"[Mabarin] GetRooms failed: {req.error}"));
                yield break;
            }

            try
            {
                var json  = JObject.Parse(req.downloadHandler.text);
                var list  = new List<RoomInfo>();
                var rooms = json["rooms"] as JArray;
                if (rooms != null)
                    foreach (var r in rooms)
                        list.Add(new RoomInfo
                        {
                            RoomId     = r["roomId"]?.ToString()       ?? "",
                            Clients    = r["clients"]?.Value<int>()    ?? 0,
                            MaxClients = r["maxClients"]?.Value<int>() ?? 0,
                            Label      = r["label"]?.ToString()        ?? "",
                        });
                tcs.SetResult(list);
            }
            catch (Exception e) { tcs.SetException(e); }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string HttpBase(string serverUrl) =>
            serverUrl.Replace("wss://", "https://").Replace("ws://", "http://").TrimEnd('/');
    }
}
