using System.Collections.Generic;
using ESPlus.Storage;
using NSubstitute;
using Xunit;

namespace ESPlus.Tests.Storage
{
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

        // [Fact]
        // public void Flush_PutFileDoubleFlush_WriteFirstToStageThenJournalThenStorage()
        // {
        //     // Arrange
        //     var source = "stage/1/file1";
        //     var destination = "prod/file1";
        //     var payload = new object();

        //     _stageStorage.Get(source).Returns(_payload);

        //     // Act
        //     _journal.Initialize();
        //     _journal.Checkpoint = 12;
        //     _journal.Put(destination, payload);
        //     _journal.Flush();
        //     _journal.Flush();

        //     // Assert
        //     Received.InOrder(() =>
        //     {
        //         _stageStorage.Received().Put(destination, payload);
        //         _stageStorage.Received().Flush();
        //         _metadataStorage.Received().Put(PersistantJournal.JournalPath, Arg.Any<JournalLog>());
        //         _metadataStorage.Received().Flush();
        //         _dataStorage.Received().Put(destination, payload);
        //         _dataStorage.Received().Flush();
        //     });
        // }                
    }
}