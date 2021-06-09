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
            return new PersistentJournal(_storage);
        }

        [Fact]
        public async Task Flush_PutFile_WriteFirstToStageThenJournalThenStorage()
        {
            // Arrange
            var source = "stage/1/file1";
            var destination = "prod/file1";
            var payload = new object();

            // Act
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(null, destination, payload);
            await _journal.FlushAsync(default);

            // Assert
            Received.InOrder(() =>
            {
                _storage.Received().Put(null, destination, payload);
                _storage.Received().FlushAsync(Arg.Is<Position>(p => Position.Start.Equals(p)), Arg.Is<Position>(p => Position.Gen(12).Equals(p)), default);
            });
        }
    }
}