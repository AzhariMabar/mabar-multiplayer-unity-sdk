# Player Overview

Mabarin SDK mendukung beberapa tipe player:

| Tipe | Cara | Keterangan |
|---|---|---|
| **Guest** | `Connect()` tanpa nama | Nama random otomatis: `Player_1234` |
| **Named Guest** | `Connect("NamaPemain")` | Nama custom, tidak butuh akun |
| **Project Player** | Custom auth + `Connect()` | Integrasi login game kamu sendiri |

Semua tipe player bisa langsung masuk room tanpa pendaftaran akun. Identifikasi dilakukan lewat **SessionId** yang di-assign server saat join room.

## Alur Umum

```
Initialize(settings)
      ↓
  Connect("Nama")          ← set nama pemain
      ↓
CreateRoom / JoinRoom      ← masuk ke room
      ↓
Send / On                  ← komunikasi in-game
      ↓
   Leave()                 ← keluar room
```

## AppKey & Isolasi Project

Setiap player terikat ke **AppKey** yang di-set di `MultiplayerSettings`. Player dengan AppKey berbeda tidak bisa bertemu satu sama lain, bahkan di room type yang sama.

Lihat halaman berikutnya untuk detail tipe player.
