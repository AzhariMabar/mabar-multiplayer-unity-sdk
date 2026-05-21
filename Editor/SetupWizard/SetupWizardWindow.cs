using UnityEditor;
using UnityEngine;
using Mabar.Multiplayer.Models;

namespace Mabar.Multiplayer.Editor
{
    public class SetupWizardWindow : EditorWindow
    {
        private MultiplayerSettings settings;

        [MenuItem("Mabar Multiplayer/Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<SetupWizardWindow>("Mabar Multiplayer Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Mabar Multiplayer SDK Setup", EditorStyles.boldLabel);
            settings = EditorGUILayout.ObjectField("Settings Asset", settings, typeof(MultiplayerSettings), false) as MultiplayerSettings;

            if (settings == null)
            {
                if (GUILayout.Button("Create Settings Asset"))
                {
                    var asset = CreateInstance<MultiplayerSettings>();
                    AssetDatabase.CreateAsset(asset, "Assets/MabarMultiplayerSettings.asset");
                    AssetDatabase.SaveAssets();
                    settings = asset;
                }
            }
            else
            {
                EditorGUILayout.LabelField("ApiUrl", settings.ApiUrl);
                EditorGUILayout.LabelField("WsUrl", settings.WsUrl);
            }
        }
    }
}
