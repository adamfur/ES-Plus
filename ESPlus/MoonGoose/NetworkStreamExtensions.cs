using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ESPlus.MoonGoose
{
    public static class NetworkStreamExtensions
    {
        public static async Task<(Queries query, Tokenizer tokenizer)> QueryAsync(this NetworkStream reader)
        {
            var length = await reader.ReadInt32Async();
            var query = (Queries) await reader.ReadInt32Async();
            var payload = await reader.ReadBinaryAsync(length - sizeof(Int32) * 2);
            var tokenizer = new Tokenizer(payload);
         
            return (query, tokenizer);
        }

        private static async Task<int> ReadInt32Async(this NetworkStream reader)
        {
            var data = await reader.ReadBinaryAsync(sizeof(Int32));
            
            return BitConverter.ToInt32(data);
        }

        private static async Task<byte[]> ReadBinaryAsync(this NetworkStream reader, int count)
        {
            var buffer = new byte[count];
            var remaining = count;
            var offset = 0;

            while (remaining != 0)
            {
                var result = await reader.ReadAsync(buffer, offset, remaining);

                if (result <= 0)
                {
                    throw new Exception();
                }

                offset += result;
                remaining -= result;
            }

            return buffer;
        }
    }
}