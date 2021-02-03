using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.FlushPolicies;
using ESPlus.Misc;
using NSubstitute;
using Xunit;

namespace ESPlus.Tests.EventHandlers
{
    public class ComplexEventHandlerTests
    {
        public class DummyEvent
        {
        }

        public class DummyEmitEvent
        {
        }

        public class DummyEmitOnSubmit
        {
            public string Key { get; set; }
            public object Payload { get; set; }
        }

        public class DummyEventHandler : BasicEventHandler<IEventHandlerContext>,
            IHandleEvent<DummyEvent>,
            IHandleEvent<DummyEmitOnSubmit>
        {
            public DummyEventHandler(IEventHandlerContext context)
                : base(context, null, new NullFlushPolicy())
            {
            }

            public DummyEventHandler(IEventHandlerContext context, IEventTypeResolver eventTypeResolver)
                : base(context, eventTypeResolver, new NullFlushPolicy())
            {
            }

            public void Apply(DummyEvent @event)
            {
                Emit(new DummyEmitEvent());
            }

            public void Apply(DummyEmitOnSubmit @event)
            {
                EmitOnSubmit(@event.Key, @event.Payload);
            }

            // public override Task<bool> DispatchEventAsync(object @event)
            // {
            //     throw new System.NotImplementedException();
            // }
        }

        public interface IReceiverDummyEventHandler : IEventHandler,
            IHandleEvent<DummyEvent>,
            IHandleEvent<DummyEmitEvent>
        {
        }

        private IEventHandlerContext _context;

        public ComplexEventHandlerTests()
        {
            _context = Substitute.For<IEventHandlerContext>();
        }

        [Fact]
        public async Task DispatchEvent_AllEventHandlersReceice_ReceivedOnce()
        {
            var eventHandler1 = Substitute.For<IReceiverDummyEventHandler>();
            var eventHandler2 = Substitute.For<IReceiverDummyEventHandler>();
            var complexHandler = new ComplexEventHandler<IEventHandlerContext>(_context);

            complexHandler.Add(eventHandler1);
            complexHandler.Add(eventHandler2);
            DummyEvent dummyEvent = new DummyEvent();

            await complexHandler.DispatchEventAsync(dummyEvent);

            await eventHandler1.Received().DispatchEventAsync(Arg.Is<DummyEvent>(p => p == dummyEvent));
            await eventHandler2.Received().DispatchEventAsync(Arg.Is<DummyEvent>(p => p == dummyEvent));
        }

        [Fact]
        public async Task DispatchEvent_EmittedEventTriggerLaterEventHandlers_TriggeredByTheEmittedEvent()
        {
            var eventHandler1 = new DummyEventHandler(_context);
            var eventHandler2 = Substitute.For<IReceiverDummyEventHandler>();
            var complexHandler = new ComplexEventHandler<IEventHandlerContext>(_context);

            complexHandler.Add(eventHandler1);
            complexHandler.Add(eventHandler2);
            var dummyEvent = new DummyEvent();

            await complexHandler.DispatchEventAsync(dummyEvent);

            await eventHandler2.Received(1).DispatchEventAsync(Arg.Is<DummyEvent>(p => p == dummyEvent));
            await eventHandler2.Received(1).DispatchEventAsync(Arg.Any<DummyEmitEvent>());
        }     

        [Fact]
        public void Flush_ContextIsAlsoFlushed_Once()
        {
            var complexHandler = new ComplexEventHandler<IEventHandlerContext>(_context);

            complexHandler.FlushAsync();

            _context.Received(1).FlushAsync();
        }           

        [Fact]
        public async Task Flush_EmitOnSubmitEventsAreRaised_PassOnToSubsequentEventHandlers()
        {
            var eventHandler1 = new DummyEventHandler(_context);
            var eventHandler2 = Substitute.For<IReceiverDummyEventHandler>();
            var complexHandler = new ComplexEventHandler<IEventHandlerContext>(_context);

            complexHandler.Add(eventHandler1);
            complexHandler.Add(eventHandler2);

            var payload1 = new object();
            var payload2 = new object();
            var payload3 = new object();

            await complexHandler.DispatchEventAsync(new DummyEmitOnSubmit() { Key = "1", Payload = payload1 });
            await complexHandler.DispatchEventAsync(new DummyEmitOnSubmit() { Key = "2", Payload = payload2 });
            await complexHandler.DispatchEventAsync(new DummyEmitOnSubmit() { Key = "2", Payload = payload3 });
            await complexHandler.FlushAsync(); 

            await eventHandler2.Received().DispatchEventAsync(Arg.Is<object>(p => p == payload1));
            await eventHandler2.DidNotReceive().DispatchEventAsync(Arg.Is<object>(p => p == payload2));
            await eventHandler2.Received(1).DispatchEventAsync(Arg.Is<object>(p => p == payload3));
        }          
    }
}
