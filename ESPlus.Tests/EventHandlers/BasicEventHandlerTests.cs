using System;
using System.Linq;
using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.FlushPolicies;
using NSubstitute;
using Xunit;

namespace ESPlus.Tests.EventHandlers
{
    public class BasicEventHandlerTests
    {
        public class DummyEvent
        {
        }

        public class DummyEmitEvent
        {
        }

        public class DummyEmitSubmitEvent
        {
        }

        public class DummyEventHandler : BasicEventHandler<IEventHandlerContext>,
            IHandleEvent<DummyEvent>,
            IHandleEvent<DummyEmitEvent>,
            IHandleEvent<DummyEmitSubmitEvent>
        {
            public bool Called { get; set; } = false;

            public DummyEventHandler(IEventHandlerContext context)
                : base(context, null, new NullFlushPolicy())
            {
            }

            public void Apply(DummyEvent @event)
            {
                Called = true;
            }

            public void Apply(DummyEmitEvent @event)
            {
                Emit(new object());
            }

            public void Apply(DummyEmitSubmitEvent @event)
            {
                EmitOnSubmit("abc", "def");
            }

            public override Task<bool> DispatchEventAsync(object @event)
            {
                throw new NotImplementedException();
            }
        }

        private IEventHandlerContext _context;

        public BasicEventHandlerTests()
        {
            _context = Substitute.For<IEventHandlerContext>();
        }

        [Fact]
        public void Flush_ContextIsAlsoFlushed_Once()
        {
            var eventHandler = new BasicEventHandler<IEventHandlerContext>(_context, null, new NullFlushPolicy());

            eventHandler.Flush();

            _context.Received(1).Flush();
        }

        [Fact]
        public void DispatchEvent_EventIsRouterTotheCorrectFunction_Once()
        {
            var eventHandler = new DummyEventHandler(_context);

            eventHandler.DispatchEvent(new DummyEvent());

            Assert.True(eventHandler.Called);
        }

        [Fact]
        public void DispatchEvent_NoRoute_Ignore()
        {
            var eventHandler = new DummyEventHandler(_context);

            eventHandler.DispatchEvent(new DummyEvent());
        }

        [Fact]
        public void TakeEmittedEvents_EmptyAfterUse_Empty()
        {
            var eventHandler = new DummyEventHandler(_context);

            eventHandler.DispatchEvent(new DummyEmitEvent());
            var pass1 = eventHandler.TakeEmittedEvents().ToList();
            var pass2 = eventHandler.TakeEmittedEvents().ToList();

            Assert.Equal(1, pass1.Count());
            Assert.Equal(0, pass2.Count());
        }

        [Fact]
        public void TakeEmittedOnSubmitEvents_EmptyAfterUse_Empty()
        {
            var eventHandler = new DummyEventHandler(_context);

            eventHandler.DispatchEvent(new DummyEmitSubmitEvent());

            var pass1 = eventHandler.TakeEmittedOnSubmitEvents().ToList();
            var pass2 = eventHandler.TakeEmittedOnSubmitEvents().ToList();

            Assert.Equal(1, pass1.Count());
            Assert.Equal(0, pass2.Count());
        }        
    }
}