using UnityEngine;

namespace Mabar.Multiplayer.Models
{
    [CreateAssetMenu(fileName = "MultiplayerSettings", menuName = "Mabar Multiplayer/Settings")]
    public class MultiplayerSettings : ScriptableObject
    {
        [Header("Backend Configuration")]
        public string ApiUrl = "http://localhost:4000";
        public string WsUrl = "ws://localhost:4000/ws";
        public string ProjectId = "";
        public int ReconnectDelaySeconds = 3;
    }
}
