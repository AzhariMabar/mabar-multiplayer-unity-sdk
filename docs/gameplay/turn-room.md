# Turn-Based Room (turn\_room)

Server enforce giliran — hanya player yang sedang giliran yang bisa broadcast event gameplay.

## Game Flow

```
Host → "start_game"
Server → "game_started" { players, currentTurn, currentTurnName }
                ↓
Current player → "end_turn" { ...payload }
Server → "turn_changed" { prevTurn, currentTurn, currentTurnName, payload }
                ↓
                ... (berulang) ...
                ↓
Any player → "game_over" { winner, ... }
Server → "game_over" (broadcast ke semua)
```

## Setup

```csharp
Multiplayer.Initialize(settings);
await Multiplayer.Connect("Budi");

var room = await Multiplayer.CreateRoom("turn_room");
RegisterTurnEvents(room);
```

## Listen Event

```csharp
void RegisterTurnEvents(MabarinRoom room)
{
    room.On<JObject>("room_joined", data => {
        Debug.Log($"Masuk room, host: {data["hostId"]}");
    });

    room.On<JObject>("player_joined", data => {
        Debug.Log($"{data["name"]} bergabung ({data["playerCount"]} pemain)");
    });

    room.On<JObject>("game_started", data => {
        var players         = data["players"] as JArray;
        string currentTurn  = data["currentTurn"].ToString();
        string currentName  = data["currentTurnName"].ToString();
        bool   isMyTurn     = currentTurn == room.SessionId;

        UpdateTurnIndicator(currentName, isMyTurn);
    });

    room.On<JObject>("turn_changed", data => {
        string currentTurn = data["currentTurn"].ToString();
        string currentName = data["currentTurnName"].ToString();
        bool   isMyTurn    = currentTurn == room.SessionId;

        // payload berisi data yang dikirim player sebelumnya saat end_turn
        var payload = data["payload"];

        UpdateTurnIndicator(currentName, isMyTurn);
        ApplyPreviousMove(payload);
    });

    room.On<JObject>("turn_timeout", data => {
        string skipped     = data["timedOutPlayerName"].ToString();
        string currentName = data["currentTurnName"].ToString();
        Debug.Log($"{skipped} timeout! Giliran {currentName}");
    });

    room.On<JObject>("game_over", data => {
        ShowGameOver(data);
    });
}
```

## Mulai Game (Host Only)

```csharp
// Hanya host yang bisa start
if (isHost)
{
    startButton.onClick.AddListener(async () => {
        await room.Send("start_game");
    });
}
```

Jika bukan host, server akan mengirim error `not_host`.

## Akhiri Giliran

```csharp
async void OnEndTurnClicked()
{
    if (!isMyTurn) return;

    // Kirim state game sebagai payload (opsional)
    await room.Send("end_turn", new {
        move      = selectedMove,
        boardState = GetBoardState()
    });

    isMyTurn = false;
    UpdateUI();
}
```

Payload dari `end_turn` akan diteruskan ke semua player lewat `turn_changed.payload`.

## Kirim Event Gameplay (Saat Giliran)

Saat giliran kamu, kamu bisa kirim event apapun selain `end_turn`:

```csharp
// Hanya valid kalau giliran kamu
await room.Send("place_card", new { cardId = "king_heart", position = 3 });
await room.Send("move_piece", new { from = "e2", to = "e4" });
```

Jika bukan giliran kamu dan kamu kirim event gameplay, server akan mengirim error `not_your_turn`.

## Akhiri Game

```csharp
async void OnGameOver(string winner)
{
    await room.Send("game_over", new {
        winner = winner,
        scores = GetAllScores()
    });
}
```

## Turn Timeout (Opsional)

Set saat create room — server otomatis skip giliran jika player tidak `end_turn` dalam waktu yang ditentukan:

```csharp
// Turn timeout 30 detik
// (Saat ini harus dikonfigurasi di server-side atau lewat opsi custom)
var room = await Multiplayer.CreateRoom("turn_room");
```

> Untuk mengaktifkan turn timeout, server perlu dikonfigurasi dengan `turnTimeoutSec` saat room dibuat. Hubungi admin server.

## Cek Giliran

```csharp
bool _isMyTurn = false;

room.On<JObject>("game_started", data => {
    _isMyTurn = data["currentTurn"].ToString() == room.SessionId;
});

room.On<JObject>("turn_changed", data => {
    _isMyTurn = data["currentTurn"].ToString() == room.SessionId;
    UpdateTurnUI(_isMyTurn);
});
```
