using System;
using System.Threading.Tasks;
using Mabar.Multiplayer.Core;
using Mabar.Multiplayer.Models;
using UnityEngine;

public class TurnBasedSample : MonoBehaviour
{
    public MultiplayerSettings Settings;

    private async void Start()
    {
        Multiplayer.Initialize(Settings);
        await Multiplayer.Connect();
        var auth = await Multiplayer.LoginGuest();
        Debug.Log($"Guest logged in: {auth.PlayerId}");

        await Multiplayer.CreateRoom("Chess Match", 2, false);
        Multiplayer.OnRpc("move", OnMoveReceived);
    }

    private void OnMoveReceived(RpcPayload payload)
    {
        Debug.Log($"Move event received from {payload.SenderId}: {JsonUtility.ToJson(payload.Payload)}");
    }

    public async Task SendMove(object moveData)
    {
        await Multiplayer.SendRpc("move", moveData);
    }
}
