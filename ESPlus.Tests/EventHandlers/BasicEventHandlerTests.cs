using System;
using System.Linq;
using System.Threading;
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
                : base(context, null, new NullFlushPolicy(), new EventJsonSerializer())
            {
            }

            public Task Apply(DummyEvent @event, CancellationToken cancellationToken)
            {
                Called = true;
                return Task.CompletedTask;
            }

            public Task Apply(DummyEmitEvent @event, CancellationToken cancellationToken)
            {
                Emit(new object());
                return Task.CompletedTask;
            }

            public Task Apply(DummyEmitSubmitEvent @event, CancellationToken cancellationToken)
            {
                EmitOnSubmit("abc", "def");
                return Task.CompletedTask;
            }

            // public override Task<bool> DispatchEventAsync(object @event)
            // {
            //     throw new NotImplementedException();
            // }
        }

        private IEventHandlerContext _context;

        public BasicEventHandlerTests()
        {
            _context = Substitute.For<IEventHandlerContext>();
        }

        [Fact]
        public void Flush_ContextIsAlsoFlushed_Once()
        {
            var eventHandler = new BasicEventHandler<IEventHandlerContext>(_context, null, new NullFlushPolicy(), new EventJsonSerializer());

            eventHandler.FlushAsync(default);

            _context.Received(1).FlushAsync(default);
        }

        [Fact]
        public async Task DispatchEvent_EventIsRouterTotheCorrectFunction_Once()
        {
            var eventHandler = new DummyEventHandler(_context);

            await eventHandler.DispatchEventAsync(new DummyEvent(), default);

            Assert.True(eventHandler.Called);
        }

        [Fact]
        public async Task DispatchEvent_NoRoute_Ignore()
        {
            var eventHandler = new DummyEventHandler(_context);

            await eventHandler.DispatchEventAsync(new DummyEvent(), default);
        }

        [Fact]
        public async Task TakeEmittedEvents_EmptyAfterUse_Empty()
        {
            var eventHandler = new DummyEventHandler(_context);

            await eventHandler.DispatchEventAsync(new DummyEmitEvent(), default);
            var pass1 = eventHandler.TakeEmittedEvents().ToList();
            var pass2 = eventHandler.TakeEmittedEvents().ToList();

            Assert.NotEmpty(pass1);
            Assert.Empty(pass2);
        }

        [Fact]
        public async Task TakeEmittedOnSubmitEvents_EmptyAfterUse_Empty()
        {
            var eventHandler = new DummyEventHandler(_context);

            await eventHandler.DispatchEventAsync(new DummyEmitSubmitEvent(), default);

            var pass1 = eventHandler.TakeEmittedOnSubmitEvents().ToList();
            var pass2 = eventHandler.TakeEmittedOnSubmitEvents().ToList();

            Assert.NotEmpty(pass1);
            Assert.Empty(pass2);
        }        
    }
}