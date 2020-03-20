using ESPlus.Storage;

namespace ESPlus.MoonGoose
{
    public class Document : HasObjectId
    {
        public string Key { get; }
        public byte[] Payload { get; }
        public long[] Keywords { get; }

        public Document(string key, long[] keywords, byte[] payload)
        {
            Key = key;
            Payload = payload;
            Keywords = keywords;
        }
        
    }
}