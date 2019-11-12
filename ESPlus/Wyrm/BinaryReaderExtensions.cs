using System;
using System.IO;

namespace ESPlus.Wyrm
{
    public static class BinaryReaderExtensions
    {
        public static (Queries query, Tokenizer tokenizer) Query(this BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var query = (Queries) reader.ReadInt32();
            var payload = reader.ReadBytes(length - sizeof(Int32) * 2);
            var tokenizer = new Tokenizer(payload);
            
            return (query, tokenizer);
        }
    }
}