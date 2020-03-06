using System;
using System.Collections.Generic;

namespace ESPlus.Wyrm
{
    public interface IWyrmReadPipelineBase
    {
        public IAsyncEnumerable<WyrmItem> QueryEventsAsync();
    }
    
    public interface ISimpleReadPipeline : IWyrmReadPipelineBase
    {
        public ISimpleReadPipeline EventFilter(IEnumerable<Type> types);
        public ISimpleReadPipeline EventFilter(params Type[] types);
        public ISimpleReadPipeline Take(int count);
        public ISimpleReadPipeline Skip(int count);
        public ISimpleReadPipeline Subscribe();
        public ISimpleReadPipeline ReadDirection(Direction direction);
    }

    public interface IWyrmStartPipeline : IWyrmReadPipelineBase, ISimpleReadPipeline
    {
        public IWyrmGroupedReadPipeline GroupByStream();
    }

    public interface IWyrmGroupedReadPipeline : IWyrmReadPipelineBase
    {
        public IWyrmGroupedReadPipeline CreateEventFilter(IEnumerable<Type> types);
        public IWyrmGroupedReadPipeline CreateEventFilter(params Type[] types);
        public IWyrmGroupedReadPipeline StreamNameFilter(string regex);
    }
}
