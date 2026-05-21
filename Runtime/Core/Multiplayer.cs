using System;
using System.Threading.Tasks;
using Mabar.Multiplayer.Auth;
using Mabar.Multiplayer.Models;
using Mabar.Multiplayer.Realtime;
using Mabar.Multiplayer.RPC;
using Mabar.Multiplayer.Rooms;
using UnityEngine;

namespace Mabar.Multiplayer.Core
{
    public static class Multiplayer
    {
        private static MultiplayerSettings settings;
        private static RealtimeClient realtimeClient;
        private static RpcClient rpcClient;

        public static bool IsReady => realtimeClient != null && realtimeClient.IsConnected;

        public static void Initialize(MultiplayerSettings configuration)
        {
            settings = configuration;
            realtimeClient = new RealtimeClient(configuration.WsUrl, configuration.ApiUrl);
            rpcClient = new RpcClient(realtimeClient);
            RoomManager.Initialize(configuration.ApiUrl, realtimeClient);
        }

        public static async Task Connect()
        {
            EnsureInitialized();
            await realtimeClient.ConnectAsync();
        }

        public static async Task<AuthResponse> LoginGuest()
        {
            EnsureInitialized();
            var auth = new GuestAuth(settings.ApiUrl);
            var response = await auth.LoginGuestAsync();
            realtimeClient.SetPlayerId(response.PlayerId);
            return response;
        }

        public static async Task CreateRoom(string name = "Casual Room", int maxPlayers = 4, bool isPrivate = false)
        {
            EnsureInitialized();
            await RoomManager.CreateRoomAsync(name, maxPlayers, isPrivate);
        }

        public static async Task JoinRoom(string roomId)
        {
            EnsureInitialized();
            await RoomManager.JoinRoomAsync(roomId);
            realtimeClient.SetRoomId(roomId);
        }

        public static async Task LeaveRoom(string roomId)
        {
            EnsureInitialized();
            await RoomManager.LeaveRoomAsync(roomId);
            realtimeClient.ClearRoomId();
        }

        public static void OnRpc(string eventName, Action<RpcPayload> callback)
        {
            EnsureInitialized();
            rpcClient.On(eventName, callback);
        }

        public static Task SendRpc(string eventName, object payload)
        {
            EnsureInitialized();
            return rpcClient.SendAsync(eventName, payload);
        }

        private static void EnsureInitialized()
        {
            if (settings == null || realtimeClient == null || rpcClient == null)
            {
                throw new InvalidOperationException("Multiplayer SDK is not initialized. Call Multiplayer.Initialize(settings) before use.");
            }
        }
    }
}
