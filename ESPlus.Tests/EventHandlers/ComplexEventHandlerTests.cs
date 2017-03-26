using ESPlus.EventHandlers;
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
                : base(context)
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
        }

        public interface IReceiverDummyEventHandler : IEventHandler<IEventHandlerContext>,
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
        public void DispatchEvent_AllEventHandlersReceice_ReceivedOnce()
        {
            var eventHandler1 = Substitute.For<IReceiverDummyEventHandler>();
            var eventHandler2 = Substitute.For<IReceiverDummyEventHandler>();
            var complexHandler = new ComplexEventHandler<IEventHandlerContext>(_context);

            complexHandler.Add(eventHandler1);
            complexHandler.Add(eventHandler2);
            DummyEvent dummyEvent = new DummyEvent();

            complexHandler.DispatchEvent(dummyEvent);

            eventHandler1.Received().DispatchEvent(Arg.Is<DummyEvent>(p => p == dummyEvent));
            eventHandler2.Received().DispatchEvent(Arg.Is<DummyEvent>(p => p == dummyEvent));
        }

        [Fact]
        public void DispatchEvent_EmittedEventTriggerLaterEventHandlers_TriggeredByTheEmittedEvent()
        {
            var eventHandler1 = new DummyEventHandler(_context);
            var eventHandler2 = Substitute.For<IReceiverDummyEventHandler>();
            var complexHandler = new ComplexEventHandler<IEventHandlerContext>(_context);

            complexHandler.Add(eventHandler1);
            complexHandler.Add(eventHandler2);
            var dummyEvent = new DummyEvent();

            complexHandler.DispatchEvent(dummyEvent);

            eventHandler2.Received(1).DispatchEvent(Arg.Is<DummyEvent>(p => p == dummyEvent));
            eventHandler2.Received(1).DispatchEvent(Arg.Any<DummyEmitEvent>());
        }     

        [Fact]
        public void Flush_ContextIsAlsoFlushed_Once()
        {
            var complexHandler = new ComplexEventHandler<IEventHandlerContext>(_context);

            complexHandler.Flush();

            _context.Received(1).Flush();
        }           

        [Fact]
        public void Flush_EmitOnSubmitEventsAreRaised_PassOnToSubsequentEventHandlers()
        {
            var eventHandler1 = new DummyEventHandler(_context);
            var eventHandler2 = Substitute.For<IReceiverDummyEventHandler>();
            var complexHandler = new ComplexEventHandler<IEventHandlerContext>(_context);

            complexHandler.Add(eventHandler1);
            complexHandler.Add(eventHandler2);

            var payload1 = new object();
            var payload2 = new object();
            var payload3 = new object();

            complexHandler.DispatchEvent(new DummyEmitOnSubmit() { Key = "1", Payload = payload1 });
            complexHandler.DispatchEvent(new DummyEmitOnSubmit() { Key = "2", Payload = payload2 });
            complexHandler.DispatchEvent(new DummyEmitOnSubmit() { Key = "2", Payload = payload3 });
            complexHandler.Flush(); 

            eventHandler2.Received().DispatchEvent(Arg.Is<object>(p => p == payload1));
            eventHandler2.DidNotReceive().DispatchEvent(Arg.Is<object>(p => p == payload2));
            eventHandler2.Received(1).DispatchEvent(Arg.Is<object>(p => p == payload3));
        }          
    }
}
