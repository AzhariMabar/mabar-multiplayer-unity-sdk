# Mabarin Multiplayer SDK

**Mabarin SDK** adalah Unity multiplayer SDK berbasis WebSocket yang tidak memerlukan dependency eksternal. Satu git URL, langsung siap pakai.

## Fitur

| Fitur | Keterangan |
|---|---|
| Zero dependencies | Tidak perlu install SDK lain — semua built-in |
| AppKey isolation | Room antar project tidak bisa saling join |
| Guest support | Tidak butuh login — langsung main |
| Relay room | Broadcast semua event ke pemain lain |
| Turn-based room | Server enforce giliran, auto-timeout |
| 1v1 Battle room | Lobby, countdown, reconnect, score |
| Cross-platform | Unity ↔ Web via protokol yang sama |

## Quick Start

```csharp
// 1. Inisialisasi
Multiplayer.Initialize(settings);

// 2. Set nama pemain (atau skip untuk guest)
await Multiplayer.Connect("NamaPemain");

// 3. Buat atau join room
var room = await Multiplayer.CreateRoom("mabar_room");

// 4. Listen event
room.On<JObject>("chat", data => {
    Debug.Log(data["text"]);
});

// 5. Kirim pesan
await room.Send("chat", new { name = "Budi", text = "Halo!" });
```

## Room Types

| Room | Deskripsi | Max Players |
|---|---|---|
| `mabar_room` | Relay umum — semua event di-broadcast | 8 |
| `turn_room` | Turn-based — server enforce giliran | 4 |
| `tetris_room` | 1v1 battle dengan fase lobby/play/score | 2 |

## Links

* [GitHub](https://github.com/AzhariMabar/mabar-multiplayer-unity-sdk)
* [Server](https://cloud.mabar.studio)
