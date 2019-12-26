using System;
using System.Text;
using ESPlus.Misc;
using Newtonsoft.Json;

namespace ESPlus
{
    public class EventJsonSerializer : IEventSerializer
    {
        private readonly IEventTypeResolver _typeResolver;

        public EventJsonSerializer(IEventTypeResolver typeResolver)
        {
            _typeResolver = typeResolver;
        }

        public byte[] Serialize<T>(T graph)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(graph, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
            }));
        }

        public object Deserialize(string eventType, byte[] buffer)
        {
            var type = _typeResolver.ResolveType(eventType);
            
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(buffer), type, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
            });
        }
    }
}