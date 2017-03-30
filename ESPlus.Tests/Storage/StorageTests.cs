using System.Collections.Generic;
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
                _metadataStorage.Received().Put(Arg.Any<string>(), Arg.Is<JournalLog>(p => p.Checkpoint == 13L));
                _metadataStorage.Received().Flush();
            });
        }
    }

    public class ReplayableJournalTests : JournalTests
    {
        protected override IJournaled Create()
        {
            return new ReplayableJournal(_metadataStorage, _stageStorage, _dataStorage);
        }

        [Fact]
        public void Flush_ReplayJournal_MoveFromStageToPersistant()
        {
            // Arrange
            var source = "stage/1/file1";
            var destination = "prod/file1";
            var payload = new object();
            var replayLog = new JournalLog
            {
                Checkpoint = 0L,
                Map = new Dictionary<string, string>
                {
                    [source] = destination
                }
            };
            
            _metadataStorage.Get(PersistantJournal.JournalPath).Returns(replayLog);
            _stageStorage.Get(source).Returns(_payload);

            // Act
            _journal.Initialize();

            // Assert
            Received.InOrder(() =>
            {
                _stageStorage.Received().Get(Arg.Any<string>());
                _dataStorage.Received().Put(Arg.Any<string>(), Arg.Any<object>());
                _dataStorage.Received().Flush();
            });
        }

        [Fact]
        public void Flush_PutFile_WriteFirstToStageThenJournalThenStorage()
        {
            // Arrange
            var source = "stage/1/file1";
            var destination = "prod/file1";
            var payload = new object();
            
            _stageStorage.Get(source).Returns(_payload);

            // Act
            _journal.Initialize();
            _journal.Checkpoint = 12;
            _journal.Put(destination, payload);
            _journal.Flush();

            // Assert
            Received.InOrder(() =>
            {
                _stageStorage.Received().Put(destination, payload);
                _stageStorage.Received().Flush();
                _metadataStorage.Received().Put(PersistantJournal.JournalPath, Arg.Any<JournalLog>());
                _metadataStorage.Received().Flush();
                _dataStorage.Received().Put(destination, payload);
                _dataStorage.Received().Flush();
            });
        }        
    }

    public class CheckpointJournalTests : JournalTests
    {
        protected override IJournaled Create()
        {
            return new CheckpointJournal(_metadataStorage, _dataStorage);
        }
    }
}