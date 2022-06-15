using Xunit;
using Raven.Client.Documents;
using System.Threading.Tasks;
using ESPlus.Storage;
using System.Threading;
using Raven.Client.Documents.Operations;
using ESPlus.Interfaces;

namespace ESPlus.IntegrationTests
{
	// ! docker run -p 8081:8080 -e RAVEN_ARGS='--Security.UnsecuredAccessAllowed=PublicNetwork --log-to-console' -e "RAVEN_Setup_Mode=None" -e "RAVEN_ServerUrl=http://0.0.0.0:8080" -e "RAVEN_License_Eula_Accepted=true" ravendb/ravendb:ubuntu-latest
	public class RavenDBStorageTests
    {
		private readonly IDocumentStore _store;
		private readonly RavenDBStorage _storage;

		public RavenDBStorageTests()
		{
			_store = RavenDBStorage.CreateDocumentStore("http://localhost:8081", "pliance");
			_storage = new(_store);
		}

		private class Document
		{
            public string Property { get; set; }
		}

		[Fact]
		public void Ensure_Database_Exists()
		{
			_store.Maintenance.ForDatabase(_store.Database).Send(new GetStatisticsOperation());
		}

		[Fact]
        public async Task Roundtrip_Should_Be_Equivalent()
        {
            // Arrange
			var expected = new Document()
			{
                Property = "abc"
			};

            // Act
			_storage.Put("tenant", "id", expected);

			var actual = await _storage.GetAsync<Document>("tenant", "id", CancellationToken.None);

			// Assert
			Assert.Equal(expected.Property, actual.Property);
		}

        [Fact]
        public async Task Get_Missing_Should_Throw()
        {
			// Arrange
			// Act
			//Assert
			await Assert.ThrowsAsync<StorageNotFoundException>(() => _storage.GetAsync<Document>("tenant", "missing", CancellationToken.None));
		}

		[Fact]
		public void Delete_Missing_Should_Pass()
		{
			// Arrange
			// Act
			//Assert
			_storage.Delete("tenant", "missing");
		}

		[Fact]
		public async Task Get_Deleted_Should_Throw()
		{
			// Arrange
			var document = new Document()
			{
				Property = "abc"
			};
			_storage.Put("tenant", "id", document);
			_storage.Delete("tenant", "id");

			// Act
			//Assert
			await Assert.ThrowsAsync<StorageNotFoundException>(() => _storage.GetAsync<Document>("tenant", "id", CancellationToken.None));
		}
	}
}