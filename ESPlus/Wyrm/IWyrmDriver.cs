using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public interface IWyrmDriver
    {
        Task<WyrmResult> CreateStreamAsync(string streamName);
        Task<WyrmResult> DeleteStreamAsync(string streamName, long version);
        Task<WyrmResult> AppendAsync(Bundle bundle);
        IWyrmReadPipeline ReadFrom(Position position);
        IWyrmReadPipeline ReadStream(string streamName);
        IAsyncEnumerable<string> EnumerateStreams();
        Task<Position> CheckpointAsync();
        Task<TimeSpan> PingAsync();
    }
}