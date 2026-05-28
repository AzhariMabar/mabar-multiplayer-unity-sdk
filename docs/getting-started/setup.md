# Setup & AppKey

## Buat Settings Asset

1. Di Project window, klik kanan → **Create → Mabarin → Settings**
2. Beri nama file (misal: `MabarinSettings`)
3. Simpan di folder `Assets/Settings/` atau folder manapun

## Konfigurasi

Pilih asset yang baru dibuat, isi di Inspector:

| Field | Nilai | Keterangan |
|---|---|---|
| **Server URL** | `wss://cloud.mabar.studio` | URL server production |
| **App Key** | `mk_xxxxxxxx...` | Key unik tiap project |

Untuk development lokal, ganti Server URL ke `ws://localhost:2567`.

## Apa itu AppKey?

AppKey adalah **identifier unik per project game**. Fungsinya:

* Memastikan room antar project tidak bisa saling join
* Memisahkan matchmaking pool per game
* Mengautentikasi koneksi ke server

> **Penting:** Setiap game/project harus punya AppKey sendiri. Player dari game A tidak akan pernah bertemu player dari game B, bahkan jika mereka pakai room type yang sama.

## Mendapatkan AppKey

Hubungi tim Mabarin untuk mendapatkan AppKey project kamu. Untuk development, gunakan key `dev` (hanya untuk local server).

## Attach ke Script

```csharp
public class GameManager : MonoBehaviour
{
    [Header("SDK")]
    public MultiplayerSettings Settings; // drag asset ke sini

    void Start()
    {
        Multiplayer.Initialize(Settings);
    }
}
```

Drag asset `MabarinSettings` dari Project window ke field **Settings** di Inspector.
