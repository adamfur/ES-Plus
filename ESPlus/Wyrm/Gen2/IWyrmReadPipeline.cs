using System;
using System.Collections.Generic;

namespace ESPlus.Wyrm
{
    public interface IWyrmReadPipeline
    {
        public IWyrmReadPipeline CreateEventFilter(IEnumerable<Type> types);
        public IWyrmReadPipeline StreamNameFilter(string regex);
        public IWyrmReadPipeline EventFilter(IEnumerable<Type> types);
        public IWyrmReadPipeline Take(int count);
        public IWyrmReadPipeline Skip(int count);
        public IWyrmReadPipeline Subscribe();
        public IWyrmReadPipeline GroupByStream();
        public IWyrmReadPipeline ReadDirection(Direction direction);
        public IAsyncEnumerable<WyrmItem> QueryEventsAsync();
        public IAsyncEnumerable<string> QueryStreamNamesAsync();
    }
}