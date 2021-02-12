using System.Threading.Tasks;
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
        public async Task Flush_PutFile_WriteFirstToStageThenJournalThenStorage()
        {
            // Arrange
            var source = "stage/1/file1";
            var destination = "prod/file1";
            var payload = new object();

            _stageStorage.GetAsync<object>(source, null).Returns(_payload);

            // Act
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(destination, null, payload);
            await _journal.FlushAsync();

            // Assert
            Received.InOrder(() =>
            {
                _dataStorage.Received().Put(destination, null, payload);
                _dataStorage.Received().FlushAsync();
                _metadataStorage.Received().Put(PersistentJournal.JournalPath, "master", Arg.Any<JournalLog>());
                _metadataStorage.Received().FlushAsync();
            });
        }
    }
}