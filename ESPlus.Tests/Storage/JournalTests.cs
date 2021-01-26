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
        public void Create_NoJournal_InitializeJournalToZero()
        {
            _journal.Initialize();
            Assert.Equal(Position.Start, _journal.Checkpoint);
        }

        [Fact]
        public void Create_HasJournalWithCheckpoint_RealTimeMode()
        {
            _metadataStorage.Get<JournalLog>(PersistentJournal.JournalPath)
                .Returns(new JournalLog {Checkpoint = Position.Gen(57)});

            _journal.Initialize();

            Assert.Equal(Position.Gen(57), _journal.Checkpoint);
            Assert.Equal(SubscriptionMode.RealTime, _journal.SubscriptionMode);
        }

        [Fact]
        public void Create_HasJournalWithCheckpoint_ReplayMode()
        {
            _metadataStorage.Get<JournalLog>(PersistentJournal.JournalPath)
                .Returns(new JournalLog {Checkpoint = Position.Start});

            _journal.Initialize();

            Assert.Equal(Position.Start, _journal.Checkpoint);
            Assert.Equal(SubscriptionMode.Replay, _journal.SubscriptionMode);
        }

        [Fact]
        public void Flush_WriteJournal_JournalSaved()
        {
            _journal.Initialize();
            _journal.Checkpoint = Position.Gen(13);
            _journal.Flush();

            Received.InOrder(() =>
            {
                _metadataStorage.Received().Put(PersistentJournal.JournalPath,
                    Arg.Is<JournalLog>(p => p.Checkpoint.Equals(Position.Gen(13))));
                _metadataStorage.Received().Flush();
            });
        }
        
        [Fact]
        public void Get_AfterPut_Accessable()
        {
            _journal.Initialize();
            _journal.Checkpoint = Position.Start;
            _journal.Put("path", _payload);

            Assert.Equal(_payload, _journal.Get<object>("path"));
        }
        
        [Fact]
        public void Get_AfterPutAndFlush_Accessable()
        {
            _journal.Initialize();
            _journal.Checkpoint = Position.Start;
            _journal.Put("path", _payload);
            _journal.Flush();

            Assert.Equal(_payload, _journal.Get<object>("path"));
        }         
        
        [Fact]
        public void Get_AfterDelete_NotAccessable()
        {
            _journal.Initialize();
            _journal.Checkpoint = Position.Start;
            _journal.Put("path", _payload);
            _journal.Delete("path");

            Assert.Null(_journal.Get<object>("path"));
        }
        
        [Fact]
        public void Get_AfterDeleteAndFlush_NotAccessable()
        {
            _journal.Initialize();
            _journal.Checkpoint = Position.Start;
            _journal.Put("path", _payload);
            _journal.Flush();
            _journal.Delete("path");
            _journal.Flush();

            Assert.Null(_journal.Get<object>("path"));
        }            
    }
}