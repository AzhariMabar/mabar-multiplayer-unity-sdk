# Server Events Reference

Daftar semua event yang dikirim server ke client.

## System Events (Semua Room)

| Event | Kapan | Payload |
|---|---|---|
| `room_joined` | Saat kamu berhasil join | `{ roomId, hostId, players[] }` |
| `player_joined` | Pemain lain masuk room | `{ id, name, playerCount }` |
| `player_left` | Pemain lain keluar | `{ id, name, consented }` |
| `host_changed` | Host pindah (host lama keluar) | `{ hostId }` |
| `disconnected` | Koneksi WebSocket putus | — |
| `error` | Error dari server | — |

### Struktur `players[]`

```json
[
  {
    "id": "sessionId",
    "name": "Budi",
    "connected": true,
    "turnIndex": 0
  }
]
```

---

## mabar\_room Events

`mabar_room` bersifat relay — tidak ada event khusus dari server selain system events.

Semua event yang kamu `Send` akan di-broadcast ke pemain lain dengan nama event yang sama.

---

## turn\_room Events

### `game_started`

Dikirim ke semua pemain saat host mengirim `start_game`.

```json
{
  "players": [...],
  "currentTurn": "sessionId-pemain-pertama",
  "currentTurnName": "Budi",
  "turnIndex": 0
}
```

### `turn_changed`

Dikirim ke semua saat giliran berganti.

```json
{
  "prevTurn": "sessionId-sebelumnya",
  "prevTurnName": "Budi",
  "currentTurn": "sessionId-sekarang",
  "currentTurnName": "Ani",
  "turnIndex": 1,
  "payload": { ...data dari end_turn... }
}
```

### `turn_timeout`

Dikirim jika turn timeout aktif dan player tidak end_turn tepat waktu.

```json
{
  "timedOutPlayer": "sessionId",
  "timedOutPlayerName": "Budi",
  "currentTurn": "sessionId-berikutnya",
  "currentTurnName": "Ani",
  "turnIndex": 1
}
```

### `game_over`

Dikirim saat salah satu player mengirim `game_over`. Payload adalah data yang dikirim player tersebut.

### `error`

```json
{ "code": "not_your_turn", "message": "Not your turn" }
{ "code": "not_host",      "message": "Only the host can start the game" }
{ "code": "already_started", "message": "Game already started" }
```

---

## tetris\_room Events

### Lobby Phase

| Event | Payload | Keterangan |
|---|---|---|
| `room_joined` | `{ roomId, phase, round, players[] }` | Kamu masuk room |
| `lobby_update` | `{ phase, round, players[] }` | Ada perubahan ready status |
| `player_disconnected` | `{ id, name }` | Pemain disconnect |
| `player_left` | `{ id, name }` | Pemain keluar (consented) |

### Countdown Phase

| Event | Payload | Keterangan |
|---|---|---|
| `countdown` | `{ v: 3 \| 2 \| 1 \| 0 }` | Tick countdown |

### Playing Phase

| Event | Payload | Keterangan |
|---|---|---|
| `round_start` | `{ round, players[] }` | Ronde dimulai |
| `board_sync` | `{ id, board, score, lines, level, piece }` | Update board lawan |
| `incoming_garbage` | `{ from, n }` | Garbage lines masuk |
| `player_out` | `{ id, name, score }` | Pemain kalah ronde ini |
| `player_reconnected` | `{ id, name }` | Pemain reconnect |
| `player_forfeit` | `{ id, name }` | Pemain forfeit (timeout reconnect) |

### Score Phase

| Event | Payload | Keterangan |
|---|---|---|
| `round_over` | `{ round, winner: { id, name } \| null, scores[] }` | Hasil ronde |
| `play_again_vote` | `{ id, name }` | Pemain vote main lagi |
| `lobby_reset` | `{ phase, round, players[] }` | Kembali ke lobby |
| `player_quit` | `{ id, name }` | Pemain quit |
| `room_closed` | `{}` | Room ditutup |

---

## Client → Server Messages

### turn\_room

| Message | Siapa | Payload | Keterangan |
|---|---|---|---|
| `start_game` | Host only | — | Mulai game |
| `end_turn` | Current player | any | Akhiri giliran, teruskan payload |
| `game_over` | Any player | any | Akhiri game |

### tetris\_room

| Message | Kapan | Payload | Keterangan |
|---|---|---|---|
| `toggle_ready` | Lobby | — | Toggle ready status |
| `board_update` | Playing | `{ board, score?, lines?, level?, piece? }` | Sync board ke lawan |
| `garbage` | Playing | `{ n }` | Kirim garbage ke lawan |
| `game_over` | Playing | `{ score? }` | Kamu kalah |
| `play_again` | Score | — | Vote main lagi |
| `quit` | Any | — | Keluar dan tutup room |
