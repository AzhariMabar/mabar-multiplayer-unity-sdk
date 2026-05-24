using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mabar.Multiplayer.Utils
{
    public static class JsonUtil
    {
        private static readonly JsonSerializerSettings ReadSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        private static readonly JsonSerializerSettings WriteSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static T Deserialize<T>(string json) =>
            JsonConvert.DeserializeObject<T>(json, ReadSettings);

        public static string Serialize(object value) =>
            JsonConvert.SerializeObject(value, WriteSettings);
    }
}
