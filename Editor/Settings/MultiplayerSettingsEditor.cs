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
            var settings = target as MultiplayerSettings;
            if (settings == null) return;

            // AppKey section — highlighted
            EditorGUILayout.Space(4);
            var boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 8, 8) };
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.LabelField("Mabar App Key", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Unique key for your game. Get it from the Mabar dashboard.", EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = string.IsNullOrEmpty(settings.AppKey) ? new Color(1f, 0.4f, 0.4f, 0.3f) : new Color(0.4f, 1f, 0.6f, 0.3f);

            var newKey = EditorGUILayout.TextField("App Key", settings.AppKey);
            if (newKey != settings.AppKey)
            {
                Undo.RecordObject(settings, "Change AppKey");
                settings.AppKey = newKey;
                EditorUtility.SetDirty(settings);
            }

            GUI.backgroundColor = prevColor;

            if (string.IsNullOrEmpty(settings.AppKey))
                EditorGUILayout.HelpBox("AppKey is required. Set it here before building.", MessageType.Error);
            else
                EditorGUILayout.HelpBox("AppKey is set. Do not commit this asset to a public repo if the key is sensitive.", MessageType.Info);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(8);

            // The rest of the fields
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Validate Settings", GUILayout.Height(28)))
            {
                if (string.IsNullOrEmpty(settings.AppKey))
                    EditorUtility.DisplayDialog("Mabar SDK", "AppKey is empty! Enter your App Key.", "OK");
                else if (string.IsNullOrEmpty(settings.ApiUrl))
                    EditorUtility.DisplayDialog("Mabar SDK", "ApiUrl is required.", "OK");
                else
                    EditorUtility.DisplayDialog("Mabar SDK", $"Settings OK!\n\nAppKey: {settings.AppKey[..System.Math.Min(8, settings.AppKey.Length)]}...\nAPI:    {settings.ApiUrl}", "Great!");
            }
        }
    }
}
