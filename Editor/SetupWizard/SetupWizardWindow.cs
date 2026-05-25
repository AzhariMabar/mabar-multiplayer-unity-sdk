using UnityEditor;
using UnityEngine;
using Mabar.Multiplayer.Models;

namespace Mabar.Multiplayer.Editor
{
    public class SetupWizardWindow : EditorWindow
    {
        private MultiplayerSettings settings;
        private int step = 0;

        [MenuItem("Mabar Multiplayer/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<SetupWizardWindow>("Mabar Setup");
            window.minSize = new Vector2(420, 320);
        }

        private void OnGUI()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
            var subStyle   = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            var labelStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };

            GUILayout.Space(12);
            GUILayout.Label("Mabar Multiplayer SDK", titleStyle);
            GUILayout.Label("Self-hosted multiplayer — paste AppKey, start building.", subStyle);
            GUILayout.Space(14);

            step = GUILayout.Toolbar(step, new[] { "1. Create Asset", "2. Set App Key", "3. Done" });
            GUILayout.Space(12);

            switch (step)
            {
                case 0: DrawStepCreateAsset(labelStyle); break;
                case 1: DrawStepSetKey(labelStyle, subStyle); break;
                case 2: DrawStepDone(labelStyle, subStyle); break;
            }
        }

        private void DrawStepCreateAsset(GUIStyle labelStyle)
        {
            GUILayout.Label("Create a Settings Asset for your project:", labelStyle);
            GUILayout.Space(8);

            settings = EditorGUILayout.ObjectField("Settings Asset", settings,
                typeof(MultiplayerSettings), false) as MultiplayerSettings;

            GUILayout.Space(8);
            if (settings == null)
            {
                if (GUILayout.Button("Create Settings Asset", GUILayout.Height(32)))
                {
                    var asset = CreateInstance<MultiplayerSettings>();
                    AssetDatabase.CreateAsset(asset, "Assets/MabarSettings.asset");
                    AssetDatabase.SaveAssets();
                    settings = asset;
                    EditorGUIUtility.PingObject(asset);
                    step = 1;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Asset found! Move to Step 2 to set your App Key.", MessageType.Info);
                if (GUILayout.Button("Next →", GUILayout.Height(28))) step = 1;
            }
        }

        private void DrawStepSetKey(GUIStyle labelStyle, GUIStyle subStyle)
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Go back to Step 1 and create or select a Settings Asset.", MessageType.Warning);
                return;
            }

            GUILayout.Label("Enter your App Key:", labelStyle);
            GUILayout.Label("Get this from the Mabar dashboard after registering your game.", subStyle);
            GUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            var newKey = EditorGUILayout.TextField("App Key", settings.AppKey);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Set AppKey");
                settings.AppKey = newKey;
                EditorUtility.SetDirty(settings);
            }

            GUILayout.Space(6);
            GUILayout.Label("Server URL (default: wss://cloud.mabar.studio):", subStyle);
            EditorGUI.BeginChangeCheck();
            var newUrl = EditorGUILayout.TextField("Server URL", settings.ServerUrl);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Set ServerUrl");
                settings.ServerUrl = newUrl;
                EditorUtility.SetDirty(settings);
            }

            GUILayout.Space(10);
            GUI.enabled = !string.IsNullOrEmpty(settings.AppKey);
            if (GUILayout.Button("Next →", GUILayout.Height(28))) step = 2;
            GUI.enabled = true;
        }

        private void DrawStepDone(GUIStyle labelStyle, GUIStyle subStyle)
        {
            if (settings == null || string.IsNullOrEmpty(settings?.AppKey))
            {
                EditorGUILayout.HelpBox("Complete Steps 1 and 2 first.", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox("Setup complete! You're ready to build.", MessageType.Info);
            GUILayout.Space(8);

            GUILayout.Label("Summary:", labelStyle);
            EditorGUILayout.LabelField("App Key",    $"{settings.AppKey[..System.Math.Min(8, settings.AppKey.Length)]}...");
            EditorGUILayout.LabelField("Server URL", settings.ServerUrl);

            GUILayout.Space(10);
            GUILayout.Label("Quick start:", subStyle);

            var codeStyle = new GUIStyle(EditorStyles.helpBox) { fontStyle = FontStyle.Italic };
            GUILayout.Label(
                "Multiplayer.Initialize(Settings);\nawait Multiplayer.Connect(\"PlayerName\");\n\nvar room = await Multiplayer.CreateRoom(\"mabar_room\");\nroom.On<JObject>(\"event\", data => Debug.Log(data));\nawait room.Send(\"move\", new { idx = 4 });",
                codeStyle);

            GUILayout.Space(10);
            if (GUILayout.Button("Ping Settings Asset", GUILayout.Height(28)))
                EditorGUIUtility.PingObject(settings);
        }
    }
}
