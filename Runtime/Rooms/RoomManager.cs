// RoomManager is intentionally empty.
//
// Room management in Mabarin SDK is entirely WebSocket-based via Colyseus.
// Use the Multiplayer static class instead:
//
//   await Multiplayer.Connect("PlayerName");
//   var room = await Multiplayer.CreateRoom("turn_room");   // create
//   var room = await Multiplayer.JoinRoom("ROOM_ID");       // join by ID
//   var room = await Multiplayer.FindOrCreate("turn_room"); // quick-match
//
// There are no REST endpoints for room create/join/leave.
