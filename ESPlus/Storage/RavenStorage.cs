using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using ESPlus.Misc;
using ESPlus.MoonGoose;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Operation = ESPlus.MoonGoose.Operation;

namespace ESPlus.Storage
{
	public class RavenStorage : IStorage
	{
		public IDocumentStore Store { get; }
		private Dictionary<StringPair, Document> Writes = new Dictionary<StringPair, Document>();
		private bool _dropped = false;

		public RavenStorage(string url, string database = "pliance")
		{
			Store = new DocumentStore
			{
				Urls = new[] { url },
				Database = database,
			}.Initialize();

			try
			{
				Store.Maintenance.ForDatabase(Store.Database).Send(new GetStatisticsOperation());
			}
			catch (DatabaseDoesNotExistException)
			{
				try
				{
					Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(Store.Database)));
				}
				catch (ConcurrencyException)
				{
				}
			}

			using var session = Store.OpenSession();
			session.Load<object>("hello");
		}

		public async Task FlushAsync(Position previousCheckpoint, Position checkpoint, CancellationToken cancellationToken)
		{
			Put("@", "CHECKPOINT", checkpoint);

			using var session = Store.OpenAsyncSession();

			foreach (var (pair, document) in Writes)
			{
				if (document.Operation == Operation.Delete)
				{
					session.Delete($"{pair.Tenant}:{pair.Path}");
				}
				else if (document.Operation == Operation.Save)
				{
					await session.StoreAsync(document.Item, $"{pair.Tenant}:{pair.Path}", cancellationToken);
				}
			}

			await session.SaveChangesAsync(cancellationToken);
			Writes = new Dictionary<StringPair, Document>();
		}

		public async Task<T> GetAsync<T>(string tenant, string path, CancellationToken cancellationToken)
		{
			if (_dropped)
			{
				return default;
			}
			
			var key = new StringPair(tenant, path);

			if (Writes.TryGetValue(key, out var resolved))
			{
				if (resolved.Operation == Operation.Save)
				{
					return (T)resolved.Item;
				}

				throw new StorageNotFoundException(key.ToString());
			}

			using var session = Store.OpenAsyncSession();
			var value = await session.LoadAsync<T>(id: $"{tenant}:{path}", cancellationToken);

			if (value is null)
			{
				throw new StorageNotFoundException($"{tenant}:{path}");
			}

			return value;
		}

		public virtual void Put<T>(string tenant, string path, T item)
		{
			var key = new StringPair(tenant, path);

			Writes[key] = new Document(key.Tenant, key.Path, item, Operation.Save);
		}

		public void Delete(string tenant, string path)
		{
			var key = new StringPair(tenant, path);

			Writes[key] = new Document(key.Tenant, key.Path, null, Operation.Delete);
		}

		public void Reset()
		{
		}

		public IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public async Task<Position> ChecksumAsync(CancellationToken cancellationToken)
		{
			try
			{
				return await GetAsync<Position>("@", "CHECKPOINT", cancellationToken);
			}
			catch (StorageNotFoundException)
			{
				return Position.Start;
			}
		}

		public async IAsyncEnumerable<byte[]> List<T>(string tenant, int size, int no, Box<int> total, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			using var session = Store.OpenAsyncSession();

			var list = await session.Query<T>().Take(size).Skip(size * no).ToListAsync(cancellationToken);
			var count = await session.Query<T>().CountAsync(cancellationToken);

			total.Value = count;
			foreach (var item in list)
			{
				cancellationToken.ThrowIfCancellationRequested();

				yield return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
			}
		}

		public IQueryable<T> Query<T>(string tenant, CancellationToken cancellationToken)
		{
			var session = Store.OpenSession();

			SessionScope.Set(session);

			return session.Query<T>();
		}

		public Task EvictCache()
		{
			Writes = new Dictionary<StringPair, Document>();
			return Task.CompletedTask;
		}

		public void DropDatabase()
		{
			Console.WriteLine($"RavenStorage: Trying to delete: [{Store.Database}]");

			try
			{
				var parameters = new DeleteDatabasesOperation.Parameters
				{
					DatabaseNames = new[] { Store.Database },
					HardDelete = true,
					TimeToWaitForConfirmation = new TimeSpan(0, 0, 1),
				};

				_dropped = true;
				Writes.Clear();
				Store.Maintenance.Server.Send(new DeleteDatabasesOperation(parameters));
			}
			catch (DatabaseDoesNotExistException)
			{
				// ignored
			}
		}
	}
}