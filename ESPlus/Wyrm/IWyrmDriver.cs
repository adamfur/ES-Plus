using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public interface IWyrmDriver
    {
        IEventSerializer Serializer { get; }
        Task<WyrmResult> CreateStreamAsync(string streamName);
        Task<WyrmResult> DeleteStreamAsync(string streamName, long version);
        Task<WyrmResult> Append(Bundle bundle);
        IEnumerable<WyrmItem> ReadAllForward(Position position);
        IEnumerable<WyrmItem> ReadAllBackward(Position position);
        IEnumerable<WyrmItem> ReadStreamForward(string streamName);
        IEnumerable<WyrmItem> ReadStreamBackward(string streamName);
        IEnumerable<WyrmItem> SubscribeStream(string streamName);
        IEnumerable<WyrmItem> SubscribeAll(Position from);
        IEnumerable<string> EnumerateStreams(params Type[] filters);
        IEnumerable<WyrmItem> ReadAllGroupByStream(params Type[] filters);
        Position Checkpoint();
        TimeSpan Ping();
    }
}