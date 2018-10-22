using System.Text;
using Newtonsoft.Json;

namespace ESPlus
{
    public class JsonIndentedSerializer : ISerializer
    {
        public byte[] Serialize<T>(T graph)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(graph, Formatting.Indented));
        }

        public T Deserialize<T>(byte[] buffer)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(buffer));
        }
    }
}