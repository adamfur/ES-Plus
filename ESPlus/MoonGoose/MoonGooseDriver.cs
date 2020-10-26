using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.MoonGoose
{
    public class MoonGooseDriver : IMoonGooseDriver
    {
        private readonly string _host;
        private readonly int _port;

        public MoonGooseDriver(string connectionString)
        {
            var parts = connectionString.Split(":");

            _host = parts[0];
            _port = int.Parse(parts[1]);
        }

        private async Task<TcpClient> Create()
        {
            var client = new TcpClient();
            client.NoDelay = false;

            await Retry.RetryAsync(() => client.ConnectAsync(_host, _port));

            return client;
        }
        
        public async Task<byte[]> GetAsync(string database, string key, CancellationToken cancellationToken = default)
        {
            using (var client = await Create())
            using (var stream = client.GetStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 12 + database.Length);
                writer.Write((int) Commands.Database);
                writer.Write((int) database.Length);
                writer.Write(Encoding.UTF8.GetBytes(database));

                var keyBuffer = Encoding.UTF8.GetBytes(key);
                writer.Write((int) 12 + keyBuffer.Length);
                writer.Write((int) Commands.Get);
                writer.Write((int)keyBuffer.Length);
                writer.Write(keyBuffer);
                writer.Flush();
                await stream.FlushAsync(cancellationToken);
                
                byte[] payload = new byte[0];

                while (true)
                {
                    var (query, tokenizer) = await stream.QueryAsync(cancellationToken);

                    if (query == Queries.Payload)
                    {
                        var length = tokenizer.ReadI32();
                        
                        payload = tokenizer.ReadBinary(length).ToArray();
                    }
                    else if (query == Queries.Success)
                    {
                        return payload;
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else
                    {
                        // throw new NotImplementedException($"query: {query}");
                    }
                }
            }
        }
        
        public async Task PutAsync(string database, IEnumerable<Document> documents, CancellationToken cancellationToken = default)
        {
            using (var client = await Create())
            using (var stream = client.GetStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 12 + database.Length);
                writer.Write((int) Commands.Database);
                writer.Write((int) database.Length);
                writer.Write(Encoding.UTF8.GetBytes(database));

                foreach (var document in documents)
                {
                    writer.Write((int) 20 + document.Payload.Length + document.Keywords.Length * sizeof(long) + document.Key.Length);
                    writer.Write((int) Commands.Put);
                    var key = Encoding.UTF8.GetBytes(document.Key);
                    writer.Write((int) key.Length);
                    writer.Write(key);
                    writer.Write((int) document.Payload.Length);
                    writer.Write(document.Payload);
                    writer.Write((int) document.Keywords.Length);
                    writer.Write(LongArrayToByteArray(document.Keywords));
                }

                writer.Write((int) 8);
                writer.Write((int) Commands.Commit);
                writer.Flush();
                await stream.FlushAsync(cancellationToken);

                while (true)
                {
                    var (query, tokenizer) = await stream.QueryAsync(cancellationToken);

                    if (query == Queries.Success)
                    {
                        return;
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException($"query: {query}");
                    }
                }
            }
        }

        private void ParseException(Tokenizer tokenizer)
        {
            var code = tokenizer.ReadI32();
            var message = tokenizer.ReadString();

            throw new MoonGooseExceptions(message);
        }

        public async IAsyncEnumerable<byte[]> SearchAsync(string database, long[] parameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using (var client = await Create())
            using (var stream = client.GetStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 12 + database.Length);
                writer.Write((int) Commands.Database);
                writer.Write((int) database.Length);
                writer.Write(Encoding.UTF8.GetBytes(database));

                writer.Write((int) 12 + parameters.Length * sizeof(long));
                writer.Write((int) Commands.Search);
                writer.Write((int) parameters.Length);
                writer.Write(LongArrayToByteArray(parameters));
                writer.Flush();
                await stream.FlushAsync(cancellationToken);

                while (true)
                {
                    var (query, tokenizer) = await stream.QueryAsync(cancellationToken);

                    if (query == Queries.Success)
                    {
                        break;
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else if (query == Queries.SearchItem)
                    {
                        yield return ParseSearchItem(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException($"query: {query}");
                    }
                }
            }
        }

        private byte[] ParseSearchItem(Tokenizer tokenizer)
        {
            var length = tokenizer.ReadI32();
            
            return tokenizer.ReadBinary(length).ToArray();
        }

        private static byte[] LongArrayToByteArray(long[] integers)
        {
            var bytes = new List<byte>(integers.Length * sizeof(long));

            foreach (var integer in integers)
            {
                bytes.AddRange(BitConverter.GetBytes(integer));
            }

            return bytes.ToArray();
        }
    }
}