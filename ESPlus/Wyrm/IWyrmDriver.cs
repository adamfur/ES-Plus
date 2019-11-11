using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public interface IWyrmDriver
    {
        IEventSerializer Serializer { get; }
        IEnumerable<WyrmItem> ReadAllForward(Position position);
        IEnumerable<WyrmItem> ReadAllBackward(Position position);
        IEnumerable<WyrmItem> ReadStreamForward(string streamName);
        IEnumerable<WyrmItem> ReadStreamBackward(string streamName);
        Task<Position> CreateStreamAsync(string streamName);
        Task<Position> DeleteStreamAsync(string streamName, long version);
        Task<Position> Append(Bundle bundle);
        IEnumerable<WyrmItem> SubscribeAll(Position from);
        IEnumerable<string> EnumerateStreams(params Type[] filters);
        IEnumerable<WyrmItem> EnumerateAllGroupByStream(params Type[] filters);
        Position Checkpoint();
    }
}