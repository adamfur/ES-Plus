using System.Threading;
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

            _stageStorage.GetAsync<object>(null, source, CancellationToken.None).Returns(_payload);

            // Act
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(null, destination, payload);
            await _journal.FlushAsync(CancellationToken.None);

            // Assert
            Received.InOrder(() =>
            {
                _dataStorage.Received().Put(null, destination, payload);
                _dataStorage.Received().FlushAsync(CancellationToken.None);
                _metadataStorage.Received().Put("master", PersistentJournal.JournalPath, Arg.Any<JournalLog>());
                _metadataStorage.Received().FlushAsync(CancellationToken.None);
            });
        }
    }
}