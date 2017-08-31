using Newtonsoft.Json;

namespace ESPlus
{
    public class JsonIndentedSerializer : ISerializer
    {
        public string Serialize<T>(T graph)
        {
            return JsonConvert.SerializeObject(graph, Formatting.Indented);
        }

        public T Deserialize<T>(string buffer)
        {
            return JsonConvert.DeserializeObject<T>(buffer);
        }
    }
}