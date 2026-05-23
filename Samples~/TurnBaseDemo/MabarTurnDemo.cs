using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mabar.Multiplayer.Core;
using Mabar.Multiplayer.Models;

/// <summary>
/// MabarinSDK — Turn-based Demo
///
/// Setup:
///   1. Buat empty GameObject di scene, attach script ini
///   2. Assign MabarSettings asset ke field Settings di Inspector
///   3. Play — UI otomatis muncul
///
/// Flow:
///   Login → Create Room (host) atau Join Room (player lain) →
///   Kirim giliran → Lihat state semua player real-time
/// </summary>
public class MabarTurnDemo : MonoBehaviour
{
    public Mabar.Multiplayer.Models.MultiplayerSettings Settings;

    // ─── State ─────────────────────────────────────────────────────────────

    enum Screen { Login, Lobby, Game }
    Screen screen = Screen.Login;

    string statusMsg   = "Belum login.";
    string roomIdInput = "";
    string moveInput   = "";
    string myPlayerId  = "";
    string myToken     = "";

    RoomRecord currentRoom;
    List<string> moveLog = new List<string>();

    bool   isPolling   = false;
    bool   actionBusy  = false;

    // ─── Styles (inisialisasi sekali) ──────────────────────────────────────

    GUIStyle styleTitle, styleStatus, styleBox, styleBtnPrimary, styleBtnSecondary,
             styleLabel, styleMono, styleTurnActive, styleTurnWaiting, styleLog;
    bool stylesReady = false;
    Vector2 logScroll;

    // ─── Unity ─────────────────────────────────────────────────────────────

