using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.FlushPolicies;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public class ComplexEventHandler<TContext> : EventHandlerBase<TContext>
        where TContext : class, IEventHandlerContext
    {
        private LinkedList<IEventHandler> _pipeline = new LinkedList<IEventHandler>();

        public ComplexEventHandler(TContext context)
            : base(context, new NullFlushPolicy())
        {
        }

        public void Add(IEventHandler eventHandler)
        {
            _pipeline.AddLast(eventHandler);
        }

        public override async Task<bool> DispatchEventAsync(object @event)
        {
            var payload = new List<object> { @event };
            var result = false;

            foreach (var eventHandler in _pipeline)
            {
                foreach (var item in payload)
                {
                    result |= await eventHandler.DispatchEventAsync(item);
                }
                payload.AddRange(eventHandler.TakeEmittedEvents());
            }
            return result;
        }

        public override IEnumerable<object> TakeEmittedEvents()
        {
            throw new NotImplementedException();
        } 

        public override IEnumerable<object> TakeEmittedOnSubmitEvents()
        {
            throw new NotImplementedException();
        }

        public override Task<object> Search(long[] parameters, string tenant)
        {
            throw new NotImplementedException();
        }

        public override Task<object> Get(string path, string tenant)
        {
            throw new NotImplementedException();
        }

        public override async Task FlushAsync()
        {
            var payload = new List<object>();

            foreach (var eventHandler in _pipeline)
            {
                foreach (var item in payload)
                {
                    await eventHandler.DispatchEventAsync(item);
                }
                payload.AddRange(eventHandler.TakeEmittedOnSubmitEvents());
            }            
            await base.FlushAsync();
        }

        public override Task<bool> DispatchAsync(Event @event)
        {
            throw new NotImplementedException();
        }

        // public override Task<bool> DispatchEventAsync(object @event)
        // {
        //     throw new NotImplementedException();
        // }
    }
}
