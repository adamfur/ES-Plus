using System;
using System.Collections.Generic;
using System.Threading;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;
using System.Threading.Tasks;
using ESPlus.MoonGoose;
using System.Linq;

namespace ESPlus.Storage
{
    public class PersistentJournal : IJournaled
    {
        private bool _changed = false;
        private Position _checkpoint = Position.Start;        
        private readonly IStorage _storage;
        public SubscriptionMode SubscriptionMode { get; set; } = SubscriptionMode.RealTime;
        private readonly Dictionary<StringPair, object> _writeCache = new Dictionary<StringPair, object>();
        private readonly HashSet<StringPair> _deletes = new HashSet<StringPair>();
        private Position _previousCheckpoint = Position.Start;
        
        public Position Checkpoint
        {
            get => _checkpoint;
            set
            {
                _changed = true;
                _checkpoint = value;
            }
        }

        public PersistentJournal(IStorage storage)
        {
            _storage = storage;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await LoadJournal(cancellationToken);
        }

        private async Task LoadJournal(CancellationToken cancellationToken)
        {
            _previousCheckpoint = Checkpoint = await _storage.ChecksumAsync(cancellationToken);

            Console.WriteLine($"Journal Checkpoint: {Checkpoint.AsHexString()}");

            if (Checkpoint.Equals(Position.Start))
            {
                SubscriptionMode = SubscriptionMode.Replay;
            }
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_changed == false)
            {
                return;
            }

            foreach (var item in _writeCache)
            {
                var destination = item.Key.Path;

                _storage.Put(item.Key.Tenant, destination, item.Value);
            }

            foreach (var item in _deletes)
            {
                _storage.Delete(item.Tenant, item.Path);
            }
            
            await _storage.FlushAsync(_previousCheckpoint, Checkpoint, cancellationToken);
            _previousCheckpoint = Checkpoint;
            Clean();
        }

        public virtual void Put<T>(string tenant, string path, T item)
        {
            var key = new StringPair(tenant, path);

            _writeCache[key] = item;
            _deletes.Remove(key);
            _changed = true;
        }

        public async Task<T> GetAsync<T>(string tenant, string path, CancellationToken cancellationToken)
        {
            var key = new StringPair(tenant, path);

            if (_deletes.Contains(key))
            {
                throw new StorageNotFoundException();
            }

            if (_writeCache.TryGetValue(key, out object item1))
            {
                return (T) item1;
            }

            if (SubscriptionMode == SubscriptionMode.Replay)
            {
                // return default;
            }

            return await _storage.GetAsync<T>(tenant, path, cancellationToken);
        }

        public async Task UpdateAsync<T>(string tenant, string path, Action<T> action, CancellationToken cancellationToken)
        {
            var model = await GetAsync<T>(tenant, path, cancellationToken);

            action(model);
            Put(tenant, path, model);
        }

        private void Clean()
        {
            _writeCache.Clear();
            _deletes.Clear();
            _changed = false;
        }

        public void Reset()
        {
            _storage.Reset();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters, CancellationToken cancellationToken)
        {
            return _storage.SearchAsync(tenant, parameters, cancellationToken);
        }

        public Task<Position> ChecksumAsync(CancellationToken cancellationToken)
        {
            return _storage.ChecksumAsync(cancellationToken);
        }

        public IAsyncEnumerable<byte[]> List<T>(string tenant, int size, int no, Box<int> total, CancellationToken cancellationToken)
        {
            return _storage.List<T>(tenant, size, no, total, cancellationToken);
        }

		public IQueryable<T> Query<T>(string tenant, CancellationToken cancellationToken)
		{
			return _storage.Query<T>(tenant, cancellationToken);
		}

        public async Task EvictCache()
        {
            await _storage.EvictCache();
            await InitializeAsync();
        }

        public virtual void Delete(string tenant, string path)
        {
            var key = new StringPair(tenant, path);

            _changed = true;
            _writeCache.Remove(key);
            _deletes.Add(key);
        }
	}
}