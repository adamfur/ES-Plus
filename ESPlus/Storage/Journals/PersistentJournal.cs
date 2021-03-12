using System;
using System.Collections.Generic;
using System.IO;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;
using System.Threading.Tasks;

namespace ESPlus.Storage
{
    public abstract class PersistentJournal : IJournaled
    {
        private bool _changed = false;
        private Position _checkpoint;        
        public const string JournalPath = "000Journal/000Journal.json";
        private readonly IStorage _metadataStorage;
        protected readonly IStorage DataStorage;
        public SubscriptionMode SubscriptionMode { get; private set; } = SubscriptionMode.RealTime;
        protected readonly Dictionary<StringPair, object> DataWriteCache = new Dictionary<StringPair, object>();
        protected HashSet<StringPair> Deletes { get; set; } = new HashSet<StringPair>();
        
        public Position Checkpoint
        {
            get => _checkpoint;
            set
            {
                _changed = true;
                _checkpoint = value;
            }
        }

        protected PersistentJournal(IStorage metadataStorage, IStorage dataStorage)
        {
            _metadataStorage = metadataStorage;
            DataStorage = dataStorage;
        }

        public async Task InitializeAsync()
        {
            await LoadJournal();
        }

        private async Task LoadJournal()
        {
            var journal = new JournalLog();

            try
            {
                journal = await _metadataStorage.GetAsync<JournalLog>("master", JournalPath) ?? new JournalLog();
            }
            catch (StorageNotFoundException)
            {
                // ignored
            }

            Console.WriteLine($"Journal Checkpoint: {journal.Checkpoint.AsHexString()}");

            Checkpoint = journal.Checkpoint;

            if (journal.Checkpoint.Equals(Position.Start))
            {
                SubscriptionMode = SubscriptionMode.Replay;
            }
            
            await PlayJournal(journal);
        }

        protected virtual Task PlayJournal(JournalLog journal)
        {
            return Task.CompletedTask;
        }

        public async Task FlushAsync()
        {
            if (_changed == false)
            {
                return;
            }

            await DoFlushAsync();
            Clean();
        }

        public virtual void Put<T>(string tenant, string path, T item)
        {
            var key = new StringPair(tenant, path);

            DataWriteCache[key] = item;
            Deletes.Remove(key);
            _changed = true;
        }

        public async Task<T> GetAsync<T>(string tenant, string path)
        {
            var key = new StringPair(tenant, path);

            if (Deletes.Contains(key))
            {
                throw new StorageNotFoundException();
            }

            if (DataWriteCache.TryGetValue(key, out object item1))
            {
                return (T) item1;
            }

            if (SubscriptionMode == SubscriptionMode.Replay)
            {
                // return default;
            }

            return await DataStorage.GetAsync<T>(tenant, path);
        }

        public async Task UpdateAsync<T>(string path, string tenant, Action<T> action)
        {
            var model = await GetAsync<T>(tenant, path);

            action(model);
            Put(tenant, path, model);
        }

        protected async Task WriteJournalAsync(Dictionary<StringPair, string> map, HashSet<StringPair> deletes)
        {
            var journal = new JournalLog
            {
                Checkpoint = Checkpoint,
                Map = new Dictionary<StringPair, string>(map),
                Deletes = new HashSet<StringPair>(deletes),
            };
            
            _metadataStorage.Put("master", JournalPath, journal);

            if (_metadataStorage != DataStorage)
            {
                await _metadataStorage.FlushAsync();
            }
        }

        protected async Task WriteToAsync(IStorage storage, Dictionary<StringPair, object> cache, HashSet<StringPair> deletes,
            string prefix = "")
        {
            foreach (var item in cache)
            {
                var destination = $"{prefix}{item.Key.Path}";

                storage.Put(item.Key.Tenant, destination, item.Value);
            }

            foreach (var item in deletes)
            {
                storage.Delete(item.Tenant, item.Path);
            }
            
            await storage.FlushAsync();
        }

        private void Clean()
        {
            DataWriteCache.Clear();
            Deletes.Clear();
            _changed = false;
            DoClean();
        }

        protected virtual void DoClean()
        {
        }

        protected virtual Task DoFlushAsync()
        {
            return Task.CompletedTask;
        }

        public void Reset()
        {
            DataStorage.Reset();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters)
        {
            return DataStorage.SearchAsync(tenant, parameters);
        }

        public virtual void Delete(string tenant, string path)
        {
            var key = new StringPair(tenant, path);

            _changed = true;
            DataWriteCache.Remove(key);
            Deletes.Add(key);
        }
    }
}