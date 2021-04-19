using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Extensions;

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

        private async Task<TcpClient> Create(CancellationToken cancellationToken)
        {
            var client = new TcpClient
            {
                NoDelay = false
            };

            await Retry.RetryAsync(async () => await client.ConnectAsync(_host, _port, cancellationToken), default);

            return client;
        }

        public async Task PutAsync(string database, List<Document> documents,
            CancellationToken cancellationToken)
        {
            if (documents.Count == 0)
            {
                return;
            }
            
            using var client = await Create(cancellationToken);
            await using var stream = client.GetStream();
            await using var writer = new BinaryWriter(stream);
            var memoryStream = new MemoryStream();
            var binaryWriter = new BinaryWriter(memoryStream);

            SelectDatabase(writer, database);
            foreach (var document in documents)
            {
                // Console.WriteLine($"({document.Operation}) PutAsync: {database}, Path: [{document.Path}], Tenant: [{document.Tenant}]");
                var encodedTenant = Encoding.UTF8.GetBytes(document.Tenant);
                var encodedPath = Encoding.UTF8.GetBytes(document.Path);
                
                binaryWriter.Write((int) document.Operation);
                binaryWriter.Write((int) document.Flags);
                binaryWriter.Write((int) encodedTenant.Length);
                binaryWriter.Write(encodedTenant);
                binaryWriter.Write((int) encodedPath.Length);
                binaryWriter.Write(encodedPath);      
                binaryWriter.Write((int) document.Payload.Length);
                binaryWriter.Write(document.Payload);
                binaryWriter.Write((int) document.Keywords.Length);

                foreach (var item in document.Keywords)
                {
                    binaryWriter.Write((long) item);
                }
            }

            var payload = memoryStream.ToArray();
            writer.Write((int) 76 + payload.Length);
            writer.Write((int) Commands.Put);
            writer.Write(Position.Start.Binary); // previousChecksum
            writer.Write(Position.Start.Binary); // checksum
            writer.Write((int) documents.Count);
            writer.Write(payload);
            await stream.FlushAsync(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
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
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        private void SelectDatabase(BinaryWriter writer, string database)
        {
            var encoded = Encoding.UTF8.GetBytes(database ?? ".");

            writer.Write((int) 12 + encoded.Length);
            writer.Write((int) Commands.Database);
            writer.Write((int) encoded.Length);
            writer.Write(encoded);
        }

        public async Task<byte[]> GetAsync(string database, string tenant, string path, CancellationToken cancellationToken)
        {
            byte[] payload = null;
            var encodedTenant = Encoding.UTF8.GetBytes(tenant ?? "@");
            var encodedKey = Encoding.UTF8.GetBytes(path);
            using var client = await Create(cancellationToken);
            await using var stream = client.GetStream();
            await using var writer = new BinaryWriter(stream);

            SelectDatabase(writer, database);
            writer.Write((int) 16 + encodedTenant.Length + encodedKey.Length);
            writer.Write((int) Commands.Get);
            writer.Write((int) encodedTenant.Length);
            writer.Write(encodedTenant);
            writer.Write((int) encodedKey.Length);
            writer.Write(encodedKey);
            await stream.FlushAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var (query, tokenizer) = await stream.QueryAsync(cancellationToken);

                if (query == Queries.Item)
                {
                    payload = tokenizer.ReadBinary().ToArray();
                }
                else if (query == Queries.Success)
                {
                    break;
                }
                else if (query == Queries.Exception)
                {
                    ParseException(tokenizer, tenant, path);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return payload;
        }

        public async Task<byte[]> ChecksumAsync(string database, CancellationToken cancellationToken)
        {
            var payload = Position.Start.Binary;
            using var client = await Create(cancellationToken);
            await using var stream = client.GetStream();
            await using var writer = new BinaryWriter(stream);

            SelectDatabase(writer, database);
            writer.Write((int) 8);
            writer.Write((int) Commands.Checksum);
            await stream.FlushAsync(cancellationToken);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                var (query, tokenizer) = await stream.QueryAsync(cancellationToken);


                if (query == Queries.Checksum)
                {
                    payload = tokenizer.ReadBinary(32).ToArray();
                }
                else if (query == Queries.Success)
                {
                    break;
                }
                else if (query == Queries.Exception)
                {
                    ParseException(tokenizer);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return payload;
        }

        public async Task<byte[]> SimulateExceptionThrow(CancellationToken cancellationToken)
        {
            var payload = Position.Start.Binary;
            using var client = await Create(cancellationToken);
            await using var stream = client.GetStream();
            await using var writer = new BinaryWriter(stream);

            writer.Write((int) 8);
            writer.Write((int) Commands.ThrowException);
            await stream.FlushAsync(cancellationToken);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                var (query, tokenizer) = await stream.QueryAsync(cancellationToken);

                if (query == Queries.Exception)
                {
                    ParseException(tokenizer);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return payload;
        }

        public async IAsyncEnumerable<byte[]> SearchAsync(string database, string tenant, long[] parameters,
            int skip, int take, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var encodedTenant = Encoding.UTF8.GetBytes(tenant ?? "@");
            using var client = await Create(cancellationToken);
            await using var stream = client.GetStream();
            await using var writer = new BinaryWriter(stream);

            SelectDatabase(writer, database);
            Skip(writer, skip);
            Take(writer, take);
            writer.Write((int) 16 + encodedTenant.Length + parameters.Length * sizeof(long));
            writer.Write((int) Commands.Search);
            writer.Write((int) encodedTenant.Length);
            writer.Write(encodedTenant);
            writer.Write((int) parameters.Length);

            foreach (var item in parameters)
            {
                writer.Write((long) item);
            }
            
            await stream.FlushAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var (query, tokenizer) = await stream.QueryAsync(cancellationToken);

                if (query == Queries.SearchResult)
                {
                    var payload = tokenizer.ReadBinary().ToArray();

                    yield return payload;
                }
                else if (query == Queries.Success)
                {
                    break;
                }
                else if (query == Queries.Exception)
                {
                    ParseException(tokenizer);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        private void Take(BinaryWriter writer, int amount)
        {
            writer.Write((int) 12);
            writer.Write((int) Commands.Take);
            writer.Write((int) amount);
        }

        private void Skip(BinaryWriter writer, int amount)
        {
            writer.Write((int) 12);
            writer.Write((int) Commands.Skip);
            writer.Write((int) amount);
        }

        private void ParseException(Tokenizer tokenizer, string tenant = "<@@@>", string path = "<@@@>")
        {
            var code = (ErrorCode) tokenizer.ReadI32();
            var message = tokenizer.ReadString();

            switch (code)
            {
                case ErrorCode.NotFound: throw new MoonGooseNotFoundException($"Not found: {tenant}/{path}");
                default: throw new MoonGooseException(message);
            }
        }
    }
}