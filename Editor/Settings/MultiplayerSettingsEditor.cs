using UnityEditor;
using UnityEngine;
using Mabar.Multiplayer.Models;

namespace Mabar.Multiplayer.Editor
{
    [CustomEditor(typeof(MultiplayerSettings))]
    public class MultiplayerSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var settings = target as MultiplayerSettings;
            if (settings == null) return;

            GUILayout.Space(10);
            if (GUILayout.Button("Validate Settings"))
            {
                if (string.IsNullOrEmpty(settings.ApiUrl) || string.IsNullOrEmpty(settings.WsUrl))
                {
                    EditorUtility.DisplayDialog("Mabar Multiplayer", "Please fill ApiUrl and WsUrl.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Mabar Multiplayer", "Settings look good.", "OK");
                }
            }
        }
    }
}
