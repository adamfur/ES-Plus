using ESPlus.Storage;

namespace ESPlus.MoonGoose
{
    public class Document : HasObjectId
    {
        public string Key { get; }
        public byte[] Payload { get; }
        public object Item { get; }
        public long[] Keywords { get; }

        public Document(string key, long[] keywords, byte[] payload, object item)
        {
            Key = key;
            Payload = payload;
            Item = item;
            Keywords = keywords;
        }
        
    }
}