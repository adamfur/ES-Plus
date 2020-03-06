using System;
using System.Collections.Generic;

namespace ESPlus.Wyrm
{
    public interface IWyrmReadPipelineBase<T>
    {
        public IAsyncEnumerable<WyrmItem> QueryAsync();
        public T EventFilter(IEnumerable<Type> types);
        public T EventFilter(params Type[] types);          
    }
    
    public interface ISimpleReadPipeline : IWyrmReadPipelineBase<ISimpleReadPipeline>
    {
        public ISimpleReadPipeline Take(int count);
        public ISimpleReadPipeline Skip(int count);
        public ISimpleReadPipeline Subscribe();
        public ISimpleReadPipeline ReadDirection(Direction direction);
    }

    public interface IWyrmGroupedReadPipeline : IWyrmReadPipelineBase<IWyrmGroupedReadPipeline>
    {
        public IWyrmGroupedReadPipeline CreateEventFilter(IEnumerable<Type> types);
        public IWyrmGroupedReadPipeline CreateEventFilter(params Type[] types);
        public IWyrmGroupedReadPipeline StreamNameFilter(string regex);
    }
}
