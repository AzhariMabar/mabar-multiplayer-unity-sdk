namespace Mabar.Multiplayer.Models
{
    public class RpcPayload
    {
        public string Event { get; set; }
        public string SenderId { get; set; }
        public string RoomId { get; set; }
        public object Payload { get; set; }
        public long Timestamp { get; set; }
        public string TargetId { get; set; }
    }
}
