using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public interface IWyrmDriver
    {
        IEventSerializer Serializer { get; }
        IEnumerable<WyrmEvent2> ReadStreamForward(string streamName);
        Task<Position> DeleteAsync(string streamName, long version);
        IEnumerable<string> EnumerateStreams(params Type[] filters);
        Position LastCheckpoint();
        IEnumerable<WyrmEvent2> Subscribe(Position from);
        IEnumerable<WyrmEvent2> EnumerateAll(Position from);
        IEnumerable<WyrmEvent2> EnumerateAllByStreams(params Type[] filters);
        Task<Position> Append(Bundle bundle);
        IEnumerable<WyrmEvent2> ReadStreamBackward(string streamName);
    }
}