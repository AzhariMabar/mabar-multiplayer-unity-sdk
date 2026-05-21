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

        public GuestAuth(string apiUrl)
        {
            this.apiUrl = apiUrl.TrimEnd('/');
        }

        public async Task<AuthResponse> LoginGuestAsync()
        {
            var url = $"{apiUrl}/auth/guest";
            var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"Guest login failed: {request.error}");
            }

            return JsonSerializer.Deserialize<AuthResponse>(request.downloadHandler.text);
        }
    }
}
