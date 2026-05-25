using System;
using System.Collections.Generic;
using System.Text;

namespace Mabar.Multiplayer.Core.Internal
{
    internal static class ColyseusProtocol
    {
        internal const byte JOIN_ROOM  = 10;
        internal const byte ERROR      = 11;
        internal const byte LEAVE_ROOM = 12;
        internal const byte ROOM_DATA  = 13;

        // Encode string as @colyseus/schema fixstr/str8/str16 for the ROOM_DATA type field.
        internal static void WriteSchemaString(List<byte> bytes, string value)
        {
            if (value == null) value = "";
            var utf8 = Encoding.UTF8.GetBytes(value);
            int len = utf8.Length;
            if (len < 0x20)
            {
                bytes.Add((byte)(len | 0xa0));
            }
            else if (len < 0x100)
            {
                bytes.Add(0xd9);
                bytes.Add((byte)len);
            }
            else
            {
                bytes.Add(0xda);
                bytes.Add((byte)(len >> 8));
                bytes.Add((byte)(len & 0xff));
            }
            bytes.AddRange(utf8);
        }

        // Decode @colyseus/schema fixstr/str8/str16 from ROOM_DATA type field.
        internal static string ReadSchemaString(byte[] data, ref int offset)
        {
            byte tag = data[offset];
            int len;
            if ((tag & 0xe0) == 0xa0)      { len = tag & 0x1f; offset += 1; }
            else if (tag == 0xd9)           { len = data[offset + 1]; offset += 2; }
            else if (tag == 0xda)           { len = (data[offset + 1] << 8) | data[offset + 2]; offset += 3; }
            else throw new Exception($"[Mabarin] Unknown schema string tag: 0x{tag:X2}");
            var s = Encoding.UTF8.GetString(data, offset, len);
            offset += len;
            return s;
        }

        // Decode Colyseus length-prefixed string: [1-byte-byteLen][utf8-bytes].
        // Used in the JOIN_ROOM server message (reconnectionToken, serializerId).
        internal static string ReadColyseusString(byte[] data, ref int offset)
        {
            if (offset >= data.Length) return "";
            int len = data[offset++];
            if (offset + len > data.Length) len = data.Length - offset;
            var s = Encoding.UTF8.GetString(data, offset, len);
            offset += len;
            return s;
        }
    }
}
