using UnityEngine;

namespace Mabar.Multiplayer.Models
{
    /// <summary>
    /// Mabarin SDK settings — create via Assets → Create → Mabarin → Settings.
    /// </summary>
    [CreateAssetMenu(fileName = "MabarinSettings", menuName = "Mabarin/Settings")]
    public class MultiplayerSettings : ScriptableObject
    {
        [Header("Server")]
        [Tooltip("WebSocket server URL. ws://localhost:2567 for local, wss://api.mabar.studio for cloud.")]
        public string ServerUrl = "wss://api.mabar.studio";

        [Header("App Key")]
        [Tooltip("Your Mabarin AppKey. Get one at mabarin.studio or via POST /apps/register.")]
        public string AppKey = "";
    }
}
