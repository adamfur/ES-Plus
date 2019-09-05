using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public interface IWyrmDriver
    {
        IEventSerializer Serializer { get; }
        IEnumerable<WyrmEvent2> EnumerateStream(string streamName);
        Task DeleteAsync(string streamName, long version);
        Task<Position> Append(IEnumerable<WyrmEvent> events);
        IEnumerable<string> EnumerateStreams(params Type[] filters);
        Position LastCheckpoint();
        IEnumerable<WyrmEvent2> Subscribe(Position from);
        IEnumerable<WyrmEvent2> EnumerateAll(Position from);
        IEnumerable<WyrmEvent2> EnumerateAllByStreams(params Type[] filters);
    }
}