using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mabar.Multiplayer.Core;
using Mabar.Multiplayer.Models;

/// <summary>
/// Simulasi 4 pemain turn-based via Debug.Log saja.
/// Tidak butuh UI — cukup attach ke GameObject, assign Settings, Play.
///
/// Output di Console:
///   [A] Login OK → player_xxx
///   [B] Login OK → player_yyy
///   ...
///   [A] Buat room → room_zzz
///   [B] Join room OK
///   --- Ronde 1 ---
///   [A] giliran A → kirim gerakan "Serbu kiri"
///   [B] giliran B → kirim gerakan "Bertahan"
///   ...
/// </summary>
public class TurnSimConsole : MonoBehaviour
{
    public Mabar.Multiplayer.Models.MultiplayerSettings Settings;

    [Header("Simulasi")]
    [Tooltip("Nama-nama pemain simulasi")]
    public string[] PlayerNames = { "A", "B", "C", "D" };

    [Tooltip("Berapa ronde simulasi")]
    public int Rounds = 3;

    [Tooltip("Delay antar aksi (detik)")]
    public float Delay = 1f;

    // ─────────────────────────────────────────────────────────────────────

    class SimPlayer
    {
        public string Name;
        public string PlayerId;
        public string Token;
    }

    readonly string[] moves = {
        "Serbu kiri",  "Bertahan",   "Maju tengah",
        "Mundur",      "Pasang jebakan", "Loncat",
        "Roll dadu",   "Draw card",  "Skip",       "Double",
    };

    void Start()
    {
        if (Settings == null) { Debug.LogError("[Sim] Assign MabarSettings di Inspector!"); return; }
        Multiplayer.Initialize(Settings);
        StartCoroutine(RunSimulation());
    }

    IEnumerator RunSimulation()
    {
        Log("=== MABAR TURN SIMULATION MULAI ===");
        Log($"Pemain: {string.Join(", ", PlayerNames)} | Ronde: {Rounds}");
        yield return new WaitForSeconds(Delay);

        // 1. Login semua pemain
        Log("--- LOGIN ---");
        var players = new List<SimPlayer>();
        foreach (var name in PlayerNames)
        {
            var task = Multiplayer.LoginGuest();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
            {
                LogErr($"[{name}] Login gagal: {task.Exception.GetBaseException().Message}");
                yield break;
            }

            var p = new SimPlayer
            {
                Name     = name,
                PlayerId = task.Result.PlayerId,
                Token    = task.Result.Token,
            };
            players.Add(p);
            Log($"[{name}] Login OK → {Truncate(p.PlayerId, 20)}...");
            yield return new WaitForSeconds(Delay * 0.3f);
        }

        yield return new WaitForSeconds(Delay);

        // 2. Pemain pertama buat room, sisanya join
        Log("--- SETUP ROOM ---");

        // Override token ke player A supaya dia yang createRoom
        // (Multiplayer class pakai token global — simulasi sequential)
        SetActivePlayer(players[0]);
        var createTask = Multiplayer.CreateRoom("Sesi Simulasi", maxPlayers: PlayerNames.Length);
        yield return new WaitUntil(() => createTask.IsCompleted);

        if (createTask.Exception != null)
        {
            LogErr($"[{players[0].Name}] Gagal buat room: {createTask.Exception.GetBaseException().Message}");
            yield break;
        }

        string roomId = createTask.Result.Id;
        Log($"[{players[0].Name}] Buat room OK → {roomId}");

        for (int i = 1; i < players.Count; i++)
        {
            SetActivePlayer(players[i]);
            yield return new WaitForSeconds(Delay * 0.5f);

            var joinTask = Multiplayer.JoinRoom(roomId);
            yield return new WaitUntil(() => joinTask.IsCompleted);

            if (joinTask.Exception != null)
            {
                LogErr($"[{players[i].Name}] Gagal join: {joinTask.Exception.GetBaseException().Message}");
                yield break;
            }
            Log($"[{players[i].Name}] Join room OK");
        }

        yield return new WaitForSeconds(Delay);

        // 3. Simulasi ronde
        for (int ronde = 1; ronde <= Rounds; ronde++)
        {
            Log($"\n=== RONDE {ronde} ===");

            // Setiap pemain submit giliran satu per satu
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                SetActivePlayer(player);

                // Poll dulu — pastikan memang giliran dia
                var pollTask = Multiplayer.GetRoom(roomId);
                yield return new WaitUntil(() => pollTask.IsCompleted);

                if (pollTask.Exception != null) { LogErr("Poll error"); yield break; }

                var room = pollTask.Result;
                bool isMyTurn = room.CurrentTurn == player.PlayerId;

                if (!isMyTurn)
                {
                    LogWarn($"[{player.Name}] Bukan giliran ({Truncate(room.CurrentTurn, 12)}... yang jalan)");
                    continue;
                }

                string move = moves[Random.Range(0, moves.Length)];
                Log($"[{player.Name}] GILIRAN → kirim: \"{move}\"");

                yield return new WaitForSeconds(Delay);

                var state = new Dictionary<string, object>
                {
                    { "move",   move },
                    { "player", player.Name },
                    { "ronde",  ronde },
                };

                var turnTask = Multiplayer.SubmitTurn(roomId, state);
                yield return new WaitUntil(() => turnTask.IsCompleted);

                if (turnTask.Exception != null)
                {
                    LogErr($"[{player.Name}] SubmitTurn gagal: {turnTask.Exception.GetBaseException().Message}");
                    yield break;
                }

                var updated  = turnTask.Result;
                int nextIdx  = updated.Players?.IndexOf(updated.CurrentTurn) ?? -1;
                string nextName = nextIdx >= 0 && nextIdx < players.Count ? players[nextIdx].Name : "?";
                Log($"[{player.Name}] OK. Giliran selanjutnya: [{nextName}]");

                yield return new WaitForSeconds(Delay);
            }
        }

        // 4. Selesai — keluar dari room
        yield return new WaitForSeconds(Delay);
        Log("\n=== SIMULASI SELESAI — SEMUA KELUAR ===");

        for (int i = players.Count - 1; i >= 0; i--)
        {
            SetActivePlayer(players[i]);
            var leaveTask = Multiplayer.LeaveRoom(roomId);
            yield return new WaitUntil(() => leaveTask.IsCompleted);

            string result = leaveTask.Exception == null ? "OK" : "error (room mungkin sudah dihapus)";
            Log($"[{players[i].Name}] Leave → {result}");
            yield return new WaitForSeconds(Delay * 0.3f);
        }

        Log("=== DONE ===");
    }

    // Simulasi switch "active player" — di production tiap device punya instance sendiri
    void SetActivePlayer(SimPlayer p)
    {
        Multiplayer.SetToken(p.Token, p.PlayerId);
    }

    static string Truncate(string s, int max) => s?.Length > max ? s[..max] : s ?? "";

    static void Log(string msg)     => Debug.Log($"<color=#38bdf8>[Sim]</color> {msg}");
    static void LogErr(string msg)  => Debug.LogError($"[Sim] {msg}");
    static void LogWarn(string msg) => Debug.LogWarning($"[Sim] {msg}");
}
