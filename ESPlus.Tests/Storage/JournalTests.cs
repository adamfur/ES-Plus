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
            Assert.Equal(0L, _journal.Checkpoint);
        }

        [Fact]
        public void Create_HasJournalWithCheckpoint_RealTimeMode()
        {
            _metadataStorage.Get(PersistantJournal.JournalPath).Returns(new JournalLog { Checkpoint = 57L });

            _journal.Initialize();

            Assert.Equal(57L, _journal.Checkpoint);
            Assert.Equal(SubscriptionMode.RealTime, _journal.SubscriptionMode);
        }

        [Fact]
        public void Create_HasJournalWithCheckpoint_ReplayMode()
        {
            _metadataStorage.Get(PersistantJournal.JournalPath).Returns(new JournalLog { Checkpoint = 0L });

            _journal.Initialize();

            Assert.Equal(0L, _journal.Checkpoint);
            Assert.Equal(SubscriptionMode.Replay, _journal.SubscriptionMode);
        }

        //[Fact]
        public void Flush_WriteJournal_JournalSaved()
        {
            var payload = new object();

            _journal.Initialize();
            _journal.Checkpoint = 13L;
            _journal.Flush();

            Received.InOrder(() =>
            {
                _metadataStorage.Received().Put(PersistantJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint == 13L));
                _metadataStorage.Received().Flush();
            });
        }
    }
}