// Room management in Mabarin SDK is handled via MabarinClient static class.
//
//   await MabarinClient.Connect("PlayerName");
//   var room = await MabarinClient.CreateRoom("mabar_room");      // buat room baru
//   var room = await MabarinClient.JoinRoom("ROOM_ID");           // join by ID
//   var room = await MabarinClient.JoinOrCreate("mabar_room");    // quick-match
//   var rooms = await MabarinClient.GetRooms("mabar_room");       // list rooms
