using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mabar.Multiplayer.Auth;
using Mabar.Multiplayer.Models;
using Mabar.Multiplayer.Rooms;
using UnityEngine;

namespace Mabar.Multiplayer.Core
{
    /// <summary>
    /// Main entry point for Mabar Multiplayer SDK.
    ///
    /// Usage:
    ///   Multiplayer.Initialize(settings);
    ///   var auth = await Multiplayer.LoginGuest();
    ///   var room = await Multiplayer.CreateRoom("Match", maxPlayers: 2);
    ///   await Multiplayer.SubmitTurn(room.Id, new Dictionary&lt;string, object&gt; { {"move", "e4"} });
    ///   var state = await Multiplayer.GetRoom(room.Id);  // poll for opponent's turn
    /// </summary>
    public static class Multiplayer
    {
        private static MultiplayerSettings settings;
        private static GuestAuth guestAuth;

        public static string PlayerId     { get; private set; }
        public static string Token        { get; private set; }
        public static long   TokenExpiry  { get; private set; }
        public static string CurrentRoomId => RoomManager.CurrentRoomId;

        public static bool IsInitialized => settings != null;
        public static bool IsAuthenticated => !string.IsNullOrEmpty(Token);

        // ─── Setup ─────────────────────────────────────────────────────────

        public static void Initialize(MultiplayerSettings configuration)
        {
            if (string.IsNullOrEmpty(configuration?.AppKey))
            {
                Debug.LogError("[MabarSDK] AppKey is empty. Set it in the MabarSettings asset.");
                return;
            }

            settings  = configuration;
            guestAuth = new GuestAuth(configuration.ApiUrl, configuration.AppKey);
            RoomManager.Initialize(configuration.ApiUrl, configuration.AppKey);

            Debug.Log($"[MabarSDK] Initialized. AppKey: {configuration.AppKey[..Math.Min(8, configuration.AppKey.Length)]}...");
        }

        // ─── Auth ──────────────────────────────────────────────────────────

        public static async Task<AuthResponse> LoginGuest()
        {
            EnsureInitialized();
            var response = await guestAuth.LoginGuestAsync();
            ApplyAuth(response);
            return response;
        }

        /// Call this before TokenExpiry to keep the session alive without re-login.
        public static async Task<AuthResponse> RefreshToken()
        {
            EnsureInitialized();
            EnsureAuthenticated();
            var response = await guestAuth.RefreshAsync(Token);
            ApplyAuth(response);
            return response;
        }

        // ─── Rooms ─────────────────────────────────────────────────────────

        public static async Task<RoomRecord> CreateRoom(string name = "Room", int maxPlayers = 4, bool isPrivate = false)
        {
            EnsureAuthenticated();
            return await RoomManager.CreateRoomAsync(name, maxPlayers, isPrivate);
        }

        public static async Task<RoomRecord> JoinRoom(string roomId, string inviteCode = null)
        {
            EnsureAuthenticated();
            return await RoomManager.JoinRoomAsync(roomId, inviteCode);
        }

        public static async Task<RoomRecord> LeaveRoom(string roomId)
        {
            EnsureAuthenticated();
            return await RoomManager.LeaveRoomAsync(roomId);
        }

        // ─── Turn-based ─────────────────────────────────────────────────────

        /// Poll the server for current room state (opponent's move, game state, etc.).
        public static async Task<RoomRecord> GetRoom(string roomId)
        {
            EnsureAuthenticated();
            return await RoomManager.GetRoomAsync(roomId);
        }

        /// Submit your move/turn. Server validates it's your turn.
        /// state: your game state as key-value pairs, e.g. new Dictionary&lt;string,object&gt;{{"board","e4"}}
        /// nextTurn: optional — override who plays next (default: auto-advance round-robin)
        public static async Task<RoomRecord> SubmitTurn(
            string roomId,
            Dictionary<string, object> state,
            string nextTurn = null)
        {
            EnsureAuthenticated();
            return await RoomManager.SubmitTurnAsync(roomId, state, nextTurn);
        }

        // ─── Internals ─────────────────────────────────────────────────────

        private static void ApplyAuth(AuthResponse response)
        {
            PlayerId    = response.PlayerId;
            Token       = response.Token;
            TokenExpiry = response.ExpiresAt;
            RoomManager.SetToken(response.Token);
        }

        private static void EnsureInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("[MabarSDK] Not initialized. Call Multiplayer.Initialize(settings) first.");
        }

        private static void EnsureAuthenticated()
        {
            EnsureInitialized();
            if (!IsAuthenticated)
                throw new InvalidOperationException("[MabarSDK] Not authenticated. Call Multiplayer.LoginGuest() first.");
        }
    }
}
