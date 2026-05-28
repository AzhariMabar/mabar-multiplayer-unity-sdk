# 1v1 Battle Room (tetris\_room)

Room privat untuk dua pemain dengan state machine lengkap. Dirancang untuk game real-time 1v1.

## Phases

```
lobby → countdown (3-2-1) → playing → score
  ↑                                      |
  └──────────── play_again ──────────────┘
```

## Setup

```csharp
Multiplayer.Initialize(settings);
await Multiplayer.Connect("Budi");

// Buat room (share RoomId ke lawan)
var room = await Multiplayer.CreateRoom("tetris_room");
// atau Join (jika punya Room ID dari lawan)
var room = await Multiplayer.JoinRoom(roomId);

RegisterBattleEvents(room);
```

> `tetris_room` tidak mendukung auto matchmaking (`FindOrCreate`). Room ini bersifat private — share Room ID secara manual ke lawan.

## Lobby Phase

```csharp
void RegisterBattleEvents(MabarinRoom room)
{
    room.On<JObject>("room_joined", data => {
        Debug.Log($"Masuk room. Menunggu lawan...");
        ShowLobby(room.RoomId);
    });

    room.On<JObject>("lobby_update", data => {
        var players = data["players"] as JArray;
        UpdateLobbyUI(players);
        // Tampilkan siapa yang sudah ready
    });
```

## Toggle Ready

```csharp
    readyButton.onClick.AddListener(async () => {
        await room.Send("toggle_ready");
    });
```

Saat kedua pemain ready, countdown otomatis dimulai.

## Countdown Phase

```csharp
    room.On<JObject>("countdown", data => {
        int v = data["v"].ToObject<int>();
        countdownText.text = v.ToString(); // 3... 2... 1...
    });
```

## Playing Phase

```csharp
    room.On<JObject>("round_start", data => {
        int round = data["round"].ToObject<int>();
        StartGameplay(round);
    });

    // Terima board lawan
    room.On<JObject>("board_sync", data => {
        string fromId = data["id"].ToString();
        var    board  = data["board"];
        int    score  = data["score"]?.ToObject<int>() ?? 0;
        UpdateOpponentDisplay(board, score);
    });

    // Terima garbage dari lawan
    room.On<JObject>("incoming_garbage", data => {
        int lines = data["n"].ToObject<int>();
        AddGarbageLines(lines);
    });
```

## Kirim Update Board

```csharp
// Kirim state board kamu ke lawan (real-time)
async void OnBoardChanged()
{
    await room.Send("board_update", new {
        board = GetBoardArray(),     // (string|null)[][]
        score = currentScore,
        lines = totalLines,
        level = currentLevel,
        piece = new {
            shape = currentPiece.Shape,
            x     = currentPiece.X,
            y     = currentPiece.Y,
            color = currentPiece.Color
        }
    });
}

// Kirim garbage ke lawan saat kamu clear banyak line
async void OnLinesCleared(int count)
{
    int garbageLines = CalculateGarbage(count);
    if (garbageLines > 0)
        await room.Send("garbage", new { n = garbageLines });
}

// Kasih tau server kalau kamu kalah
async void OnGameOver()
{
    await room.Send("game_over", new { score = finalScore });
}
```

## Score Phase

```csharp
    room.On<JObject>("player_out", data => {
        string name = data["name"].ToString();
        Debug.Log($"{name} kalah!");
    });

    room.On<JObject>("round_over", data => {
        int round = data["round"].ToObject<int>();
        var winner = data["winner"]; // null jika draw
        ShowRoundResult(round, winner);
    });
```

## Main Lagi / Quit

```csharp
    // Vote main lagi
    playAgainButton.onClick.AddListener(async () => {
        await room.Send("play_again");
    });

    room.On<JObject>("play_again_vote", data => {
        string name = data["name"].ToString();
        Debug.Log($"{name} mau main lagi");
    });

    // Keluar
    quitButton.onClick.AddListener(async () => {
        await room.Send("quit");
    });

    room.On<JObject>("room_closed", _ => {
        Debug.Log("Room ditutup");
        GoToMainMenu();
    });
} // end RegisterBattleEvents
```

## Disconnect & Reconnect

Saat `playing`, jika salah satu pemain disconnect:
* Server menunggu **30 detik** untuk reconnect
* Jika tidak reconnect → dianggap forfeit, lawan menang ronde ini

```csharp
room.On<JObject>("player_disconnected", data => {
    Debug.Log($"{data["name"]} disconnect! Menunggu reconnect...");
});

room.On<JObject>("player_reconnected", data => {
    Debug.Log($"{data["name"]} kembali!");
});

room.On<JObject>("player_forfeit", data => {
    Debug.Log($"{data["name"]} forfeit karena tidak kembali dalam 30 detik");
});
```

Untuk reconnect, panggil `JoinRoom` dengan Room ID yang sama:

```csharp
var room = await Multiplayer.JoinRoom(lastRoomId);
```
