using System;
using System.Collections.Generic;
using UnityEngine;
using Mabar.Multiplayer.Core;
using Mabar.Multiplayer.Models;
using Newtonsoft.Json.Linq;

namespace MabarinSamples
{
    /// <summary>
    /// Chat Room — cross-platform Unity ↔ Web.
    ///
    /// Protocol (mabar_room relay):
    ///   SEND  "chat"  { name, text }
    ///   RECV  "chat"  { name, text }   ← dari pemain lain
    ///
    /// Setup:
    ///   1. Attach ke GameObject
    ///   2. Set Settings (MultiplayerSettings asset)
    ///   3. Play → OnGUI panel muncul
    ///   4. Share Room ID ke tab web /demos atau Unity lain
    /// </summary>
    public class ChatRoomDemo : MonoBehaviour
    {
        [Header("SDK")]
        public MultiplayerSettings Settings;

        // ── State ─────────────────────────────────────────────────────────────
        enum Phase { Setup, Chat }
        Phase _phase = Phase.Setup;

        string _playerName  = "Unity Player";
        string _roomIdInput = "";
        string _msgInput    = "";
        string _statusMsg   = "";

        MabarinRoom           _room;
        List<ChatEntry>       _messages = new();
        Vector2               _scroll;

        struct ChatEntry { public string Sender; public string Text; public bool IsSystem; }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        void OnGUI()
        {
            var area = new Rect(10, 10, Mathf.Min(Screen.width - 20, 500), Screen.height - 20);
            GUILayout.BeginArea(area);

            if (_phase == Phase.Setup) DrawSetup();
            else                       DrawChat();

            GUILayout.EndArea();
        }

        void OnDestroy() => _room?.Leave();

        // ── Setup screen ───────────────────────────────────────────────────────

        void DrawSetup()
        {
            GUILayout.Label("=== 💬 CHAT ROOM ===");
            GUILayout.Label("Cross-platform: Unity ↔ Web (/demos → Chat Room)");
            GUILayout.Space(12);

            GUILayout.Label("Nama:");
            _playerName = GUILayout.TextField(_playerName, GUILayout.Width(260));
            GUILayout.Space(8);

            if (GUILayout.Button("Buat Room", GUILayout.Width(200))) _ = CreateRoom();

            GUILayout.Space(4);
            GUILayout.Label("Room ID (untuk join):");
            _roomIdInput = GUILayout.TextField(_roomIdInput, GUILayout.Width(260));
            if (GUILayout.Button("Gabung Room", GUILayout.Width(200))) _ = JoinRoom();

            if (!string.IsNullOrEmpty(_statusMsg))
                GUILayout.Label(_statusMsg);
        }

        // ── Chat screen ────────────────────────────────────────────────────────

        void DrawChat()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Room: {_room?.RoomId ?? "—"}");
            if (GUILayout.Button("Copy ID", GUILayout.Width(70)))
                GUIUtility.systemCopyBuffer = _room?.RoomId ?? "";
            if (GUILayout.Button("Keluar", GUILayout.Width(60))) LeaveRoom();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Message list
            float listHeight = Screen.height - 140;
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(listHeight));
            foreach (var m in _messages)
            {
                if (m.IsSystem)
                    GUILayout.Label($"— {m.Text} —");
                else
                    GUILayout.Label($"[{m.Sender}] {m.Text}");
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4);

            // Input row
            GUILayout.BeginHorizontal();
            _msgInput = GUILayout.TextField(_msgInput, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Kirim", GUILayout.Width(70))) _ = SendChat();
            GUILayout.EndHorizontal();

            // Enter key shortcut
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return
                && !string.IsNullOrWhiteSpace(_msgInput))
                _ = SendChat();
        }

        // ── Room actions ───────────────────────────────────────────────────────

        async System.Threading.Tasks.Task CreateRoom()
        {
            if (string.IsNullOrWhiteSpace(_playerName)) { _statusMsg = "Isi nama dulu!"; return; }
            _statusMsg = "Membuat room...";
            try
            {
                Multiplayer.Initialize(Settings);
                await Multiplayer.Connect(_playerName.Trim());
                _room = await Multiplayer.CreateRoom("mabar_room");
                RegisterEvents();
                _phase = Phase.Chat;
                AddSystem($"Room dibuat: {_room.RoomId}");
                _statusMsg = "";
            }
            catch (Exception e) { _statusMsg = $"Error: {e.Message}"; }
        }

        async System.Threading.Tasks.Task JoinRoom()
        {
            if (string.IsNullOrWhiteSpace(_playerName)) { _statusMsg = "Isi nama dulu!"; return; }
            if (string.IsNullOrWhiteSpace(_roomIdInput)) { _statusMsg = "Isi Room ID!"; return; }
            _statusMsg = "Bergabung...";
            try
            {
                Multiplayer.Initialize(Settings);
                await Multiplayer.Connect(_playerName.Trim());
                _room = await Multiplayer.JoinRoom(_roomIdInput.Trim());
                RegisterEvents();
                _phase = Phase.Chat;
                AddSystem($"Bergabung ke room {_room.RoomId}");
                _statusMsg = "";
            }
            catch (Exception e) { _statusMsg = $"Error: {e.Message}"; }
        }

        async System.Threading.Tasks.Task SendChat()
        {
            if (_room == null || string.IsNullOrWhiteSpace(_msgInput)) return;
            string text = _msgInput.Trim();
            _msgInput = "";
            AddMsg(Multiplayer.PlayerName, text); // show locally
            try
            {
                await _room.Send("chat", new { name = Multiplayer.PlayerName, text });
            }
            catch (Exception e) { AddSystem($"Gagal kirim: {e.Message}"); }
        }

        void LeaveRoom()
        {
            _room?.Leave();
            _room = null;
            _messages.Clear();
            _phase = Phase.Setup;
            _statusMsg = "";
        }

        // ── Event listeners ────────────────────────────────────────────────────

        void RegisterEvents()
        {
            _room.On<JObject>("room_joined", data =>
            {
                AddSystem($"Kamu ({Multiplayer.PlayerName}) masuk room");
            });

            _room.On<JObject>("player_joined", data =>
            {
                string name = data["name"]?.ToString() ?? "Seseorang";
                AddSystem($"{name} bergabung");
            });

            _room.On<JObject>("player_left", data =>
            {
                string name = data["name"]?.ToString() ?? "Seseorang";
                AddSystem($"{name} meninggalkan room");
            });

            // Chat message from other players
            _room.On<JObject>("chat", data =>
            {
                string sender = data["name"]?.ToString() ?? "Unknown";
                string text   = data["text"]?.ToString() ?? "";
                AddMsg(sender, text);
            });
        }

        // ── UI helpers ─────────────────────────────────────────────────────────

        void AddMsg(string sender, string text)
        {
            _messages.Add(new ChatEntry { Sender = sender, Text = text });
            _scroll = new Vector2(0, float.MaxValue);
        }

        void AddSystem(string text)
        {
            _messages.Add(new ChatEntry { IsSystem = true, Text = text });
            _scroll = new Vector2(0, float.MaxValue);
        }
    }
}
