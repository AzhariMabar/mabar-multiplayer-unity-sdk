using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mabar.Multiplayer.Core.Internal;
using UnityEngine;

namespace Mabar.Multiplayer.Core
{
    /// <summary>
    /// Mabarin SDK v2 — entry point.
    ///
    /// No external dependencies required (self-contained WebSocket client).
    ///
    /// Quick start:
    ///   Multiplayer.Initialize(settings);
    ///   await Multiplayer.Connect("PlayerName");
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

        // ── Connect ────────────────────────────────────────────────────────────

        /// <summary>Set player name. Call once before CreateRoom / JoinRoom.</summary>
        public static Task Connect(string playerName = "")
        {
            EnsureInit();
            PlayerName = string.IsNullOrEmpty(playerName)
                ? $"Player_{UnityEngine.Random.Range(1000, 9999)}"
                : playerName.Trim();
            Debug.Log($"[Mabarin] Player: {PlayerName}");
            return Task.CompletedTask;
        }

        // ── Room management ────────────────────────────────────────────────────

        /// <summary>Create a new room — you become host.</summary>
        public static async Task<MabarinRoom> CreateRoom(string roomType = "turn_room",
            Dictionary<string, object> options = null)
        {
            EnsureInit();
            var seat = await Matchmaker.Create(_settings.ServerUrl, roomType, _settings.AppKey, PlayerName);
            return await ConnectRoom(seat);
        }

        /// <summary>Join an existing room by its Room ID.</summary>
        public static async Task<MabarinRoom> JoinRoom(string roomId,
            Dictionary<string, object> options = null)
        {
            EnsureInit();
            var seat = await Matchmaker.JoinById(_settings.ServerUrl, roomId, _settings.AppKey, PlayerName);
            return await ConnectRoom(seat);
        }

        /// <summary>Join any available room of this type, or create one if none exist.</summary>
        public static async Task<MabarinRoom> FindOrCreate(string roomType = "turn_room",
            Dictionary<string, object> options = null)
        {
            EnsureInit();
            var seat = await Matchmaker.JoinOrCreate(_settings.ServerUrl, roomType, _settings.AppKey, PlayerName);
            return await ConnectRoom(seat);
        }

        // ── Internals ──────────────────────────────────────────────────────────

        private static async Task<MabarinRoom> ConnectRoom(SeatReservation seat)
        {
            var room = new MabarinRoom();
            await room.Connect(seat, _settings.ServerUrl);
            return room;
        }

        private static void EnsureInit()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("[Mabarin] Call Initialize(settings) first.");
        }
    }
}
