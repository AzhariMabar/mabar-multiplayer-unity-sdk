using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
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
        internal static Task<SeatReservation> Create(string serverUrl, string roomType, string appKey, string name)
            => Post(serverUrl, $"matchmake/create/{roomType}", appKey, name, null);

        internal static Task<SeatReservation> JoinById(string serverUrl, string roomId, string appKey, string name)
            => Post(serverUrl, $"matchmake/joinById/{roomId}", appKey, name, null);

        internal static Task<SeatReservation> JoinOrCreate(string serverUrl, string roomType, string appKey, string name)
            => Post(serverUrl, $"matchmake/join/{roomType}", appKey, name, null);

        private static Task<SeatReservation> Post(string serverUrl, string path, string appKey, string name, string extra)
        {
            var tcs = new TaskCompletionSource<SeatReservation>();
            MabarinHost.Run(DoPost(serverUrl, path, appKey, name, tcs));
            return tcs.Task;
        }

        private static IEnumerator DoPost(string serverUrl, string path, string appKey, string name,
                                          TaskCompletionSource<SeatReservation> tcs)
        {
            var httpBase = serverUrl
                .Replace("wss://", "https://")
                .Replace("ws://",  "http://")
                .TrimEnd('/');

            var bodyJson = $"{{\"appKey\":\"{EscJson(appKey)}\",\"name\":\"{EscJson(name)}\"}}";
            var bodyBytes = Encoding.UTF8.GetBytes(bodyJson);

            using var req = new UnityWebRequest($"{httpBase}/{path}", "POST")
            {
                uploadHandler   = new UploadHandlerRaw(bodyBytes),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");

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
                    SessionId = json["sessionId"]?.ToString() ?? "",
                    RoomId    = json["room"]?["roomId"]?.ToString() ?? "",
                    ProcessId = json["room"]?["processId"]?.ToString() ?? "",
                });
            }
            catch (Exception e) { tcs.SetException(e); }
        }

        private static string EscJson(string s) =>
            s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }
}
