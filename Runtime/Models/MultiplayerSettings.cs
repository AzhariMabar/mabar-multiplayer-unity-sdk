using UnityEngine;

namespace Mabar.Multiplayer.Models
{
    [CreateAssetMenu(fileName = "MabarSettings", menuName = "Mabar Multiplayer/Settings")]
    public class MultiplayerSettings : ScriptableObject
    {
        [Header("Mabar App Key")]
        [Tooltip("Your unique App Key from Mabar dashboard. Set this in the Unity Editor — never hardcode it.")]
        public string AppKey = "";

        [Header("Backend URL")]
        [Tooltip("Mabar backend API endpoint. Use the hosted URL or http://localhost:4000 for local dev.")]
        public string ApiUrl = "https://mabar-api.vercel.app";

        [Tooltip("WebSocket endpoint. Must match ApiUrl host.")]
        public string WsUrl = "wss://mabar-api.vercel.app/ws";

        [Header("Advanced")]
        public int ReconnectDelaySeconds = 3;
    }
}
