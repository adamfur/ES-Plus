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
        protected readonly IStorage _metadataStorage;
        protected readonly IStorage _stageStorage;
        protected readonly IStorage _dataStorage;
        protected readonly object _payload;

        public JournalTests()
        {
            _metadataStorage = Substitute.For<IStorage>();
            _stageStorage = Substitute.For<IStorage>();
            _dataStorage = Substitute.For<IStorage>();
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
            _metadataStorage.GetAsync<JournalLog>("master", PersistentJournal.JournalPath, CancellationToken.None)
                .Returns(new JournalLog {Checkpoint = Position.Gen(57)});

            await _journal.InitializeAsync();

            Assert.Equal(Position.Gen(57), _journal.Checkpoint);
            Assert.Equal(SubscriptionMode.RealTime, _journal.SubscriptionMode);
        }

        [Fact]
        public async Task Create_HasJournalWithCheckpoint_ReplayMode()
        {
            _metadataStorage.GetAsync<JournalLog>(null, PersistentJournal.JournalPath, CancellationToken.None)
                .Returns(new JournalLog {Checkpoint = Position.Start});

            await _journal.InitializeAsync();

            Assert.Equal(Position.Start, _journal.Checkpoint);
            Assert.Equal(SubscriptionMode.Replay, _journal.SubscriptionMode);
        }

        [Fact]
        public async Task Flush_WriteJournal_JournalSaved()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Gen(13);
            await _journal.FlushAsync(CancellationToken.None);

            Received.InOrder(() =>
            {
                _metadataStorage.Received().Put("master",
                    PersistentJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint.Equals(Position.Gen(13))));
                _metadataStorage.Received().FlushAsync(CancellationToken.None);
            });
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

            _dataStorage.GetAsync<object>(null, "path", CancellationToken.None).Returns(_payload);
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