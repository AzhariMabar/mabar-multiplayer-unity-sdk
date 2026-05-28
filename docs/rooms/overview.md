# Room Types

Mabarin SDK menyediakan tiga tipe room bawaan:

## mabar\_room — Relay Room

Room serbaguna untuk semua jenis game. Semua pesan yang dikirim satu player akan di-relay (broadcast) ke semua player lain di room. Game logic sepenuhnya di client.

**Cocok untuk:** trivia, party game, chat, game host-driven, social room.

```csharp
var room = await Multiplayer.CreateRoom("mabar_room");
await room.Send("custom_event", new { key = "value" });
```

| Property | Value |
|---|---|
| Max players | 8 |
| Server logic | Hanya relay — tidak ada validasi gameplay |
| Reconnect window | 30 detik |

---

## turn\_room — Turn-Based Room

Room dengan sistem giliran yang di-enforce server. Server tahu siapa yang sedang giliran dan hanya menerima event gameplay dari player tersebut.

**Cocok untuk:** domino, kartu, board game, catur.

```csharp
var room = await Multiplayer.CreateRoom("turn_room");
// Host mengirim start_game untuk mulai
await room.Send("start_game");
```

| Property | Value |
|---|---|
| Max players | 4 |
| Turn timeout | Opsional (set saat buat room) |
| Server logic | Enforce giliran, auto-advance saat timeout |
| Reconnect window | 30 detik |

---

## tetris\_room — 1v1 Battle Room

Room privat untuk dua pemain dengan state machine lengkap: lobby → countdown → playing → score.

**Cocok untuk:** game 1v1 real-time seperti Tetris, fighting, puzzle battle.

```csharp
var room = await Multiplayer.CreateRoom("tetris_room");
```

| Property | Value |
|---|---|
| Max players | 2 |
| Visibility | Private (tidak muncul di listing publik) |
| Phases | lobby → countdown → playing → score |
| Reconnect window | 30 detik (selama phase playing) |

---

## Perbandingan

| Fitur | mabar\_room | turn\_room | tetris\_room |
|---|---|---|---|
| Max players | 8 | 4 | 2 |
| Server enforce turn | Tidak | Ya | Tidak |
| Game phases | Tidak | waiting/playing/done | lobby/countdown/playing/score |
| Reconnect | Ya (30s) | Ya (30s) | Ya (30s, saat playing) |
| Auto matchmaking | Ya | Ya | Tidak (private) |
