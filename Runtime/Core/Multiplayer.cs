using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mabar.Multiplayer.Core.Internal;
using Mabar.Multiplayer.Models;
using UnityEngine;

namespace Mabar.Multiplayer.Core
{
    /// <summary>
    /// MabarinClient — SDK entry point.
    ///
    /// Quick start:
    ///   MabarinClient.Initialize(settings);
    ///   await MabarinClient.Connect("PlayerName");
    ///   var room = await MabarinClient.CreateRoom("mabar_room");
    ///   room.On("event", data => Debug.Log(data));
    ///   await room.Send("move", new { x = 1f, y = 0f });
    /// </summary>
    public static class MabarinClient
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
        public static async Task<MabarinRoom> CreateRoom(
            string roomType = "mabar_room",
            Dictionary<string, object> options = null)
        {
            EnsureInit();
            var seat = await Matchmaker.Create(_settings.ServerUrl, roomType, _settings.AppKey, PlayerName, options);
            return await ConnectRoom(seat);
        }

        /// <summary>Join an existing room by its Room ID.</summary>
        public static async Task<MabarinRoom> JoinRoom(
            string roomId,
            Dictionary<string, object> options = null)
        {
            EnsureInit();
            var seat = await Matchmaker.JoinById(_settings.ServerUrl, roomId, _settings.AppKey, PlayerName, options);
            return await ConnectRoom(seat);
        }

        /// <summary>Join any available room of this type, or create one if none exist.</summary>
        public static async Task<MabarinRoom> JoinOrCreate(
            string roomType = "mabar_room",
            Dictionary<string, object> options = null)
        {
            EnsureInit();
            var seat = await Matchmaker.JoinOrCreate(_settings.ServerUrl, roomType, _settings.AppKey, PlayerName, options);
            return await ConnectRoom(seat);
        }

        /// <summary>List available rooms of this type.</summary>
        public static Task<List<RoomInfo>> GetRooms(string roomType = "mabar_room")
        {
            EnsureInit();
            return Matchmaker.GetRooms(_settings.ServerUrl, roomType, _settings.AppKey);
        }

        /// <summary>Reconnect to a room after an unexpected disconnect using the saved token.</summary>
        public static async Task<MabarinRoom> Reconnect(string reconnectionToken)
        {
            EnsureInit();
            var seat = await Matchmaker.Reconnect(_settings.ServerUrl, reconnectionToken);
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
