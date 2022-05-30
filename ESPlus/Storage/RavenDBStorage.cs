using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using ESPlus.Interfaces;
using ESPlus.MoonGoose;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace ESPlus.Storage
{
	public class RavenDBStorage : IStorage
	{
		private readonly IDocumentStore _store;

		public RavenDBStorage(IDocumentStore store)
		{
			_store = store;
		}

        public static IDocumentStore CreateDocumentStore(string url, string database = "pliance")
        {
			var store = new DocumentStore()
			{
				Urls = new[] { url },
				Database = database,
			}.Initialize();

			try
			{
				store.Maintenance.ForDatabase(store.Database).Send(new GetStatisticsOperation());
			}
			catch (DatabaseDoesNotExistException)
			{
				try
				{
					store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(store.Database)));
				}
				catch (ConcurrencyException)
				{
				}
			}
			
			using var session = store.OpenSession();
			session.Load<object>("hello");

			return store;
		}

		public void Put<T>(string tenant, string path, T item)
		{
			using var session = _store.OpenSession();

			session.Store(
				id: $"{tenant}:{path}",
				entity: item
			);

			session.SaveChanges();
		}

		public void Delete(string tenant, string path)
		{
			using var session = _store.OpenSession();

			session.Delete(id: $"{tenant}:{path}");

			session.SaveChanges();
		}

		public async Task<T> GetAsync<T>(string tenant, string path, CancellationToken cancellationToken)
		{
			using var session = _store.OpenAsyncSession();

			var value = await session.LoadAsync<T>(id: $"{tenant}:{path}", cancellationToken);
            if (value is null)
            {
				throw new StorageNotFoundException();
			}

			return value;
		}

		public void Reset()
		{
		}

		public IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters, CancellationToken cancellationToken)
			=> null;

		public Task<Position> ChecksumAsync(CancellationToken cancellationToken)
            => Task.FromResult(Position.Start);

		public async IAsyncEnumerable<byte[]> List(string tenant, int size, int no, Box<int> total, CancellationToken cancellationToken)
		{
			using var session = _store.OpenAsyncSession();
			var list = await session.Query<dynamic>().Take(size).Skip(size * no).ToListAsync(cancellationToken);
			var count = await session.Query<dynamic>().CountAsync(cancellationToken);

			total.Value = count;
			foreach (var item in list)
			{
				yield return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
			}
		}

		public Task EvictCache() => Task.CompletedTask;

		public Task FlushAsync(Position previousCheckpoint, Position checkpoint, CancellationToken cancellationToken)
			=> Task.CompletedTask;
	}
}