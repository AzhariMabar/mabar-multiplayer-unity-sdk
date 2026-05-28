using System;
using System.Collections.Generic;
using UnityEngine;
using Mabar.Multiplayer.Core;
using Mabar.Multiplayer.Models;
using Newtonsoft.Json.Linq;

/// <summary>
/// Mabarin SDK — Turn-Based Demo (WebSocket)
///
/// Setup:
///   1. Buat empty GameObject di scene, attach script ini
///   2. Assign MultiplayerSettings asset ke field Settings di Inspector
///   3. Play — UI muncul, isi nama lalu Connect
/// </summary>
public class MabarTurnDemo : MonoBehaviour
{
    public MultiplayerSettings Settings;

    // ─── State ─────────────────────────────────────────────────────────────────

    enum Screen { Connect, Lobby, Game }
    Screen _screen = Screen.Connect;

    MabarinRoom _room;

    string _status        = "Isi nama lalu klik Connect.";
    string _nameInput     = "Player";
    string _roomIdInput   = "";
    string _moveInput     = "";
    string _mySessionId   = "";
    string _currentTurn   = "";       // sessionId siapa yang giliran
    string _currentTurnName = "";
    bool   _isHost        = false;
    bool   _gameStarted   = false;
    bool   _busy          = false;

    // Players: { id, name }
    readonly List<(string id, string name)> _players = new();
    readonly List<string> _log = new();
    Vector2 _logScroll;

    // ─── GUI Styles ────────────────────────────────────────────────────────────

    GUIStyle _styleTitle, _styleStatus, _styleBox, _styleBtnPrimary,
             _styleBtnSecondary, _styleBtnDanger, _styleLabel, _styleMono,
             _styleTurnActive, _styleTurnWaiting, _styleLog;
    bool _stylesReady;

    // ─── Unity ────────────────────────────────────────────────────────────────

    void Start()
    {
        if (Settings == null)
        {
            _status = "ERROR: Assign MultiplayerSettings di Inspector!";
            return;
        }
        MabarinClient.Initialize(Settings);
    }

