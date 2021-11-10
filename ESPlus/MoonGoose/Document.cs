using System;
using System.Text;
using System.Text.Json;

namespace ESPlus.MoonGoose
{
    public class Document
    {
        public string Path { get; }

        public byte[] Payload
        {
            get
            {
                var json = JsonSerializer.Serialize(Item);
                var encoded = Encoding.UTF8.GetBytes(json);

                return encoded;
            }
        }
        public object Item { get; }
        public virtual long[] Keywords => Array.Empty<long>();
        public string Tenant { get; private set; }
        public Flags Flags { get; set; } = Flags.None;
        public Operation Operation { get; }

        public Document(string tenant, string path, object item, Operation operation)
        {
            if (operation == Operation.Delete)
            {
                Flags = Flags.Indexed;
            }
            
            Path = path;
            Item = item ?? "";
            Tenant = tenant ?? "@";
            Operation = operation;
        }
    }

    [Flags]
    public enum Flags
    {
        None = 0,
        Indexed = (1 << 0),
    }

    public enum Operation
    {
        Save,
        Delete,
    }
}