using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ESPlus.Storage
{
    public class CustomDictionaryConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToList());
        }

        public override Dictionary<TKey, TValue> ReadJson(JsonReader reader, Type objectType, Dictionary<TKey, TValue> existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            return serializer.Deserialize<KeyValuePair<TKey, TValue>[]>(reader).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}