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
                : base(context, null, new NullFlushPolicy())
            {
            }

            public Task Apply(DummyEvent @event)
            {
                Called = true;
                return Task.CompletedTask;
            }

            public Task Apply(DummyEmitEvent @event)
            {
                Emit(new object());
                return Task.CompletedTask;
            }

            public Task Apply(DummyEmitSubmitEvent @event)
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
            var eventHandler = new BasicEventHandler<IEventHandlerContext>(_context, null, new NullFlushPolicy());

            eventHandler.FlushAsync(CancellationToken.None);

            _context.Received(1).FlushAsync(CancellationToken.None);
        }

        [Fact]
        public async Task DispatchEvent_EventIsRouterTotheCorrectFunction_Once()
        {
            var eventHandler = new DummyEventHandler(_context);

            await eventHandler.DispatchEventAsync(new DummyEvent(), CancellationToken.None);

            Assert.True(eventHandler.Called);
        }

        [Fact]
        public async Task DispatchEvent_NoRoute_Ignore()
        {
            var eventHandler = new DummyEventHandler(_context);

            await eventHandler.DispatchEventAsync(new DummyEvent(), CancellationToken.None);
        }

        [Fact]
        public async Task TakeEmittedEvents_EmptyAfterUse_Empty()
        {
            var eventHandler = new DummyEventHandler(_context);

            await eventHandler.DispatchEventAsync(new DummyEmitEvent(), CancellationToken.None);
            var pass1 = eventHandler.TakeEmittedEvents().ToList();
            var pass2 = eventHandler.TakeEmittedEvents().ToList();

            Assert.NotEmpty(pass1);
            Assert.Empty(pass2);
        }

        [Fact]
        public async Task TakeEmittedOnSubmitEvents_EmptyAfterUse_Empty()
        {
            var eventHandler = new DummyEventHandler(_context);

            await eventHandler.DispatchEventAsync(new DummyEmitSubmitEvent(), CancellationToken.None);

            var pass1 = eventHandler.TakeEmittedOnSubmitEvents().ToList();
            var pass2 = eventHandler.TakeEmittedOnSubmitEvents().ToList();

            Assert.NotEmpty(pass1);
            Assert.Empty(pass2);
        }        
    }
}