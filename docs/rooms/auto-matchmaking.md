# Auto Matchmaking

Matchmaking otomatis — bergabung ke room yang tersedia, atau buat room baru jika tidak ada.

## Syntax

```csharp
MabarinRoom room = await Multiplayer.FindOrCreate(roomType);
```

| Parameter | Type | Default | Keterangan |
|---|---|---|---|
| `roomType` | `string` | `"turn_room"` | Tipe room yang dicari |

## Cara Kerja

```
FindOrCreate("turn_room")
        ↓
Ada room tersedia      → Join room itu
Tidak ada room         → Buat room baru
Room AppKey berbeda    → Tidak ikut (isolasi project)
```

Server mencari room yang:
* Tipe sama (`turn_room`, `mabar_room`, dll)
* AppKey sama dengan AppKey-mu
* Masih bisa menerima pemain baru (belum penuh, belum locked)

## Contoh — Matchmaking Sederhana

```csharp
async void QuickPlay()
{
    Multiplayer.Initialize(settings);
    await Multiplayer.Connect("Budi");

    // Cari room, atau buat kalau tidak ada
    var room = await Multiplayer.FindOrCreate("turn_room");

    Debug.Log($"Masuk room: {room.RoomId}");

    room.On<JObject>("room_joined", data => {
        Debug.Log("Berhasil masuk!");
    });

    room.On<JObject>("player_joined", data => {
        Debug.Log($"{data["name"]} ikut bergabung");
    });
}
```

## Contoh — Matchmaking dengan Loading Screen

```csharp
async void StartMatchmaking()
{
    ShowLoadingScreen("Mencari lawan...");

    try
    {
        Multiplayer.Initialize(settings);
        await Multiplayer.Connect(playerName);

        var room = await Multiplayer.FindOrCreate("turn_room");

        room.On<JObject>("player_joined", data => {
            // Lawan ditemukan — mulai game
            HideLoadingScreen();
            StartGame(room);
        });

        // Kalau kamu yang host, tunggu lawan masuk
        if (IsHost(room)) ShowWaitingScreen(room.RoomId);
        else               HideLoadingScreen();
    }
    catch (Exception e)
    {
        HideLoadingScreen();
        ShowError(e.Message);
    }
}
```

## Perbedaan dengan CreateRoom dan JoinRoom

| Method | Kapan Dipakai |
|---|---|
| `CreateRoom` | Kamu yang buat room, share ID ke teman |
| `JoinRoom(id)` | Kamu punya Room ID dari teman/host |
| `FindOrCreate` | Quick play / random matchmaking |

## Catatan Penting

* `tetris_room` **tidak mendukung** `FindOrCreate` karena room ini bersifat private
* Matchmaking pool terpisah per AppKey — player dari project berbeda tidak bisa ketemu
* Jika semua room penuh, server akan membuat room baru secara otomatis
