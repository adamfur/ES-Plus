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
            _metadataStorage.GetAsync<JournalLog>(PersistentJournal.JournalPath, "master")
                .Returns(new JournalLog {Checkpoint = Position.Gen(57)});

            await _journal.InitializeAsync();

            Assert.Equal(Position.Gen(57), _journal.Checkpoint);
            Assert.Equal(SubscriptionMode.RealTime, _journal.SubscriptionMode);
        }

        [Fact]
        public async Task Create_HasJournalWithCheckpoint_ReplayMode()
        {
            _metadataStorage.GetAsync<JournalLog>(PersistentJournal.JournalPath, null)
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
            await _journal.FlushAsync();

            Received.InOrder(() =>
            {
                _metadataStorage.Received().Put(PersistentJournal.JournalPath, "master",
                    Arg.Is<JournalLog>(p => p.Checkpoint.Equals(Position.Gen(13))));
                _metadataStorage.Received().FlushAsync();
            });
        }
        
        [Fact]
        public async Task Get_AfterPut_Accessable()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Start;
            _journal.Put("path", null, _payload);

            var stringPair1 = new StringPair(null, "path").GetHashCode();
            var stringPair2 = new StringPair(null, "path").GetHashCode();

            Assert.Equal(_payload, await _journal.GetAsync<object>("path", null));
        }
        
        [Fact]
        public async Task Get_AfterPutAndFlush_Accessable()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Start;
            _journal.Put("path", null, _payload);
            await _journal.FlushAsync();

            _dataStorage.GetAsync<object>("path", null).Returns(_payload);
            Assert.Equal(_payload, await _journal.GetAsync<object>("path", null));
        }         
        
        [Fact]
        public async Task Get_AfterDelete_NotAccessable()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Start;
            _journal.Put("path", null, _payload);
            _journal.Delete("path", null);

            Assert.Null(await _journal.GetAsync<object>("path", null));
        }
        
        [Fact]
        public async Task Get_AfterDeleteAndFlush_NotAccessable()
        {
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Start;
            _journal.Put("path", null, _payload);
            await _journal.FlushAsync();
            _journal.Delete("path", null);
            await _journal.FlushAsync();

            Assert.Null(await _journal.GetAsync<object>("path", null));
        }            
    }
}