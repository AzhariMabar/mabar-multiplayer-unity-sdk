using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if MABAR_COLYSEUS
using Colyseus;
#endif

namespace Mabar.Multiplayer.Core
{
    /// <summary>
    /// Mabarin SDK v2 — WebSocket entry point.
    ///
    /// Requires Colyseus Unity SDK installed first:
    ///   Package Manager → + → Add package from git URL
    ///   https://github.com/colyseus/colyseus-unity-sdk.git#upm
    ///
    /// Quick start:
    ///   Multiplayer.Initialize(settings);
    ///   await Multiplayer.Connect("YourName");
    ///   var room = await Multiplayer.CreateRoom("mabar_room");
    ///   room.On&lt;JObject&gt;("event", data =&gt; Debug.Log(data));
    ///   await room.Send("move", new { idx = 4 });
    /// </summary>
    public static class Multiplayer
    {
        private static MultiplayerSettings _settings;

        public static bool   IsInitialized => _settings != null;
        public static string PlayerName    { get; private set; }

        // ── Setup ──────────────────────────────────────────────────────────────

        public static void Initialize(MultiplayerSettings settings)
        {
            if (string.IsNullOrEmpty(settings?.AppKey))
            {
                Debug.LogError("[Mabarin] AppKey is empty. Set it in MultiplayerSettings asset.");
                return;
            }
            _settings = settings;
            Debug.Log($"[Mabarin] Initialized. Server: {settings.ServerUrl}");
        }

#if MABAR_COLYSEUS
        private static ColyseusClient _client;

        public static bool IsConnected => _client != null;

        // ── Connect ────────────────────────────────────────────────────────────

        /// <summary>Set player name and prepare WebSocket client. Call before CreateRoom/JoinRoom.</summary>
        public static Task Connect(string playerName = "")
        {
            EnsureInitialized();
            PlayerName = string.IsNullOrEmpty(playerName)
                ? $"Player_{UnityEngine.Random.Range(1000, 9999)}"
                : playerName.Trim();
            _client = new ColyseusClient(_settings.ServerUrl);
            Debug.Log($"[Mabarin] Connected as {PlayerName}");
            return Task.CompletedTask;
        }

        // ── Room management ────────────────────────────────────────────────────

        /// <summary>Create a new room — you become host.</summary>
        public static async Task<MabarinRoom> CreateRoom(string roomType = "turn_room", Dictionary<string, object> options = null)
        {
            EnsureConnected();
            var raw = await _client.Create<object>(roomType, BuildOptions(options));
            return new MabarinRoom(raw);
        }

        /// <summary>Join an existing room by its ID.</summary>
        public static async Task<MabarinRoom> JoinRoom(string roomId, Dictionary<string, object> options = null)
        {
            EnsureConnected();
            var raw = await _client.JoinById<object>(roomId, BuildOptions(options));
            return new MabarinRoom(raw);
        }

        /// <summary>Join any available room, or create one if none exist (quick-match).</summary>
        public static async Task<MabarinRoom> FindOrCreate(string roomType = "turn_room", Dictionary<string, object> options = null)
        {
            EnsureConnected();
            var raw = await _client.JoinOrCreate<object>(roomType, BuildOptions(options));
            return new MabarinRoom(raw);
        }

        /// <summary>Reconnect to a room after disconnect. Pass room.ReconnectionToken saved before disconnect.</summary>
        public static async Task<MabarinRoom> Reconnect(string reconnectionToken)
        {
            EnsureConnected();
            var raw = await _client.ReconnectById<object>(reconnectionToken);
            return new MabarinRoom(raw);
        }

        // ── Internals ──────────────────────────────────────────────────────────

        private static Dictionary<string, object> BuildOptions(Dictionary<string, object> extra)
        {
            var opts = new Dictionary<string, object>
            {
                { "appKey", _settings.AppKey },
                { "name",   PlayerName },
            };
            if (extra != null)
                foreach (var kv in extra) opts[kv.Key] = kv.Value;
            return opts;
        }

        private static void EnsureInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("[Mabarin] Call Initialize(settings) first.");
        }

        private static void EnsureConnected()
        {
            EnsureInitialized();
            if (!IsConnected)
                throw new InvalidOperationException("[Mabarin] Call Connect() first.");
        }

#else
        // ── Stub: Colyseus SDK not installed ───────────────────────────────────
        // Install via Package Manager → + → Add package from git URL:
        //   https://github.com/colyseus/colyseus-unity-sdk.git#upm

        public static bool IsConnected => false;

        public static Task Connect(string playerName = "")
            => throw Missing();
        public static Task<MabarinRoom> CreateRoom(string roomType = "turn_room", Dictionary<string, object> options = null)
            => throw Missing();
        public static Task<MabarinRoom> JoinRoom(string roomId, Dictionary<string, object> options = null)
            => throw Missing();
        public static Task<MabarinRoom> FindOrCreate(string roomType = "turn_room", Dictionary<string, object> options = null)
            => throw Missing();
        public static Task<MabarinRoom> Reconnect(string reconnectionToken)
            => throw Missing();

        static Exception Missing() => new InvalidOperationException(
            "[Mabarin] Colyseus Unity SDK belum terinstall.\n" +
            "Package Manager → + → Add package from git URL:\n" +
            "https://github.com/colyseus/colyseus-unity-sdk.git#upm");
#endif
    }
}
