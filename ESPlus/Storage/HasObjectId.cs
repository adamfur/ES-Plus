using Newtonsoft.Json;

namespace ESPlus.Storage
{
    public class HasObjectId
    {
        [JsonIgnore]
        public string ID { get; set; }
    }
}