    void Start()
    {
        if (Settings == null)
        {
            statusMsg = "ERROR: Assign MabarSettings di Inspector!";
            return;
        }
        Multiplayer.Initialize(Settings);
        statusMsg = "SDK siap. Tekan Login untuk mulai.";
    }

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        styleTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22, fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.55f, 0.85f, 1f) },
            alignment = TextAnchor.MiddleCenter,
        };
        styleStatus = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12, wordWrap = true,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
            alignment = TextAnchor.MiddleCenter,
        };
        styleBox = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = MakeTexture(new Color(0.08f, 0.11f, 0.18f, 0.97f)) },
            padding = new RectOffset(20, 20, 16, 16),
        };
        styleBtnPrimary = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13, fontStyle = FontStyle.Bold,
            normal   = { background = MakeTexture(new Color(0.05f, 0.55f, 0.9f)),  textColor = Color.white },
            hover    = { background = MakeTexture(new Color(0.1f, 0.65f, 1f)),     textColor = Color.white },
            active   = { background = MakeTexture(new Color(0.0f, 0.4f, 0.75f)),   textColor = Color.white },
            padding  = new RectOffset(12, 12, 8, 8),
        };
        styleBtnSecondary = new GUIStyle(styleBtnPrimary)
        {
            normal = { background = MakeTexture(new Color(0.15f, 0.18f, 0.25f)), textColor = new Color(0.7f, 0.7f, 0.7f) },
            hover  = { background = MakeTexture(new Color(0.2f, 0.24f, 0.32f)),  textColor = Color.white },
        };
        styleLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12, normal = { textColor = new Color(0.75f, 0.75f, 0.75f) },
        };
        styleMono = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11, font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
            normal = { textColor = new Color(0.55f, 0.85f, 1f) },
        };
        styleTurnActive = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13, fontStyle = FontStyle.Bold,
            normal = {
                background = MakeTexture(new Color(0.05f, 0.35f, 0.15f, 0.8f)),
                textColor  = new Color(0.4f, 1f, 0.6f),
            },
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(12, 8, 6, 6),
        };
        styleTurnWaiting = new GUIStyle(GUI.skin.box)
        {
            fontSize = 12,
            normal = {
                background = MakeTexture(new Color(0.1f, 0.12f, 0.18f, 0.6f)),
                textColor  = new Color(0.45f, 0.45f, 0.55f),
            },
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(12, 8, 6, 6),
        };
        styleLog = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11, wordWrap = true, richText = true,
            normal    = { textColor = new Color(0.6f, 0.65f, 0.7f) },
        };
    }

    void OnGUI()
    {
        InitStyles();

        // Background
        GUI.color = new Color(0.06f, 0.09f, 0.15f, 1f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = Mathf.Min(560f, Screen.width - 32f);
        float x = (Screen.width - w) / 2f;
        float y = 30f;

        GUILayout.BeginArea(new Rect(x, y, w, Screen.height - 60f));
        GUILayout.BeginVertical();

        // Header
        GUILayout.Label("MabarinSDK — Turn Demo", styleTitle);
        GUILayout.Label(statusMsg, styleStatus);
        GUILayout.Space(12);

        switch (screen)
        {
            case Screen.Login:  DrawLogin(); break;
            case Screen.Lobby:  DrawLobby(); break;
            case Screen.Game:   DrawGame();  break;
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // ─── Screens ───────────────────────────────────────────────────────────

    void DrawLogin()
    {
        GUILayout.BeginVertical(styleBox);
        GUILayout.Label("Klik Login untuk masuk sebagai guest player.", styleLabel);
        GUILayout.Label("Tiap instance Unity = satu pemain berbeda.", styleLabel);
        GUILayout.Space(10);
        GUI.enabled = !actionBusy;
        if (GUILayout.Button(actionBusy ? "Logging in..." : "Login sebagai Guest", styleBtnPrimary, GUILayout.Height(40)))
            StartCoroutine(DoLogin());
        GUI.enabled = true;
        GUILayout.EndVertical();
    }

    void DrawLobby()
    {
        // Create room
        GUILayout.BeginVertical(styleBox);
        GUILayout.Label("Buat Room Baru", new GUIStyle(styleLabel) { fontStyle = FontStyle.Bold, fontSize = 13 });
        GUILayout.Space(4);
        GUILayout.Label("Kamu jadi host. Bagikan Room ID ke pemain lain.", styleLabel);
        GUILayout.Space(8);
        GUI.enabled = !actionBusy;
        if (GUILayout.Button(actionBusy ? "..." : "Buat Room (max 4 pemain)", styleBtnPrimary, GUILayout.Height(36)))
            StartCoroutine(DoCreateRoom());
        GUI.enabled = true;
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Join room
        GUILayout.BeginVertical(styleBox);
        GUILayout.Label("Gabung ke Room yang Ada", new GUIStyle(styleLabel) { fontStyle = FontStyle.Bold, fontSize = 13 });
        GUILayout.Space(4);
        GUILayout.Label("Paste Room ID dari host:", styleLabel);
        GUILayout.Space(4);
        roomIdInput = GUILayout.TextField(roomIdInput, GUILayout.Height(32));
        GUILayout.Space(6);
        GUI.enabled = !actionBusy && !string.IsNullOrEmpty(roomIdInput);
        if (GUILayout.Button(actionBusy ? "..." : "Join Room", styleBtnSecondary, GUILayout.Height(36)))
            StartCoroutine(DoJoinRoom(roomIdInput.Trim()));
        GUI.enabled = true;
        GUILayout.EndVertical();

        GUILayout.Space(8);
        GUILayout.Label($"Player ID kamu: {myPlayerId}", styleMono);
    }

    void DrawGame()
    {
        if (currentRoom == null) return;

        bool myTurn = currentRoom.CurrentTurn == myPlayerId;

        // Room info
        GUILayout.BeginVertical(styleBox);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Room: {currentRoom.Id}", styleMono);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{currentRoom.Players?.Count ?? 0}/{currentRoom.MaxPlayers} pemain", styleLabel);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        // Player turn list
        GUILayout.BeginVertical(styleBox);
        GUILayout.Label("Urutan Giliran", new GUIStyle(styleLabel) { fontStyle = FontStyle.Bold });
        GUILayout.Space(6);
        if (currentRoom.Players != null)
        {
            for (int i = 0; i < currentRoom.Players.Count; i++)
            {
                var pid      = currentRoom.Players[i];
                bool isTurn  = pid == currentRoom.CurrentTurn;
                bool isMe    = pid == myPlayerId;
                string label = $"{(isTurn ? "▶  " : "    ")}Player {i + 1}{(isMe ? " (kamu)" : "")}";
                GUILayout.Label(label, isTurn ? styleTurnActive : styleTurnWaiting, GUILayout.Height(28));
                GUILayout.Space(2);
            }
        }
        GUILayout.EndVertical();

        GUILayout.Space(8);

        // Submit move
        GUILayout.BeginVertical(styleBox);
        if (myTurn)
        {
            GUILayout.Label("Giliran kamu! Masukkan gerakan:", new GUIStyle(styleLabel) { normal = { textColor = new Color(0.4f, 1f, 0.6f) } });
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            moveInput = GUILayout.TextField(moveInput, GUILayout.Height(32), GUILayout.ExpandWidth(true));
            GUILayout.Space(6);
            GUI.enabled = !actionBusy && !string.IsNullOrEmpty(moveInput.Trim());
            if (GUILayout.Button(actionBusy ? "..." : "Kirim", styleBtnPrimary, GUILayout.Width(70), GUILayout.Height(32)))
                StartCoroutine(DoSubmitTurn());
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.Label("Contoh: 'e4', 'draw card', 'skip', 'roll 6'", styleStatus);
        }
        else
        {
            GUILayout.Label(
                $"Menunggu giliran Player {(currentRoom.Players?.IndexOf(currentRoom.CurrentTurn) + 1 ?? 0)}...",
                new GUIStyle(styleLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.6f) } }
            );
            if (GUILayout.Button("Refresh sekarang", styleBtnSecondary, GUILayout.Height(28)))
                StartCoroutine(DoPollOnce());
        }
        GUILayout.EndVertical();

        GUILayout.Space(8);

        // Move log
        GUILayout.BeginVertical(styleBox);
        GUILayout.Label("Riwayat Giliran", new GUIStyle(styleLabel) { fontStyle = FontStyle.Bold });
        GUILayout.Space(4);
        logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.Height(120));
        if (moveLog.Count == 0)
            GUILayout.Label("Belum ada gerakan.", styleLog);
        else
            for (int i = moveLog.Count - 1; i >= 0; i--)
                GUILayout.Label(moveLog[i], styleLog);
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        // Leave
        if (GUILayout.Button("Keluar dari Room", styleBtnSecondary, GUILayout.Height(30)))
            StartCoroutine(DoLeaveRoom());
    }

    // ─── Actions ───────────────────────────────────────────────────────────

    IEnumerator DoLogin()
    {
        actionBusy = true;
        statusMsg  = "Logging in...";

        var task = Multiplayer.LoginGuest();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            statusMsg = $"Login gagal: {task.Exception.GetBaseException().Message}";
        }
        else
        {
            var auth  = task.Result;
            myPlayerId = auth.PlayerId;
            myToken    = auth.Token;
            statusMsg  = $"Login berhasil! ID: {Truncate(myPlayerId, 16)}...";
            screen     = Screen.Lobby;
        }
        actionBusy = false;
    }

    IEnumerator DoCreateRoom()
    {
        actionBusy = true;
        statusMsg  = "Membuat room...";

        var task = Multiplayer.CreateRoom("Sesi Mabar", maxPlayers: 4);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            statusMsg = $"Gagal buat room: {task.Exception.GetBaseException().Message}";
        }
        else
        {
            currentRoom = task.Result;
            moveLog.Clear();
            LogMove($"Room dibuat! ID: <b>{currentRoom.Id}</b>");
            statusMsg = $"Room ready. Bagikan ID ini ke lawan.";
            screen    = Screen.Game;
            StartPolling();
        }
        actionBusy = false;
    }

    IEnumerator DoJoinRoom(string roomId)
    {
        actionBusy = true;
        statusMsg  = $"Joining room {Truncate(roomId, 12)}...";

        var task = Multiplayer.JoinRoom(roomId);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            statusMsg = $"Gagal join: {task.Exception.GetBaseException().Message}";
        }
        else
        {
            currentRoom = task.Result;
            moveLog.Clear();
            LogMove($"Bergabung ke room <b>{currentRoom.Id}</b>");
            statusMsg = "Berhasil join! Menunggu giliran...";
            screen    = Screen.Game;
            StartPolling();
        }
        actionBusy = false;
    }

    IEnumerator DoSubmitTurn()
    {
        if (string.IsNullOrEmpty(moveInput.Trim())) yield break;
        actionBusy = true;

        string move = moveInput.Trim();
        moveInput   = "";

        var state = new System.Collections.Generic.Dictionary<string, object>
        {
            { "lastMove",   move },
            { "movedBy",    myPlayerId },
            { "movedAt",    System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
        };

        var task = Multiplayer.SubmitTurn(currentRoom.Id, state);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            statusMsg = $"Gagal submit: {task.Exception.GetBaseException().Message}";
            moveInput  = move;
        }
        else
        {
            currentRoom = task.Result;
            int nextIdx = currentRoom.Players?.IndexOf(currentRoom.CurrentTurn) + 1 ?? 0;
            LogMove($"<color=#4ade80>Kamu</color> → <b>{move}</b>");
            statusMsg = $"Giliran dikirim! Sekarang giliran Player {nextIdx}.";
        }
        actionBusy = false;
    }

    IEnumerator DoPollOnce()
    {
        var task = Multiplayer.GetRoom(currentRoom.Id);
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.Exception == null && task.Result != null)
        {
            var prev = currentRoom;
            currentRoom = task.Result;
            if (prev?.CurrentTurn != currentRoom.CurrentTurn)
                LogMove($"Giliran berpindah ke Player {currentRoom.Players?.IndexOf(currentRoom.CurrentTurn) + 1 ?? 0}");
        }
    }

    IEnumerator DoLeaveRoom()
    {
        actionBusy = true;
        StopPolling();
        var task = Multiplayer.LeaveRoom(currentRoom.Id);
        yield return new WaitUntil(() => task.IsCompleted);
        currentRoom = null;
        moveLog.Clear();
        screen    = Screen.Lobby;
        statusMsg = "Keluar dari room.";
        actionBusy = false;
    }

    // ─── Polling ───────────────────────────────────────────────────────────

    void StartPolling()
    {
        if (!isPolling) StartCoroutine(PollLoop());
    }

    void StopPolling()
    {
        isPolling = false;
    }

    IEnumerator PollLoop()
    {
        isPolling = true;
        while (isPolling && currentRoom != null)
        {
            yield return new WaitForSeconds(2f);
            if (actionBusy || currentRoom == null) continue;

            var task = Multiplayer.GetRoom(currentRoom.Id);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null || task.Result == null) continue;

            var updated = task.Result;
            if (updated.CurrentTurn != currentRoom.CurrentTurn)
            {
                currentRoom = updated;
                int idx = currentRoom.Players?.IndexOf(currentRoom.CurrentTurn) + 1 ?? 0;
                LogMove($"Giliran berpindah → <color=#38bdf8>Player {idx}</color>");
                if (currentRoom.CurrentTurn == myPlayerId)
                    statusMsg = "Giliran kamu!";
                else
                    statusMsg = $"Menunggu Player {idx}...";
            }
            else
            {
                currentRoom = updated;
            }
        }
        isPolling = false;
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    void LogMove(string msg)
    {
        string time = System.DateTime.Now.ToString("HH:mm:ss");
        moveLog.Add($"<color=#334155>[{time}]</color> {msg}");
        if (moveLog.Count > 50) moveLog.RemoveAt(0);
    }

    static string Truncate(string s, int max) =>
        s?.Length > max ? s[..max] : s ?? "";

    static Texture2D MakeTexture(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
