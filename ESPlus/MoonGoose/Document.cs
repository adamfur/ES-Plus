using ESPlus.Storage;

namespace ESPlus.MoonGoose
{
    public class Document
    {
        public string Filename { get; }
        public byte[] Payload { get; }
        public object Item { get; }
        public long[] Keywords { get; }
        public string Tenant { get; set; }
        public Flags Flags { get; }
        public Operation Operation { get; }

        public Document(string filename, long[] keywords, byte[] payload, object item, string tenant, Flags flags, Operation operation)
        {
            Filename = filename;
            Payload = payload;
            Item = item;
            Tenant = tenant ?? "";
            Flags = flags;
            Operation = operation;
            Keywords = keywords;
        }
    }

    public enum Flags : int
    {
        None,
        Indexed = (1 << 0),
    }

    public enum Operation : int
    {
        Save,
        Delete,
    }
}