using System;
using UnityEngine;
using Mabar.Multiplayer.Core;
using Mabar.Multiplayer.Models;
using Newtonsoft.Json.Linq;

namespace Mabar.Multiplayer.Samples
{
    /// <summary>
    /// TicTacToe 1v1 — cross-platform Unity ↔ Web.
    ///
    /// Protocol (mabar_room — pure relay, no server turn enforcement):
    ///   SEND  "move"  { idx }   ← kirim move kamu (cell 0-8)
    ///   RECV  "move"  { idx }   ← move lawan
    ///
    /// Cocok dimainkan lintas platform dengan web /demos → Tic-Tac-Toe.
    /// Share Room ID ke browser — room bisa digunakan dari Unity atau web.
    ///
    /// Host = X (selalu jalan duluan), Joiner = O.
    /// Deteksi menang dilakukan di client masing-masing.
    ///
    /// Setup:
    ///   1. Attach ke GameObject
    ///   2. Set Settings (MultiplayerSettings asset, AppKey = 'dev' atau dari dashboard)
    ///   3. Play → gunakan OnGUI panel
    /// </summary>
    public class TicTacToeGame : MonoBehaviour
    {
        [Header("SDK")]
        public MultiplayerSettings Settings;

        // ── Board ──────────────────────────────────────────────────────────────
        // 0 = empty, 1 = X, 2 = O
        readonly int[] _board = new int[9];

        static readonly int[][] WIN_LINES =
        {
            new[]{0,1,2}, new[]{3,4,5}, new[]{6,7,8},
            new[]{0,3,6}, new[]{1,4,7}, new[]{2,5,8},
            new[]{0,4,8}, new[]{2,4,6},
        };

        // ── State ──────────────────────────────────────────────────────────────
        enum Phase { Setup, Waiting, Game, Over }
        Phase _phase = Phase.Setup;

        string   _playerName   = "Unity Player";
        string   _roomIdInput  = "";
        string   _statusMsg    = "";
        string   _mySymbol     = "X";   // "X" or "O"
        string   _opponentName = "";
        bool     _myTurn       = false;
        string   _result       = "";    // "X" | "O" | "draw" | ""

        MabarinRoom _room;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        void OnGUI()
        {
            var area = new Rect(10, 10, Mathf.Min(Screen.width - 20, 420), Screen.height - 20);
            GUILayout.BeginArea(area);

            switch (_phase)
            {
                case Phase.Setup:   DrawSetup();   break;
                case Phase.Waiting: DrawWaiting(); break;
                case Phase.Game:    DrawGame();    break;
                case Phase.Over:    DrawOver();    break;
            }

            GUILayout.EndArea();
        }

        void OnDestroy() => _room?.Leave();

        // ── Setup screen ───────────────────────────────────────────────────────

        void DrawSetup()
        {
            GUILayout.Label("=== ✕ ○ TIC-TAC-TOE ===");
            GUILayout.Label("Cross-platform: Unity ↔ Web (/demos → Tic-Tac-Toe)");
            GUILayout.Space(12);

            GUILayout.Label("Nama:");
            _playerName = GUILayout.TextField(_playerName, GUILayout.Width(260));
            GUILayout.Space(8);

            if (GUILayout.Button("Buat Room (jadi X)", GUILayout.Width(220))) _ = CreateRoom();

            GUILayout.Space(4);
            GUILayout.Label("Room ID (untuk join sebagai O):");
            _roomIdInput = GUILayout.TextField(_roomIdInput, GUILayout.Width(260));
            if (GUILayout.Button("Gabung Room (jadi O)", GUILayout.Width(220))) _ = JoinRoom();

            if (!string.IsNullOrEmpty(_statusMsg))
                GUILayout.Label(_statusMsg);
        }

        // ── Waiting screen ─────────────────────────────────────────────────────

        void DrawWaiting()
        {
            GUILayout.Label("=== LOBBY ===");
            GUILayout.Label($"Kamu: {_playerName} ({_mySymbol})");
            GUILayout.Label($"Room ID: {_room?.RoomId ?? "—"}");
            if (GUILayout.Button("Copy Room ID", GUILayout.Width(160)))
                GUIUtility.systemCopyBuffer = _room?.RoomId ?? "";
            GUILayout.Space(8);
            GUILayout.Label("Menunggu lawan bergabung...");
            GUILayout.Label("(Share Room ID ke tab web atau Unity lain)");
        }

        // ── Game screen ────────────────────────────────────────────────────────

