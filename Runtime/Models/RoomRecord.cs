namespace Mabar.Multiplayer.Models
{
    /// <summary>Room listing entry returned by MabarinClient.GetRooms().</summary>
    public class RoomInfo
    {
        public string RoomId     { get; set; }
        public int    Clients    { get; set; }
        public int    MaxClients { get; set; }
        public string Label      { get; set; }
    }
}
