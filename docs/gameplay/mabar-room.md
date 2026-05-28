# Relay Room (mabar\_room)

Room serbaguna — server hanya meneruskan (relay) semua pesan ke pemain lain. Game logic sepenuhnya di client.

## Cara Kerja

```
Player A → Send("move", { x:3, y:5 })
Server   → Broadcast ke semua kecuali Player A
Player B,C,D → Terima "move", { x:3, y:5 }
```

## Kapan Dipakai

* Game trivia / kuis (host broadcast soal, semua jawab)
* Party game tanpa urutan ketat
* Chat room / lobby
* Game sederhana di mana client bisa dipercaya
* Prototipe cepat

## Contoh — Chat Room

```csharp
async void Start()
{
    Multiplayer.Initialize(settings);
    await Multiplayer.Connect("Budi");
    var room = await Multiplayer.CreateRoom("mabar_room");

    // Terima chat dari pemain lain
    room.On<JObject>("chat", data => {
        string sender = data["name"].ToString();
        string text   = data["text"].ToString();
        chatLog.text += $"\n[{sender}] {text}";
    });
}

async void SendChat(string text)
{
    await room.Send("chat", new {
        name = Multiplayer.PlayerName,
        text = text
    });
    // Tambahkan ke log lokal sendiri (pesan sendiri tidak di-relay balik)
    chatLog.text += $"\n[Saya] {text}";
}
```

## Contoh — Game Trivia

```csharp
// Host: kirim soal
await room.Send("question", new {
    id   = currentQuestion.Id,
    text = currentQuestion.Text,
    choices = currentQuestion.Choices
});

// Semua player: terima soal
room.On<JObject>("question", data => {
    ShowQuestion(data);
});

// Player: kirim jawaban
await room.Send("answer", new {
    questionId = currentQuestion.Id,
    choice     = selectedAnswer
});

// Host: terima jawaban dari semua player
room.On<JObject>("answer", data => {
    string playerId = data["from"].ToString(); // ID pengirim (dari server context)
    int    choice   = data["choice"].ToObject<int>();
    RecordAnswer(playerId, choice);
});
```

## Catatan Penting

* Pesan **tidak dikirim balik ke pengirim** — add data ke UI lokal sendiri
* Tidak ada validasi gameplay di server — client yang tentukan win/lose
* Semua jenis event bisa dikirim — nama event bebas
* Max 8 pemain per room
