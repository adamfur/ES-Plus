using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.MoonGoose;

namespace ESPlus.Wyrm
{
    public static class NetworkStreamExtensions
    {
        public static async Task<(Queries query, Tokenizer tokenizer)> QueryAsync(this NetworkStream reader, CancellationToken cancellationToken)
        {
            var length = await reader.ReadInt32Async(cancellationToken);
            var query = (Queries) await reader.ReadInt32Async(cancellationToken);
            var payload = await reader.ReadBinaryAsync(length - sizeof(Int32) * 2, cancellationToken);
            var tokenizer = new Tokenizer(payload);
         
            return (query, tokenizer);
        }

        public static async Task<int> ReadInt32Async(this NetworkStream reader, CancellationToken cancellationToken)
        {
            var data = await reader.ReadBinaryAsync(sizeof(Int32), cancellationToken);
            
            return BitConverter.ToInt32(data);
        }
        
        public static async Task<long> ReadInt64Async(this NetworkStream reader, CancellationToken cancellationToken)
        {
            var data = await reader.ReadBinaryAsync(sizeof(Int64), cancellationToken);
            
            return BitConverter.ToInt32(data);
        }

        public static async Task<byte[]> ReadBinaryAsync(this NetworkStream reader, int count,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[count];
            var remaining = count;
            var offset = 0;

            while (remaining != 0)
            {
                var result = await reader.ReadAsync(buffer, offset, remaining, cancellationToken);

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