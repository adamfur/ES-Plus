using System.Threading;
using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;
using ESPlus.Storage;
using NSubstitute;
using Xunit;

namespace ESPlus.Tests.Storage
{
    public abstract class JournalTests
    {
        protected readonly IJournaled _journal;
        protected readonly IStorage _storage;
        protected readonly object _payload;

        public JournalTests()
        {
            _storage = Substitute.For<IStorage>();
            _storage.ChecksumAsync(default).Returns(Task.FromResult(Position.Start));
            _journal = Create();
            _payload = new object();
        }

        protected abstract IJournaled Create();

        [Fact]
        public async Task Create_NoJournal_InitializeJournalToZero()
        {
            await _journal.InitializeAsync();
            Assert.Equal(Position.Start, _journal.Checkpoint);
        }

        [Fact]
        public async Task Create_HasJournalWithCheckpoint_RealTimeMode()
        {
            _storage.ChecksumAsync(default).Returns(Task.FromResult(Position.Gen(57)));
        
            await _journal.InitializeAsync();
        
            Assert.Equal(Position.Gen(57), _journal.Checkpoint);
            Assert.Equal(SubscriptionMode.RealTime, _journal.SubscriptionMode);
        }
        
        [Fact]
        public async Task Create_HasJournalWithCheckpoint_ReplayMode()
        {
            _storage.ChecksumAsync(default).Returns(Task.FromResult(Position.Start));
        
            await _journal.InitializeAsync();
        
            Assert.Equal(Position.Start, _journal.Checkpoint);
            Assert.Equal(SubscriptionMode.Replay, _journal.SubscriptionMode);
        }
        
        [Fact]
        public async Task Flush_WriteJournal_JournalSaved()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Gen(13);
            await _journal.FlushAsync(default);
        
            await _storage.Received().FlushAsync(Arg.Is<Position>(p => Position.Start.Equals(p)), Arg.Is<Position>(p => Position.Gen(13).Equals(p)), default);
        }
        
        [Fact]
        public async Task Get_AfterPut_Accessable()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Start;
            _journal.Put(null, "path", _payload);

            var stringPair1 = new StringPair(null, "path").GetHashCode();
            var stringPair2 = new StringPair(null, "path").GetHashCode();

            Assert.Equal(_payload, await _journal.GetAsync<object>(null, "path", CancellationToken.None));
        }
        
        [Fact]
        public async Task Get_AfterPutAndFlush_Accessable()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Start;
            _journal.Put(null, "path", _payload);
            await _journal.FlushAsync(CancellationToken.None);

            _storage.GetAsync<object>(null, "path", CancellationToken.None).Returns(_payload);
            Assert.Equal(_payload, await _journal.GetAsync<object>(null, "path", CancellationToken.None));
        }         
        
        [Fact]
        public async Task Get_AfterDelete_NotAccessable()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Start;
            _journal.Put(null, "path", _payload);
            _journal.Delete(null, "path");

            Assert.Null(await _journal.GetAsync<object>(null, "path", CancellationToken.None));
        }
        
        [Fact]
        public async Task Get_AfterDeleteAndFlush_NotAccessable()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Start;
            _journal.Put(null, "path", _payload);
            await _journal.FlushAsync(CancellationToken.None);
            _journal.Delete(null, "path");
            await _journal.FlushAsync(CancellationToken.None);

            Assert.Null(await _journal.GetAsync<object>(null, "path", CancellationToken.None));
        }            
    }
}