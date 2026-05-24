namespace Mabar.Multiplayer.Models
{
    public class RoomRecord
    {
        public string   Id            { get; set; }
        public string   HostId        { get; set; }
        public string   Name          { get; set; }
        public int      MaxPlayers    { get; set; }
        public bool     IsPrivate     { get; set; }
        public string   InviteCode    { get; set; }
        public string[] Players       { get; set; }
        public long     CreatedAt     { get; set; }
        public long     UpdatedAt     { get; set; }
        public string   CurrentTurn   { get; set; }
        public long     TurnStartedAt { get; set; }

        // Raw JSON string — deserialize to your own struct:
        // JsonUtility.FromJson<MyState>(room.State)
        public string   State         { get; set; }
    }
}
