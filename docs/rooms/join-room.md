# Join Room by ID

Bergabung ke room yang sudah ada menggunakan Room ID.

## Syntax

```csharp
MabarinRoom room = await Multiplayer.JoinRoom(roomId);
```

| Parameter | Type | Keterangan |
|---|---|---|
| `roomId` | `string` | Room ID yang didapat dari host |

## Cara Kerja

1. Kamu mendapat Room ID dari host (share via chat, QR code, copy-paste, dll)
2. Panggil `JoinRoom(roomId)` dengan ID tersebut
3. Server memvalidasi AppKey — jika AppKey tidak cocok dengan room, join **ditolak**
4. Kamu masuk room dan mulai terima event

## Contoh

```csharp
[SerializeField] string _roomIdInput; // dari UI input field

async void OnJoinButtonClicked()
{
    Multiplayer.Initialize(settings);
    await Multiplayer.Connect("Ani");

    try
    {
        var room = await Multiplayer.JoinRoom(_roomIdInput.Trim());
        Debug.Log($"Berhasil join room {room.RoomId}");
        RegisterEvents(room);
    }
    catch (Exception e)
    {
        Debug.LogError($"Gagal join: {e.Message}");
    }
}

void RegisterEvents(MabarinRoom room)
{
    room.On<JObject>("room_joined", data => {
        // data berisi roomId, hostId, dan daftar players yang sudah ada
        var players = data["players"] as JArray;
        Debug.Log($"Ada {players.Count} pemain di room");
    });

    room.On<JObject>("player_joined", data => {
        Debug.Log($"{data["name"]} bergabung");
    });
}
```

## Validasi AppKey

`JoinRoom` hanya berhasil jika AppKey-mu sama dengan AppKey yang digunakan host saat membuat room. Ini mencegah pemain dari project berbeda saling bergabung.

```
Project A (AppKey: mk_aaa) → buat room → Room #ABC
Project B (AppKey: mk_bbb) → join Room #ABC → DITOLAK ❌

Project A (AppKey: mk_aaa) → join Room #ABC → DITERIMA ✅
```

## Error yang Mungkin Terjadi

| Error | Penyebab |
|---|---|
| `Invalid or missing AppKey` | AppKey tidak valid |
| `AppKey tidak cocok dengan room ini` | AppKey berbeda dengan host |
| `Game sedang berlangsung` | Room sudah locked (khusus tetris\_room) |
| `Room not found` | Room ID salah atau room sudah tutup |

## Reconnect

Jika kamu disconnect saat game berlangsung, gunakan `JoinRoom` dengan Room ID yang sama untuk reconnect (dalam 30 detik):

```csharp
var room = await Multiplayer.JoinRoom(lastRoomId);
// Server otomatis restore session jika masih dalam window reconnect
```
