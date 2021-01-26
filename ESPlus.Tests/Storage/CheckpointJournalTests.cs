using ESPlus.Storage;
using ESPlus.Subscribers;
using NSubstitute;
using Xunit;

namespace ESPlus.Tests.Storage
{
    public class CheckpointJournalTests : JournalTests
    {
        protected override IJournaled Create()
        {
            return new CheckpointJournal(_metadataStorage, _dataStorage);
        }

        [Fact]
        public void Flush_PutFile_WriteFirstToStageThenJournalThenStorage()
        {
            // Arrange
            var source = "stage/1/file1";
            var destination = "prod/file1";
            var payload = new object();

            _stageStorage.Get<object>(source).Returns(_payload);

            // Act
            _journal.Initialize();
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(destination, payload);
            _journal.Flush();

            // Assert
            Received.InOrder(() =>
            {
                _dataStorage.Received().Put(destination, payload);
                _dataStorage.Received().Flush();
                _metadataStorage.Received().Put(PersistentJournal.JournalPath, Arg.Any<JournalLog>());
                _metadataStorage.Received().Flush();
            });
        }
    }
}