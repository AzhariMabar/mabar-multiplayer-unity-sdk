# Create Room

Buat room baru — kamu otomatis menjadi **host** room tersebut.

## Syntax

```csharp
MabarinRoom room = await Multiplayer.CreateRoom(roomType);
```

| Parameter | Type | Default | Keterangan |
|---|---|---|---|
| `roomType` | `string` | `"turn_room"` | Tipe room yang dibuat |

## Contoh

### Buat Relay Room

```csharp
Multiplayer.Initialize(settings);
await Multiplayer.Connect("Budi");

var room = await Multiplayer.CreateRoom("mabar_room");
Debug.Log($"Room dibuat: {room.RoomId}");

// Bagikan room.RoomId ke pemain lain untuk join
```

### Buat Turn-Based Room

```csharp
var room = await Multiplayer.CreateRoom("turn_room");
```

### Buat 1v1 Battle Room

```csharp
var room = await Multiplayer.CreateRoom("tetris_room");
```

## Room ID

Setelah berhasil, `room.RoomId` berisi ID unik room. Bagikan ID ini ke pemain lain agar bisa join.

```csharp
// Copy ke clipboard
GUIUtility.systemCopyBuffer = room.RoomId;

// Tampilkan di UI
roomIdText.text = room.RoomId;
```

## Event Setelah Create

Server akan mengirim event `room_joined` otomatis saat kamu berhasil masuk room:

```csharp
room.On<JObject>("room_joined", data => {
    string roomId  = data["roomId"].ToString();
    string hostId  = data["hostId"].ToString();
    var    players = data["players"] as JArray;
    Debug.Log($"Masuk room {roomId} sebagai host");
});
```

## Error Handling

```csharp
try
{
    var room = await Multiplayer.CreateRoom("mabar_room");
}
catch (Exception e)
{
    Debug.LogError($"Gagal buat room: {e.Message}");
}
```

Penyebab error umum:

| Error | Penyebab |
|---|---|
| `Invalid or missing AppKey` | AppKey tidak valid |
| `Join timeout` | Server tidak merespons dalam 10 detik |
| Connection refused | Server URL salah atau server mati |
