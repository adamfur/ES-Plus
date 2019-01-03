using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.FlushPolicies;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public class ComplexEventHandler<TContext> : EventHandlerBase<TContext>
        where TContext : IEventHandlerContext
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

        public override bool DispatchEvent(object @event)
        {
            var payload = new List<object> { @event };
            var result = false;

            foreach (var eventHandler in _pipeline)
            {
                foreach (var item in payload)
                {
                    result |= eventHandler.DispatchEvent(item);
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

        public override void Flush()
        {
            var payload = new List<object>();

            foreach (var eventHandler in _pipeline)
            {
                foreach (var item in payload)
                {
                    eventHandler.DispatchEvent(item);
                }
                payload.AddRange(eventHandler.TakeEmittedOnSubmitEvents());
            }            
            base.Flush();
        }

        public override bool Dispatch(Event @event)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DispatchEventAsync(object @event)
        {
            throw new NotImplementedException();
        }
    }
}
