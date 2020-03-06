using System;
using System.Collections.Generic;
using System.Linq;

namespace ESPlus.Wyrm
{
    public class WyrmStartPipeline : IWyrmGroupedReadPipeline, ISimpleReadPipeline
    {
        private readonly WyrmDriver _wyrmDriver;
        private bool _subscribe = false;
        private string _regex;
        private List<Type> _createEventFilter;
        private List<Type> _eventFilter;
        private int _take = -1;
        private bool _groupByStream = false;
        private Direction _direction = Direction.Forward;
        private int _skip = -1;
        private IApply _apply;

        public WyrmStartPipeline(WyrmDriver wyrmDriver, IApply apply)
        {
            _wyrmDriver = wyrmDriver;
            _apply = apply;
        }

        public IAsyncEnumerable<WyrmItem> QueryAsync()
        {
            return _wyrmDriver.ReadQueryAsync(_apply, _subscribe, _regex, _createEventFilter, _eventFilter, _take, _groupByStream, _direction, _skip);
        }

        ISimpleReadPipeline IWyrmReadPipelineBase<ISimpleReadPipeline>.EventFilter(IEnumerable<Type> types)
        {
            _eventFilter = types.ToList();
            return this;
        }

        ISimpleReadPipeline IWyrmReadPipelineBase<ISimpleReadPipeline>.EventFilter(params Type[] types)
        {
            _eventFilter = types.ToList();
            return this;
        }

        IWyrmGroupedReadPipeline IWyrmReadPipelineBase<IWyrmGroupedReadPipeline>.EventFilter(IEnumerable<Type> types)
        {
            _eventFilter = types.ToList();
            return this;
        }

        IWyrmGroupedReadPipeline IWyrmReadPipelineBase<IWyrmGroupedReadPipeline>.EventFilter(params Type[] types)
        {
            _eventFilter = types.ToList();
            return this;
        }

        public IWyrmGroupedReadPipeline CreateEventFilter(IEnumerable<Type> types)
        {
            _createEventFilter = types.ToList(); 
            return this;
        }

        public IWyrmGroupedReadPipeline CreateEventFilter(params Type[] types)
        {
            _createEventFilter = types.ToList(); 
            return this;
        }

        public IWyrmGroupedReadPipeline StreamNameFilter(string regex)
        {
            _regex = regex;
            return this;
        }

        public ISimpleReadPipeline Take(int count)
        {
            _take = count;
            return this;
        }

        public ISimpleReadPipeline Skip(int count)
        {
            _skip = count;
            return this;
        }

        public ISimpleReadPipeline Subscribe()
        {
            _subscribe = true;
            return this;
        }

        public ISimpleReadPipeline ReadDirection(Direction direction)
        {
            _direction = direction;
            return this;
        }

        public IWyrmGroupedReadPipeline GroupByStream()
        {
            _groupByStream = true;
            return this;
        }
    }
}