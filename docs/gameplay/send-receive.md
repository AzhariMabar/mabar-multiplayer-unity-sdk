# Send & Receive Messages

Komunikasi antara client dan server dilakukan lewat **event messages** berbasis string type.

## Mengirim Pesan

```csharp
await room.Send(type, data);
```

| Parameter | Type | Keterangan |
|---|---|---|
| `type` | `string` | Nama event |
| `data` | `object` | Payload (opsional) — bisa anonymous object, Dictionary, atau class apapun |

### Contoh Send

```csharp
// Data sederhana
await room.Send("move", new { x = 3, y = 5 });

// Data kompleks
await room.Send("update_state", new {
    position = new { x = 1.5f, y = 0f, z = 3.2f },
    health   = 100,
    items    = new[] { "sword", "shield" }
});

// Tanpa data
await room.Send("ping");

// Pakai Dictionary
await room.Send("custom", new Dictionary<string, object> {
    { "action", "attack" },
    { "target", "player_xyz" }
});
```

## Menerima Event

```csharp
room.On<T>(type, handler);      // typed — auto deserialize
room.On(type, handler);          // raw JToken
```

### Typed Handler (Recommended)

```csharp
// Pakai JObject untuk akses field dinamis
room.On<JObject>("move", data => {
    int x = data["x"].ToObject<int>();
    int y = data["y"].ToObject<int>();
    MovePlayer(x, y);
});

// Pakai class custom
room.On<MoveData>("move", data => {
    MovePlayer(data.X, data.Y);
});

[System.Serializable]
public class MoveData {
    public int X;
    public int Y;
}
```

### Raw Handler

```csharp
room.On("move", jtoken => {
    Debug.Log(jtoken.ToString());
});
```

## Chaining Handler

`On` mengembalikan `MabarinRoom` sehingga bisa di-chain:

```csharp
room
    .On<JObject>("player_joined", OnPlayerJoined)
    .On<JObject>("player_left",   OnPlayerLeft)
    .On<JObject>("move",          OnMove)
    .On<JObject>("game_over",     OnGameOver);
```

## Hapus Handler

```csharp
room.Off("move"); // hapus semua handler untuk event "move"
```

## Event Sistem Bawaan

Selain event game kamu, room selalu mengirim event sistem ini:

| Event | Kapan | Data |
|---|---|---|
| `room_joined` | Saat kamu berhasil join | `{ roomId, hostId, players[] }` |
| `player_joined` | Pemain lain masuk | `{ id, name, playerCount }` |
| `player_left` | Pemain lain keluar | `{ id, name, consented }` |
| `disconnected` | Koneksi ke server putus | — |
| `error` | Server error | — |

```csharp
room.On<JObject>("disconnected", _ => {
    Debug.Log("Terputus dari server");
    ShowReconnectButton();
});
```

## Thread Safety

Semua handler dipanggil di **Unity main thread** — aman untuk akses GameObject, UI, dan MonoBehaviour langsung dari dalam handler.

```csharp
room.On<JObject>("score_update", data => {
    // Langsung aman untuk update UI
    scoreText.text = data["score"].ToString();
});
```
