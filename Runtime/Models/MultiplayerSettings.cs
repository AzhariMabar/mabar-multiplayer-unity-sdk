using UnityEngine;

namespace Mabar.Multiplayer.Models
{
    [CreateAssetMenu(fileName = "MabarSettings", menuName = "Mabar Multiplayer/Settings")]
    public class MultiplayerSettings : ScriptableObject
    {
        [Header("Mabar App Key")]
        [Tooltip("Your unique App Key. Get it from the SDK owner — paste here, done.")]
        public string AppKey = "";

        [Header("Backend URL")]
        [Tooltip("Mabar API endpoint. Leave default for hosted, or http://localhost:4000 for local dev.")]
        public string ApiUrl = "https://mabar.studio/mk/api";
    }
}
