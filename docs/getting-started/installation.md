# Installation

## Syarat

* Unity **2021.3 LTS** atau lebih baru
* .NET Standard 2.1 / .NET 4.x

## Install via Package Manager (Recommended)

1. Buka **Window → Package Manager**
2. Klik tombol **+** di pojok kiri atas
3. Pilih **Add package from git URL...**
4. Masukkan URL berikut:

```
https://github.com/AzhariMabar/mabar-multiplayer-unity-sdk.git
```

5. Klik **Add** — Unity akan otomatis mengunduh dan menginstall package.

## Install via manifest.json (Alternative)

Edit file `Packages/manifest.json` dan tambahkan:

```json
{
  "dependencies": {
    "com.mabar.multiplayer": "https://github.com/AzhariMabar/mabar-multiplayer-unity-sdk.git",
    ...
  }
}
```

## Verifikasi

Setelah install, pastikan package terlihat di **Package Manager** dengan nama **Mabarin Multiplayer SDK**.

Tidak ada package lain yang perlu diinstall — SDK ini self-contained.

## Samples (Opsional)

Di Package Manager, pilih **Mabarin Multiplayer SDK → Samples** lalu import:

| Sample | Keterangan |
|---|---|
| **Chat Room** | Cross-platform chat Unity ↔ Web |
| **Tic Tac Toe** | 1v1 turn-based via relay room |
| **Turn Base Demo** | Contoh lengkap `turn_room` |
