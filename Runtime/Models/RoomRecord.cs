// RoomRecord is no longer used.
//
// Room state in Mabarin SDK is delivered via WebSocket events, not REST.
// Listen to server events using room.On("event_name", handler):
//
//   room.On("room_joined",   data => { string roomId = data["roomId"].ToString(); });
//   room.On("player_joined", data => { string name   = data["name"].ToString(); });
//   room.On("turn_changed",  data => { string turn   = data["currentTurn"].ToString(); });
//   room.On("game_over",     data => { /* handle result */ });
