using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public interface IWyrmDriver
    {
        IEventSerializer Serializer { get; }
        IAsyncEnumerable<WyrmEvent2> EnumerateStream(string streamName, CancellationToken cancellationToken = default);
        Task DeleteAsync(string streamName, long version, CancellationToken cancellationToken = default);
        Task<WyrmResult> Append(IEnumerable<WyrmEvent> events, CancellationToken cancellationToken);
        IAsyncEnumerable<string> EnumerateStreams(CancellationToken cancellationToken, params Type[] filters);
        Task<Position> LastCheckpointAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<WyrmEvent2> EnumerateAllByStreamsAsync(CancellationToken cancellationToken = default,
            params Type[] filters);
        Task PingAsync();
        IAsyncEnumerable<WyrmEvent2> SubscribeAsync(Position from, CancellationToken cancellationToken = default);
        IAsyncEnumerable<WyrmEvent2> EnumerateAll(Position from, CancellationToken cancellationToken = default);
        IAsyncEnumerable<WyrmEvent2> EnumerateAll(DateTime from, DateTime to, CancellationToken cancellationToken = default);
        IAsyncEnumerable<byte[]> Pull(CancellationToken cancellationToken = default);
    }
}