    void OnGUI()
    {
        BuildStyles();

        // Background
        GUI.color = new Color(0.05f, 0.07f, 0.11f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = Mathf.Min(540f, Screen.width - 32f);
        float x = (Screen.width - w) / 2f;

        GUILayout.BeginArea(new Rect(x, 24f, w, Screen.height - 48f));
        GUILayout.BeginVertical();

        GUILayout.Label("Mabarin SDK — Turn Demo", _styleTitle);
        GUILayout.Label(_status, _styleStatus);
        GUILayout.Space(12);

        switch (_screen)
        {
            case Screen.Connect: DrawConnect(); break;
            case Screen.Lobby:   DrawLobby();   break;
            case Screen.Game:    DrawGame();    break;
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // ─── Screens ───────────────────────────────────────────────────────────────

    void DrawConnect()
    {
        GUILayout.BeginVertical(_styleBox);
        GUILayout.Label("Nama Pemain", _styleLabel);
        _nameInput = GUILayout.TextField(_nameInput, 30, GUILayout.Height(32));
        GUILayout.Space(8);
        GUI.enabled = !_busy && !string.IsNullOrEmpty(_nameInput.Trim());
        if (GUILayout.Button(_busy ? "Menghubungkan..." : "Connect", _styleBtnPrimary, GUILayout.Height(38)))
            DoConnect();
        GUI.enabled = true;
        GUILayout.EndVertical();
    }

    void DrawLobby()
    {
        GUILayout.BeginVertical(_styleBox);
        GUILayout.Label("Buat Room Baru", BoldLabel());
        GUILayout.Space(4);
        GUILayout.Label("Kamu jadi host. Bagikan Room ID ke pemain lain.", _styleLabel);
        GUILayout.Space(6);
        GUI.enabled = !_busy;
        if (GUILayout.Button(_busy ? "..." : "Buat Room (turn_room)", _styleBtnPrimary, GUILayout.Height(36)))
            DoCreateRoom();
        GUI.enabled = true;
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical(_styleBox);
        GUILayout.Label("Gabung ke Room", BoldLabel());
        GUILayout.Space(4);
        GUILayout.Label("Paste Room ID dari host:", _styleLabel);
        GUILayout.Space(4);
        _roomIdInput = GUILayout.TextField(_roomIdInput, GUILayout.Height(32));
        GUILayout.Space(6);
        GUI.enabled = !_busy && !string.IsNullOrEmpty(_roomIdInput);
        if (GUILayout.Button(_busy ? "..." : "Join Room", _styleBtnSecondary, GUILayout.Height(36)))
            DoJoinRoom(_roomIdInput.Trim());
        GUI.enabled = true;
        GUILayout.EndVertical();
    }

    void DrawGame()
    {
        // ── Room ID ──
        GUILayout.BeginVertical(_styleBox);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Room:", _styleLabel, GUILayout.Width(46));
        GUILayout.Label(_room?.Id ?? "—", _styleMono);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        // ── Players ──
        GUILayout.BeginVertical(_styleBox);
        GUILayout.Label("Pemain", BoldLabel());
        GUILayout.Space(6);
        foreach (var (id, name) in _players)
        {
            bool isTurn = _gameStarted && id == _currentTurn;
            bool isMe   = id == _mySessionId;
            string lbl  = (isTurn ? "▶  " : "    ") + name + (isMe ? " (kamu)" : "");
            GUILayout.Label(lbl, isTurn ? _styleTurnActive : _styleTurnWaiting, GUILayout.Height(28));
            GUILayout.Space(2);
        }

        if (!_gameStarted && _isHost && _players.Count >= 2)
        {
            GUILayout.Space(6);
            if (GUILayout.Button("Mulai Game", _styleBtnPrimary, GUILayout.Height(34)))
                _room?.Send("start_game");
        }
        GUILayout.EndVertical();

        GUILayout.Space(8);

        // ── Turn panel ──
        GUILayout.BeginVertical(_styleBox);
        bool myTurn = _gameStarted && _mySessionId == _currentTurn;

        if (!_gameStarted)
            GUILayout.Label("Menunggu game dimulai...", _styleLabel);
        else if (myTurn)
        {
            GUILayout.Label("Giliran kamu!",
                new GUIStyle(_styleLabel) { normal = { textColor = new Color(0.24f, 1f, 0.37f) } });
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            _moveInput = GUILayout.TextField(_moveInput, GUILayout.Height(32), GUILayout.ExpandWidth(true));
            GUILayout.Space(6);
            if (GUILayout.Button("Akhiri Giliran", _styleBtnPrimary, GUILayout.Width(120), GUILayout.Height(32)))
                DoEndTurn();
            GUILayout.EndHorizontal();
            GUILayout.Label("Isi gerakan (opsional), lalu Akhiri Giliran.", _styleStatus);
        }
        else
        {
            GUILayout.Label("Menunggu giliran " + _currentTurnName + "...",
                new GUIStyle(_styleLabel) { normal = { textColor = new Color(0.6f, 0.6f, 0.7f) } });
        }
        GUILayout.EndVertical();

        GUILayout.Space(8);

        // ── Log ──
        GUILayout.BeginVertical(_styleBox);
        GUILayout.Label("Log", BoldLabel());
        GUILayout.Space(4);
        _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.Height(110));
        for (int i = _log.Count - 1; i >= 0; i--)
            GUILayout.Label(_log[i], _styleLog);
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.Space(8);
        if (GUILayout.Button("Keluar Room", _styleBtnDanger, GUILayout.Height(30)))
            DoLeave();
    }

    // ─── Actions ───────────────────────────────────────────────────────────────

    async void DoConnect()
    {
        _busy = true;
        _status = "Menghubungkan...";
        try
        {
            await MabarinClient.Connect(_nameInput.Trim());
            _status = "Terhubung sebagai " + MabarinClient.PlayerName + ".";
            _screen = Screen.Lobby;
        }
        catch (Exception e) { _status = "Gagal: " + e.Message; }
        _busy = false;
    }

    async void DoCreateRoom()
    {
        _busy = true;
        _status = "Membuat room...";
        try
        {
            _room = await MabarinClient.CreateRoom("turn_room");
            _isHost = true;
            _mySessionId = _room.SessionId;
            RegisterRoomEvents();
            _screen = Screen.Game;
            _status = "Room dibuat! Bagikan Room ID ke pemain lain.";
            AddLog("Room dibuat. Room ID: " + _room.Id);
        }
        catch (Exception e) { _status = "Gagal buat room: " + e.Message; }
        _busy = false;
    }

    async void DoJoinRoom(string roomId)
    {
        _busy = true;
        _status = "Bergabung...";
        try
        {
            _room = await MabarinClient.JoinRoom(roomId);
            _isHost = false;
            _mySessionId = _room.SessionId;
            RegisterRoomEvents();
            _screen = Screen.Game;
            _status = "Bergabung ke room " + roomId + ".";
            AddLog("Bergabung ke room.");
        }
        catch (Exception e) { _status = "Gagal join: " + e.Message; }
        _busy = false;
    }

    void DoEndTurn()
    {
        string move = _moveInput.Trim();
        _moveInput = "";
        if (!string.IsNullOrEmpty(move))
            _room?.Send("end_turn", new { move });
        else
            _room?.Send("end_turn", new { });
    }

    async void DoLeave()
    {
        if (_room != null) { await _room.Leave(); _room = null; }
        _players.Clear();
        _log.Clear();
        _currentTurn = "";
        _currentTurnName = "";
        _gameStarted = false;
        _isHost = false;
        _screen = Screen.Lobby;
        _status = "Keluar dari room.";
    }

    // ─── Room events ───────────────────────────────────────────────────────────

    void RegisterRoomEvents()
    {
        _room.On("room_joined", (JToken data) =>
        {
            var rawPlayers = data["players"] as JArray;
            RefreshPlayerList(rawPlayers);
        });

        _room.On("player_joined", (JToken data) =>
        {
            string id   = data["id"]?.ToString()   ?? "";
            string name = data["name"]?.ToString() ?? id;
            if (!_players.Exists(p => p.id == id))
                _players.Add((id, name));
            int count = data["playerCount"]?.Value<int>() ?? _players.Count;
            _status = count + " pemain di room.";
            AddLog(name + " bergabung.");
        });

        _room.On("player_left", (JToken data) =>
        {
            string id   = data["id"]?.ToString()   ?? "";
            string name = data["name"]?.ToString() ?? id;
            _players.RemoveAll(p => p.id == id);
            AddLog(name + " keluar.");
        });

        _room.On("game_started", (JToken data) =>
        {
            _gameStarted = true;
            _currentTurn     = data["currentTurn"]?.ToString()     ?? "";
            _currentTurnName = data["currentTurnName"]?.ToString() ?? _currentTurn;
            var rawPlayers = data["players"] as JArray;
            RefreshPlayerList(rawPlayers);
            AddLog("Game dimulai! Giliran pertama: " + _currentTurnName);
            _status = "Game berlangsung.";
        });

        _room.On("turn_changed", (JToken data) =>
        {
            _currentTurn     = data["currentTurn"]?.ToString()     ?? "";
            _currentTurnName = data["currentTurnName"]?.ToString() ?? _currentTurn;
            string prevName  = data["prevTurnName"]?.ToString()    ?? "";
            string move      = data["payload"]?["move"]?.ToString() ?? "";
            string logLine   = prevName + " → " + _currentTurnName;
            if (!string.IsNullOrEmpty(move)) logLine += " | gerakan: " + move;
            AddLog(logLine);
            _status = "Giliran: " + _currentTurnName;
        });

        _room.On("turn_timeout", (JToken data) =>
        {
            string timedOut      = data["timedOutPlayerName"]?.ToString() ?? "";
            _currentTurn         = data["currentTurn"]?.ToString()        ?? "";
            _currentTurnName     = data["currentTurnName"]?.ToString()    ?? _currentTurn;
            AddLog(timedOut + " timeout → giliran " + _currentTurnName);
        });

        _room.On("game_over", (JToken data) =>
        {
            _gameStarted = false;
            AddLog("Game selesai! " + data?.ToString());
            _status = "Game selesai.";
        });

        _room.On("error", (JToken data) =>
        {
            string msg = data["message"]?.ToString() ?? "Unknown error";
            _status = "Error: " + msg;
            AddLog("[Error] " + msg);
        });

        _room.On("disconnected", (JToken _) =>
        {
            _status = "Terputus dari server.";
            _screen = Screen.Lobby;
        });
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    void RefreshPlayerList(JArray raw)
    {
        _players.Clear();
        if (raw == null) return;
        foreach (var p in raw)
        {
            string id   = p["id"]?.ToString()   ?? "";
            string name = p["name"]?.ToString() ?? id;
            _players.Add((id, name));
        }
    }

    void AddLog(string msg)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        _log.Add("[" + time + "] " + msg);
        if (_log.Count > 60) _log.RemoveAt(0);
    }

    GUIStyle BoldLabel() => new GUIStyle(_styleLabel) { fontStyle = FontStyle.Bold, fontSize = 13 };

    // ─── Styles ────────────────────────────────────────────────────────────────

    void BuildStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _styleTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 20, fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.35f, 0.65f, 1f) },
            alignment = TextAnchor.MiddleCenter,
        };
        _styleStatus = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12, wordWrap = true,
            normal   = { textColor = new Color(0.55f, 0.55f, 0.6f) },
            alignment = TextAnchor.MiddleCenter,
        };
        _styleBox = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = MakeTex(new Color(0.07f, 0.09f, 0.13f, 0.97f)) },
            padding = new RectOffset(16, 16, 14, 14),
        };
        _styleBtnPrimary = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13, fontStyle = FontStyle.Bold,
            normal   = { background = MakeTex(new Color(0.08f, 0.48f, 0.2f)),  textColor = Color.white },
            hover    = { background = MakeTex(new Color(0.12f, 0.6f,  0.25f)), textColor = Color.white },
            active   = { background = MakeTex(new Color(0.05f, 0.35f, 0.15f)), textColor = Color.white },
            padding  = new RectOffset(12, 12, 8, 8),
        };
        _styleBtnSecondary = new GUIStyle(_styleBtnPrimary)
        {
            normal = { background = MakeTex(new Color(0.1f, 0.13f, 0.18f)), textColor = new Color(0.7f, 0.7f, 0.75f) },
            hover  = { background = MakeTex(new Color(0.15f, 0.18f, 0.25f)), textColor = Color.white },
        };
        _styleBtnDanger = new GUIStyle(_styleBtnPrimary)
        {
            normal = { background = MakeTex(new Color(0.55f, 0.1f, 0.1f)), textColor = Color.white },
            hover  = { background = MakeTex(new Color(0.7f,  0.12f, 0.12f)), textColor = Color.white },
        };
        _styleLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12, normal = { textColor = new Color(0.7f, 0.7f, 0.75f) },
        };
        _styleMono = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12, font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
            normal   = { textColor = new Color(0.35f, 0.65f, 1f) },
        };
        _styleTurnActive = new GUIStyle(GUI.skin.box)
        {
            fontSize  = 13, fontStyle = FontStyle.Bold,
            normal    = { background = MakeTex(new Color(0.04f, 0.28f, 0.12f, 0.85f)), textColor = new Color(0.24f, 1f, 0.37f) },
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(12, 8, 5, 5),
        };
        _styleTurnWaiting = new GUIStyle(GUI.skin.box)
        {
            fontSize  = 12,
            normal    = { background = MakeTex(new Color(0.07f, 0.09f, 0.14f, 0.6f)), textColor = new Color(0.45f, 0.45f, 0.5f) },
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(12, 8, 5, 5),
        };
        _styleLog = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11, wordWrap = true,
            normal   = { textColor = new Color(0.5f, 0.55f, 0.6f) },
        };
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
