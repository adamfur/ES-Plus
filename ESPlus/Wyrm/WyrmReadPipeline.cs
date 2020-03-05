using System;
using System.Collections.Generic;
using System.Linq;

namespace ESPlus.Wyrm
{
    public class WyrmReadPipeline : IWyrmReadPipeline
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

        public WyrmReadPipeline(WyrmDriver wyrmDriver, IApply apply)
        {
            _wyrmDriver = wyrmDriver;
            _apply = apply;
        }

        public IWyrmReadPipeline CreateEventFilter(IEnumerable<Type> types)
        {
            _createEventFilter = types.ToList(); 
            return this;
        }

        public IWyrmReadPipeline StreamNameFilter(string regex)
        {
            _regex = regex;
            return this;
        }

        public IWyrmReadPipeline EventFilter(IEnumerable<Type> types)
        {
            _eventFilter = types.ToList();
            return this;
        }

        public IWyrmReadPipeline Take(int count)
        {
            _take = count;
            return this;
        }

        public IWyrmReadPipeline Skip(int count)
        {
            _skip = count;
            return this;
        }

        public IWyrmReadPipeline Subscribe()
        {
            _subscribe = true;
            return this;
        }

        public IWyrmReadPipeline GroupByStream()
        {
            _groupByStream = true;
            return this;
        }

        public IWyrmReadPipeline ReadDirection(Direction direction)
        {
            _direction = direction;
            return this;
        }

        public IAsyncEnumerable<WyrmItem> QueryEventsAsync()
        {
            return _wyrmDriver.ReadQueryAsync(_apply, _subscribe, _regex, _createEventFilter, _eventFilter, _take, _groupByStream, _direction, _skip);
        }

        public IAsyncEnumerable<string> QueryStreamNamesAsync()
        {
            throw new NotImplementedException();
        }
    }
}