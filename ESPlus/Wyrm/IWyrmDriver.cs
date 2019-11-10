using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public interface IWyrmDriver
    {
        IEventSerializer Serializer { get; }
        IEnumerable<WyrmItem> ReadStreamForward(string streamName);
        Task<Position> DeleteStreamAsync(string streamName, long version);
        IEnumerable<string> EnumerateStreams(params Type[] filters);
        Position LastCheckpoint();
        IEnumerable<WyrmItem> SubscribeAll(Position from);
        IEnumerable<WyrmItem> EnumerateAll(Position from);
        IEnumerable<WyrmItem> EnumerateAllGroupByStream(params Type[] filters);
        Task<Position> Append(Bundle bundle);
        IEnumerable<WyrmItem> ReadStreamBackward(string streamName);
        Task<Position> CreateStreamAsync(string streamName);
    }
}