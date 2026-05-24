using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mabar.Multiplayer.Utils
{
    public class RawJsonConverter : JsonConverter<string>
    {
        public override string ReadJson(JsonReader reader, Type objectType, string existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            return JToken.Load(reader).ToString(Formatting.None);
        }

        public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
        {
            if (value == null) { writer.WriteNull(); return; }
            writer.WriteRawValue(value);
        }
    }
}
