using System;
using System.Collections.Generic;

namespace ESPlus.EventHandlers
{
    public class ComplexEventHandler<TContext> : EventHandlerBase<TContext>
        where TContext : IEventHandlerContext
    {
        private LinkedList<IEventHandler<TContext>> _pipeline = new LinkedList<IEventHandler<TContext>>();

        public ComplexEventHandler(TContext context)
            : base(context)
        {
        }

        public void Add(IEventHandler<TContext> eventHandler)
        {
            _pipeline.AddLast(eventHandler);
        }

        public override void DispatchEvent(object @event)
        {
            var payload = new List<object> { @event };

            foreach (var eventHandler in _pipeline)
            {
                foreach (var item in payload)
                {
                    eventHandler.DispatchEvent(item);
                }
                payload.AddRange(eventHandler.TakeEmittedEvents());
            }
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
    }
}