        void DrawGame()
        {
            GUILayout.Label($"Kamu: {_playerName} ({_mySymbol})   Lawan: {_opponentName}");
            GUILayout.Label(_myTurn ? ">>> GILIRAN KAMU <<<" : $"Menunggu {_opponentName}...");
            GUILayout.Space(8);

            int myVal  = _mySymbol == "X" ? 1 : 2;
            int oppVal = myVal == 1 ? 2 : 1;

            for (int row = 0; row < 3; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 3; col++)
                {
                    int idx  = row * 3 + col;
                    int cell = _board[idx];
                    string label = cell == 1 ? "X" : cell == 2 ? "O" : "·";

                    GUI.enabled = _myTurn && cell == 0;
                    if (GUILayout.Button(label, GUILayout.Width(60), GUILayout.Height(60)))
                        _ = MakeMove(idx);
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            if (!string.IsNullOrEmpty(_statusMsg))
                GUILayout.Label(_statusMsg);
        }

        // ── Over screen ────────────────────────────────────────────────────────

        void DrawOver()
        {
            GUILayout.Label("=== GAME OVER ===");

            if (_result == "draw")
                GUILayout.Label("SERI!");
            else if (_result == _mySymbol)
                GUILayout.Label($"KAMU MENANG! ({_mySymbol})");
            else
                GUILayout.Label($"Kalah... ({_result} menang)");

            GUILayout.Space(8);

            // Show final board
            for (int row = 0; row < 3; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 3; col++)
                {
                    int v = _board[row * 3 + col];
                    GUILayout.Label(v == 1 ? " X " : v == 2 ? " O " : " · ", GUILayout.Width(40));
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(12);
            if (GUILayout.Button("Main Lagi", GUILayout.Width(120))) Reset();
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
                _room      = await Multiplayer.CreateRoom("mabar_room");
                _mySymbol  = "X";
                RegisterEvents();
                _phase = Phase.Waiting;
                _statusMsg = "";
                Debug.Log($"[TTT] Room dibuat: {_room.RoomId}");
            }
            catch (Exception e) { _statusMsg = $"Error: {e.Message}"; }
        }

        async System.Threading.Tasks.Task JoinRoom()
        {
            if (string.IsNullOrWhiteSpace(_playerName))  { _statusMsg = "Isi nama dulu!"; return; }
            if (string.IsNullOrWhiteSpace(_roomIdInput)) { _statusMsg = "Isi Room ID!"; return; }
            _statusMsg = "Bergabung...";
            try
            {
                Multiplayer.Initialize(Settings);
                await Multiplayer.Connect(_playerName.Trim());
                _room      = await Multiplayer.JoinRoom(_roomIdInput.Trim());
                _mySymbol  = "O";
                _myTurn    = false; // X goes first
                RegisterEvents();
                _phase = Phase.Waiting;
                _statusMsg = "";
                Debug.Log($"[TTT] Joined room: {_room.RoomId}");
            }
            catch (Exception e) { _statusMsg = $"Error: {e.Message}"; }
        }

        async System.Threading.Tasks.Task MakeMove(int idx)
        {
            if (!_myTurn || _board[idx] != 0) return;
            _myTurn = false;

            // Apply locally first (optimistic)
            int myVal = _mySymbol == "X" ? 1 : 2;
            _board[idx] = myVal;

            // Check if I won
            string w = CheckWin();
            if (w != "") { SetResult(w); }

            try
            {
                await _room.Send("move", new { idx });
            }
            catch (Exception e)
            {
                // Rollback on send failure
                _board[idx] = 0;
                _myTurn = true;
                _statusMsg = $"Gagal kirim: {e.Message}";
            }
        }

        // ── Events ─────────────────────────────────────────────────────────────

        void RegisterEvents()
        {
            _room.On<JObject>("room_joined", data =>
            {
                // If joining as O, host's info is in data.players[0]
                if (_mySymbol == "O")
                {
                    var players = data["players"] as JArray;
                    if (players != null && players.Count > 0)
                    {
                        _opponentName = players[0]["name"]?.ToString() ?? "Host";
                        StartGame();
                    }
                }
                Debug.Log($"[TTT] room_joined. Symbol={_mySymbol}");
            });

            _room.On<JObject>("player_joined", data =>
            {
                // Host gets this when O joins
                if (_mySymbol == "X")
                {
                    _opponentName = data["name"]?.ToString() ?? "Lawan";
                    StartGame();
                }
                Debug.Log($"[TTT] player_joined: {data["name"]}");
            });

            _room.On<JObject>("move", data =>
            {
                // Opponent's move
                int idx    = data["idx"]?.ToObject<int>() ?? -1;
                int oppVal = _mySymbol == "X" ? 2 : 1;

                if (idx >= 0 && idx < 9 && _board[idx] == 0)
                    _board[idx] = oppVal;

                // Check if opponent won
                string w = CheckWin();
                if (w != "")
                    SetResult(w);
                else
                    _myTurn = true; // my turn now

                _statusMsg = _myTurn ? "Giliran kamu!" : $"Menunggu {_opponentName}...";
                Debug.Log($"[TTT] move recv idx={idx}, myTurn={_myTurn}");
            });

            _room.On<JObject>("player_left", _ =>
            {
                if (_phase == Phase.Game)
                {
                    _result = _mySymbol; // opponent left = I win
                    _phase  = Phase.Over;
                    _statusMsg = "Lawan meninggalkan room.";
                }
            });
        }

        // ── Game logic ─────────────────────────────────────────────────────────

        void StartGame()
        {
            Array.Fill(_board, 0);
            _result    = "";
            _myTurn    = _mySymbol == "X"; // X goes first
            _phase     = Phase.Game;
            _statusMsg = _myTurn ? "Giliran kamu!" : $"Giliran {_opponentName}";
            Debug.Log($"[TTT] Game started! {_playerName}={_mySymbol}, opponent={_opponentName}, myTurn={_myTurn}");
        }

        string CheckWin()
        {
            int xWins = CheckSymbol(1); // 1 = X value
            int oWins = CheckSymbol(2); // 2 = O value

            if (xWins > 0) return "X";
            if (oWins > 0) return "O";

            // Draw check — all cells filled
            foreach (int v in _board)
                if (v == 0) return "";

            return "draw";
        }

        int CheckSymbol(int val)
        {
            foreach (var line in WIN_LINES)
                if (_board[line[0]] == val && _board[line[1]] == val && _board[line[2]] == val)
                    return 1;
            return 0;
        }

        void SetResult(string result)
        {
            _result = result;
            _phase  = Phase.Over;
            Debug.Log($"[TTT] Game over: {result}");
        }

        void Reset()
        {
            _room?.Leave();
            _room = null;
            Array.Fill(_board, 0);
            _result    = "";
            _myTurn    = false;
            _statusMsg = "";
            _opponentName = "";
            _phase = Phase.Setup;
        }
    }
}
