using System;
using System.Text;
using Newtonsoft.Json;

namespace ESPlus
{
    public class EventJsonSerializer : IEventSerializer
    {
        public byte[] Serialize<T>(T graph)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(graph, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
            }));
        }

        public object Deserialize(Type type, byte[] buffer)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(buffer), type, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
            });
        }
    }
}