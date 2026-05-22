using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Mabar.Multiplayer.Auth
{
    public class AuthResponse
    {
        public string PlayerId { get; set; }
        public string Token { get; set; }
        public long ExpiresAt { get; set; }
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
            var url = $"{apiUrl}/auth/guest";
            using var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Mabar-App-Key", appKey);

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"Guest login failed: {request.error} — {request.downloadHandler.text}");

            return JsonSerializer.Deserialize<AuthResponse>(request.downloadHandler.text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
