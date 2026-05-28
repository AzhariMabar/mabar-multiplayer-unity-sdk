# Guest Player

Guest player adalah cara paling cepat untuk mulai bermain — tidak butuh akun, tidak butuh login.

## Cara Pakai

### Tanpa Nama (Full Guest)

```csharp
Multiplayer.Initialize(settings);
await Multiplayer.Connect(); // nama otomatis: Player_4821
```

Server akan assign nama random dalam format `Player_XXXX`.

### Guest dengan Nama Custom

```csharp
Multiplayer.Initialize(settings);
await Multiplayer.Connect("Budi");
```

Nama akan di-trim otomatis maksimal 40 karakter.

## Cek Nama Aktif

```csharp
Debug.Log(Multiplayer.PlayerName); // nama yang sedang aktif
```

## Contoh Lengkap

```csharp
using UnityEngine;
using Mabar.Multiplayer.Core;
using Mabar.Multiplayer.Models;
using Newtonsoft.Json.Linq;

public class QuickStart : MonoBehaviour
{
    public MultiplayerSettings Settings;

    async void Start()
    {
        // Init & connect sebagai guest
        Multiplayer.Initialize(Settings);
        await Multiplayer.Connect(); // atau Connect("NamaKamu")

        // Langsung buat room
        var room = await Multiplayer.CreateRoom("mabar_room");

        Debug.Log($"Room: {room.RoomId}, Player: {Multiplayer.PlayerName}");
    }
}
```

## Catatan

* Guest tidak punya persistent ID — setiap sesi menghasilkan SessionId baru
* Jika player disconnect saat game berlangsung, ada **30 detik** untuk reconnect sebelum dianggap keluar
* Untuk reconnect, gunakan `JoinRoom(roomId)` dengan Room ID yang sama
