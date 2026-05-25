using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Colyseus;
using UnityEngine;

namespace Mabar.Multiplayer.Core
{
    /// <summary>
    /// Mabarin SDK v2 — WebSocket entry point.
    ///
    /// Quick start:
    ///   Multiplayer.Initialize(settings);
    ///   await Multiplayer.Connect("YourName");
    ///
    ///   var room = await Multiplayer.CreateRoom();
    ///   room.On("turn_changed", (data) => Debug.Log(data));
    ///   await room.Send("end_turn", new { board = myBoard });
    /// </summary>
    public static class Multiplayer
    {
        private static MultiplayerSettings _settings;
        private static ColyseusClient      _client;

        public static bool   IsInitialized  => _settings != null;
        public static bool   IsConnected    => _client   != null;
        public static string PlayerName     { get; private set; }

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

        // ── Connect ────────────────────────────────────────────────────────────

        /// <summary>
        /// Set player name and initialize WebSocket client.
        /// Call once before CreateRoom / JoinRoom.
        /// </summary>
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

        /// <summary>Create a new room (you become host).</summary>
        public static async Task<MabarinRoom> CreateRoom(string roomType = "turn_room", Dictionary<string, object> options = null)
        {
            EnsureConnected();
            var opts = BuildOptions(options);
            var raw  = await _client.Create<object>(roomType, opts);
            return new MabarinRoom(raw);
        }

        /// <summary>Join an existing room by its ID.</summary>
        public static async Task<MabarinRoom> JoinRoom(string roomId, Dictionary<string, object> options = null)
        {
            EnsureConnected();
            var opts = BuildOptions(options);
            var raw  = await _client.JoinById<object>(roomId, opts);
            return new MabarinRoom(raw);
        }

        /// <summary>
        /// Join any available room, or create one if none exist.
        /// Useful for quick-match.
        /// </summary>
        public static async Task<MabarinRoom> FindOrCreate(string roomType = "turn_room", Dictionary<string, object> options = null)
        {
            EnsureConnected();
            var opts = BuildOptions(options);
            var raw  = await _client.JoinOrCreate<object>(roomType, opts);
            return new MabarinRoom(raw);
        }

        /// <summary>
        /// Reconnect to a room after unexpected disconnect.
        /// Save room.ReconnectionToken before disconnect.
        /// </summary>
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
    }
}
