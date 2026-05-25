using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Mabar.Multiplayer.Core.Internal
{
    // Minimal msgpack encoder/decoder for Colyseus ROOM_DATA payloads.
    // Encode: any object → byte[] via JToken.FromObject intermediate.
    // Decode: byte[] → JToken (JObject, JArray, or JValue).
    internal static class MsgPack
    {
        // ── Encode ─────────────────────────────────────────────────────────────

        internal static byte[] Encode(object value)
        {
            var bytes = new List<byte>(64);
            JToken jt = (value == null) ? JValue.CreateNull()
                      : (value is JToken j) ? j
                      : JToken.FromObject(value);
            WriteJToken(bytes, jt);
            return bytes.ToArray();
        }

        private static void WriteJToken(List<byte> o, JToken t)
        {
            switch (t.Type)
            {
                case JTokenType.Null:
                case JTokenType.Undefined:
                    o.Add(0xc0); break;

                case JTokenType.Boolean:
                    o.Add(t.Value<bool>() ? (byte)0xc3 : (byte)0xc2); break;

                case JTokenType.Integer:
                    WriteInt64(o, t.Value<long>()); break;

                case JTokenType.Float:
                    WriteFloat64(o, t.Value<double>()); break;

                case JTokenType.String:
                    WriteString(o, t.Value<string>()); break;

                case JTokenType.Bytes:
                    WriteBin(o, t.Value<byte[]>()); break;

                case JTokenType.Array:
                    var arr = (JArray)t;
                    WriteArrayHeader(o, arr.Count);
                    foreach (var item in arr) WriteJToken(o, item);
                    break;

                case JTokenType.Object:
                    var obj = (JObject)t;
                    WriteMapHeader(o, obj.Count);
                    foreach (var kv in obj)
                    {
                        WriteString(o, kv.Key);
                        WriteJToken(o, kv.Value ?? JValue.CreateNull());
                    }
                    break;

                default:
                    o.Add(0xc0); break;
            }
        }

        private static void WriteInt64(List<byte> o, long v)
        {
            if (v >= 0)    { WriteUInt64(o, (ulong)v); return; }
            if (v >= -32)  { o.Add((byte)(v & 0xff)); return; }
            if (v >= -128) { o.Add(0xd0); o.Add((byte)(sbyte)v); return; }
            if (v >= -32768) { o.Add(0xd1); o.Add((byte)((short)v >> 8)); o.Add((byte)(short)v); return; }
            if (v >= -2147483648L)
            {
                o.Add(0xd2);
                o.Add((byte)(v >> 24)); o.Add((byte)(v >> 16));
                o.Add((byte)(v >> 8));  o.Add((byte)v);
                return;
            }
            o.Add(0xd3);
            for (int i = 7; i >= 0; i--) o.Add((byte)(v >> (i * 8)));
        }

        private static void WriteUInt64(List<byte> o, ulong v)
        {
            if (v <= 127)        { o.Add((byte)v); return; }
            if (v <= 0xff)       { o.Add(0xcc); o.Add((byte)v); return; }
            if (v <= 0xffff)     { o.Add(0xcd); o.Add((byte)(v >> 8)); o.Add((byte)v); return; }
            if (v <= 0xffffffff) { o.Add(0xce); for (int i = 3; i >= 0; i--) o.Add((byte)(v >> (i*8))); return; }
            o.Add(0xcf); for (int i = 7; i >= 0; i--) o.Add((byte)(v >> (i*8)));
        }

        private static void WriteFloat64(List<byte> o, double v)
        {
            o.Add(0xcb);
            var b = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            o.AddRange(b);
        }

        private static void WriteString(List<byte> o, string s)
        {
            if (s == null) s = "";
            var utf8 = Encoding.UTF8.GetBytes(s);
            int len = utf8.Length;
            if (len < 32)    { o.Add((byte)(len | 0xa0)); }
            else if (len < 256) { o.Add(0xd9); o.Add((byte)len); }
            else if (len < 65536) { o.Add(0xda); o.Add((byte)(len >> 8)); o.Add((byte)len); }
            else { o.Add(0xdb); for (int i = 3; i >= 0; i--) o.Add((byte)(len >> (i*8))); }
            o.AddRange(utf8);
        }

        private static void WriteBin(List<byte> o, byte[] b)
        {
            if (b == null) { o.Add(0xc0); return; }
            if (b.Length < 256) { o.Add(0xc4); o.Add((byte)b.Length); }
            else { o.Add(0xc5); o.Add((byte)(b.Length >> 8)); o.Add((byte)b.Length); }
            o.AddRange(b);
        }

        private static void WriteArrayHeader(List<byte> o, int n)
        {
            if (n < 16) o.Add((byte)(n | 0x90));
            else if (n < 65536) { o.Add(0xdc); o.Add((byte)(n >> 8)); o.Add((byte)n); }
            else { o.Add(0xdd); for (int i = 3; i >= 0; i--) o.Add((byte)(n >> (i*8))); }
        }

        private static void WriteMapHeader(List<byte> o, int n)
        {
            if (n < 16) o.Add((byte)(n | 0x80));
            else if (n < 65536) { o.Add(0xde); o.Add((byte)(n >> 8)); o.Add((byte)n); }
            else { o.Add(0xdf); for (int i = 3; i >= 0; i--) o.Add((byte)(n >> (i*8))); }
        }

        // ── Decode ─────────────────────────────────────────────────────────────

        internal static JToken Decode(byte[] data, int offset)
        {
            try { return ReadValue(data, ref offset); }
            catch { return JValue.CreateNull(); }
        }

        private static JToken ReadValue(byte[] d, ref int i)
        {
            byte b = d[i++];

            if ((b & 0x80) == 0)    return new JValue((long)b);                 // positive fixint
            if ((b & 0xe0) == 0xe0) return new JValue((long)(sbyte)b);          // negative fixint
            if ((b & 0xe0) == 0xa0) { int n = b & 0x1f; var s = Encoding.UTF8.GetString(d, i, n); i += n; return s; } // fixstr
            if ((b & 0xf0) == 0x90) return ReadArray(d, ref i, b & 0x0f);       // fixarray
            if ((b & 0xf0) == 0x80) return ReadMap(d, ref i, b & 0x0f);         // fixmap

            switch (b)
            {
                case 0xc0: return JValue.CreateNull();
                case 0xc2: return new JValue(false);
                case 0xc3: return new JValue(true);

                case 0xcc: return new JValue((long)d[i++]);
                case 0xcd: { long v = ((long)d[i] << 8) | d[i+1]; i += 2; return new JValue(v); }
                case 0xce: { long v = ((long)d[i]<<24)|((long)d[i+1]<<16)|((long)d[i+2]<<8)|d[i+3]; i+=4; return new JValue(v); }
                case 0xcf: { ulong v=0; for(int k=0;k<8;k++) v=(v<<8)|d[i++]; return new JValue((long)v); }

                case 0xd0: return new JValue((long)(sbyte)d[i++]);
                case 0xd1: { var v=(short)(((short)d[i]<<8)|d[i+1]); i+=2; return new JValue((long)v); }
                case 0xd2: { int v=(d[i]<<24)|(d[i+1]<<16)|(d[i+2]<<8)|d[i+3]; i+=4; return new JValue((long)v); }
                case 0xd3: { long v=0; for(int k=0;k<8;k++) v=(v<<8)|d[i++]; return new JValue(v); }

                case 0xca: { var bb=new byte[4]; Array.Copy(d,i,bb,0,4); i+=4; if(BitConverter.IsLittleEndian) Array.Reverse(bb); return new JValue((double)BitConverter.ToSingle(bb,0)); }
                case 0xcb: { var bb=new byte[8]; Array.Copy(d,i,bb,0,8); i+=8; if(BitConverter.IsLittleEndian) Array.Reverse(bb); return new JValue(BitConverter.ToDouble(bb,0)); }

                case 0xd9: { int n=d[i++]; var s=Encoding.UTF8.GetString(d,i,n); i+=n; return s; }
                case 0xda: { int n=(d[i]<<8)|d[i+1]; i+=2; var s=Encoding.UTF8.GetString(d,i,n); i+=n; return s; }
                case 0xdb: { int n=(d[i]<<24)|(d[i+1]<<16)|(d[i+2]<<8)|d[i+3]; i+=4; var s=Encoding.UTF8.GetString(d,i,n); i+=n; return s; }

                case 0xc4: { int n=d[i++]; var ba=new byte[n]; Array.Copy(d,i,ba,0,n); i+=n; return JToken.FromObject(ba); }
                case 0xc5: { int n=(d[i]<<8)|d[i+1]; i+=2; var ba=new byte[n]; Array.Copy(d,i,ba,0,n); i+=n; return JToken.FromObject(ba); }

                case 0xdc: { int n=(d[i]<<8)|d[i+1]; i+=2; return ReadArray(d,ref i,n); }
                case 0xdd: { int n=(d[i]<<24)|(d[i+1]<<16)|(d[i+2]<<8)|d[i+3]; i+=4; return ReadArray(d,ref i,n); }
                case 0xde: { int n=(d[i]<<8)|d[i+1]; i+=2; return ReadMap(d,ref i,n); }
                case 0xdf: { int n=(d[i]<<24)|(d[i+1]<<16)|(d[i+2]<<8)|d[i+3]; i+=4; return ReadMap(d,ref i,n); }

                default: return JValue.CreateNull();
            }
        }

        private static JArray ReadArray(byte[] d, ref int i, int count)
        {
            var a = new JArray();
            for (int k = 0; k < count; k++) a.Add(ReadValue(d, ref i));
            return a;
        }

        private static JObject ReadMap(byte[] d, ref int i, int count)
        {
            var o = new JObject();
            for (int k = 0; k < count; k++)
            {
                var key = ReadValue(d, ref i).ToString();
                var val = ReadValue(d, ref i);
                o[key] = val;
            }
            return o;
        }
    }
}
