# API Reference

## Multiplayer (static class)

Entry point utama SDK. Semua method bersifat `static`.

### Properties

| Property | Type | Keterangan |
|---|---|---|
| `IsInitialized` | `bool` | `true` setelah `Initialize()` dipanggil |
| `PlayerName` | `string` | Nama player aktif (di-set via `Connect()`) |

### Methods

#### `Initialize(settings)`

```csharp
public static void Initialize(MultiplayerSettings settings)
```

Inisialisasi SDK dengan settings asset. Harus dipanggil sekali sebelum method lain.

| Parameter | Type | Keterangan |
|---|---|---|
| `settings` | `MultiplayerSettings` | Settings asset dari `Assets → Create → Mabarin → Settings` |

---

#### `Connect(playerName)`

```csharp
public static Task Connect(string playerName = "")
```

Set nama player aktif. Panggil sebelum masuk room.

| Parameter | Type | Default | Keterangan |
|---|---|---|---|
| `playerName` | `string` | `""` | Nama player. Jika kosong → `Player_XXXX` |

---

#### `CreateRoom(roomType)`

```csharp
public static async Task<MabarinRoom> CreateRoom(
    string roomType = "turn_room",
    Dictionary<string, object> options = null
)
```

Buat room baru. Kamu menjadi host.

| Parameter | Type | Default | Keterangan |
|---|---|---|---|
| `roomType` | `string` | `"turn_room"` | `"mabar_room"`, `"turn_room"`, atau `"tetris_room"` |

**Returns:** `MabarinRoom`  
**Throws:** `Exception` jika server error atau timeout

---

#### `JoinRoom(roomId)`

```csharp
public static async Task<MabarinRoom> JoinRoom(
    string roomId,
    Dictionary<string, object> options = null
)
```

Join room yang sudah ada menggunakan Room ID.

| Parameter | Type | Keterangan |
|---|---|---|
| `roomId` | `string` | Room ID dari host |

**Returns:** `MabarinRoom`  
**Throws:** `Exception` jika room tidak ditemukan, AppKey mismatch, atau timeout

---

#### `FindOrCreate(roomType)`

```csharp
public static async Task<MabarinRoom> FindOrCreate(
    string roomType = "turn_room",
    Dictionary<string, object> options = null
)
```

Join room yang tersedia, atau buat baru jika tidak ada. Auto matchmaking.

| Parameter | Type | Default | Keterangan |
|---|---|---|---|
| `roomType` | `string` | `"turn_room"` | Tipe room yang dicari |

**Returns:** `MabarinRoom`  
**Throws:** `Exception` jika server error atau timeout

---

## MabarinRoom (class)

Instance room aktif. Didapat dari `CreateRoom`, `JoinRoom`, atau `FindOrCreate`.

### Properties

| Property | Type | Keterangan |
|---|---|---|
| `RoomId` | `string` | ID unik room |
| `SessionId` | `string` | ID session kamu di room ini |
| `ReconnectionToken` | `string` | Token untuk reconnect |

### Methods

#### `On<T>(type, handler)`

```csharp
public MabarinRoom On<T>(string type, Action<T> handler)
```

Register typed event handler.

| Parameter | Type | Keterangan |
|---|---|---|
| `type` | `string` | Nama event |
| `handler` | `Action<T>` | Callback yang dipanggil saat event diterima |

**Returns:** `MabarinRoom` (untuk chaining)

---

#### `On(type, handler)`

```csharp
public MabarinRoom On(string type, Action<JToken> handler)
```

Register raw event handler (payload sebagai `JToken`).

---

#### `Off(type)`

```csharp
public MabarinRoom Off(string type)
```

Hapus semua handler untuk event `type`.

**Returns:** `MabarinRoom` (untuk chaining)

---

#### `Send(type, data)`

```csharp
public async Task Send(string type, object data = null)
```

Kirim pesan ke server.

| Parameter | Type | Keterangan |
|---|---|---|
| `type` | `string` | Nama event |
| `data` | `object` | Payload (opsional) |

---

#### `Leave()`

```csharp
public async Task Leave()
```

Keluar dari room dan tutup koneksi WebSocket.

---

## MultiplayerSettings (ScriptableObject)

Buat via **Assets → Create → Mabarin → Settings**.

| Field | Type | Default | Keterangan |
|---|---|---|---|
| `ServerUrl` | `string` | `"wss://cloud.mabar.studio"` | URL server WebSocket |
| `AppKey` | `string` | `""` | AppKey project kamu |
