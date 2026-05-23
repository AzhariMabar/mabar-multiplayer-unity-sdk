using System;
using System.Text;
using System.Threading.Tasks;
using Mabar.Multiplayer.Utils;
using UnityEngine.Networking;

namespace Mabar.Multiplayer.Auth
{
    public class AuthResponse
    {
        public string PlayerId  { get; set; }
        public string Token     { get; set; }
        public long   ExpiresAt { get; set; }
    }

    public class GuestAuth
    {
        private readonly string apiUrl;
        private readonly string appKey;

        public GuestAuth(string apiUrl, string appKey)
        {
            this.apiUrl = apiUrl.TrimEnd('/');
            this.appKey = appKey;
        }

        public async Task<AuthResponse> LoginGuestAsync()
        {
            using var req = new UnityWebRequest($"{apiUrl}/auth/guest", "POST")
            {
                uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Mabar-App-Key", appKey);

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"[MabarSDK] Guest login failed: {req.error} — {req.downloadHandler.text}");

            return JsonUtil.Deserialize<AuthResponse>(req.downloadHandler.text);
        }

        public async Task<AuthResponse> RefreshAsync(string token)
        {
            using var req = new UnityWebRequest($"{apiUrl}/auth/refresh", "POST")
            {
                uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Mabar-App-Key", appKey);
            req.SetRequestHeader("Authorization", $"Bearer {token}");

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"[MabarSDK] Token refresh failed: {req.error} — {req.downloadHandler.text}");

            return JsonUtil.Deserialize<AuthResponse>(req.downloadHandler.text);
        }
    }
}
