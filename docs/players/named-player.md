# Named Player

Named player adalah guest dengan nama custom yang kamu tentukan sendiri — tetap tidak butuh akun atau registrasi.

## Cara Pakai

```csharp
Multiplayer.Initialize(settings);
await Multiplayer.Connect("NamaPemain");
```

## Integrasi dengan Sistem Auth Game

Jika game kamu sudah punya sistem login sendiri (misalnya Firebase, PlayFab, atau backend custom), gunakan username dari sistem tersebut sebagai nama player:

```csharp
// Setelah user login di sistem kamu
string username = AuthService.CurrentUser.DisplayName;

Multiplayer.Initialize(settings);
await Multiplayer.Connect(username);

var room = await Multiplayer.CreateRoom("turn_room");
```

## Nama di Room

Nama player akan terlihat oleh semua pemain lain di room:

```csharp
room.On<JObject>("player_joined", data => {
    string nama = data["name"].ToString();
    Debug.Log($"{nama} bergabung!");
});
```

## Aturan Nama

| Aturan | Detail |
|---|---|
| Panjang maksimal | 40 karakter |
| Jika kosong | Diganti `Player_XXXX` (random) |
| Karakter | Semua karakter diperbolehkan |
| Whitespace | Di-trim otomatis (leading/trailing) |

## Ubah Nama Antar Session

`Connect()` bisa dipanggil ulang untuk mengganti nama sebelum masuk room berikutnya:

```csharp
await Multiplayer.Connect("NamaBaru");
var room = await Multiplayer.CreateRoom("mabar_room");
```

> Nama hanya bisa diganti **sebelum** masuk room. Setelah join, nama sudah terikat ke session tersebut.
