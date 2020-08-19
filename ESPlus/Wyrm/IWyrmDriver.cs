using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public interface IWyrmDriver
    {
        IEventSerializer Serializer { get; }
        IEnumerable<WyrmEvent2> EnumerateStream(string streamName);
        Task DeleteAsync(string streamName, long version, CancellationToken cancellationToken);
        Task<WyrmResult> Append(IEnumerable<WyrmEvent> events);
        IAsyncEnumerable<string> EnumerateStreams(CancellationToken cancellationToken, params Type[] filters);
        Task<Position> LastCheckpointAsync(CancellationToken cancellationToken);
        IEnumerable<WyrmEvent2> Subscribe(Position from);
        IEnumerable<WyrmEvent2> EnumerateAll(Position from);
        IAsyncEnumerable<WyrmEvent2> EnumerateAllByStreamsAsync(CancellationToken cancellationToken,
            params Type[] filters);
        Task PingAsync();
        IAsyncEnumerable<WyrmEvent2> SubscribeAsync(Position from, CancellationToken cancellationToken);
    }
}