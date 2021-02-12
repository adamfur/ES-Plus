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
        protected readonly IStorage _dataStorage;
        public SubscriptionMode SubscriptionMode { get; private set; } = SubscriptionMode.RealTime;
        // private readonly ConditionalWeakTable<string, HasObjectId> _cache = new ConditionalWeakTable<string, HasObjectId>();
        protected readonly Dictionary<StringPair, object> _dataWriteCache = new Dictionary<StringPair, object>();
        protected HashSet<StringPair> _deletes { get; set; } = new HashSet<StringPair>();
        
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
            _dataStorage = dataStorage;
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
                journal = await _metadataStorage.GetAsync<JournalLog>(JournalPath, "master") ?? new JournalLog();
            }
            catch (Exception)
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

        public virtual void Put<T>(string path, string tenant, T item)
        {
            var key = new StringPair(path, tenant);

            _dataWriteCache[key] = item;
            _deletes.Remove(key);
            _changed = true;
        }

        public async Task<T> GetAsync<T>(string path, string tenant)
        {
            var key = new StringPair(path, tenant);
            
            try
            {
                if (_dataWriteCache.TryGetValue(key, out object item1))
                {
                    return (T) item1;
                }

                if (SubscriptionMode == SubscriptionMode.Replay)
                {
//                    return default;
                }

                return await _dataStorage.GetAsync<T>(path, tenant);
            }
            catch (DirectoryNotFoundException)
            {
                return default;
            }
        }

        public async Task UpdateAsync<T>(string path, string tenant, Action<T> action)
        {
            var model = await GetAsync<T>(path, tenant);

            if (model is null)
            {
                throw new Exception($"{nameof(PersistentJournal)}::Update, Path: {path}. model is null, tenant: {tenant ?? "@"}");
            }

            action(model);
            Put(path, tenant, model);
        }

        protected async Task WriteJournalAsync(Dictionary<StringPair, string> map, HashSet<StringPair> deletes)
        {
            var journal = new JournalLog
            {
                Checkpoint = Checkpoint,
                Map = new Dictionary<StringPair, string>(map),
                Deletes = new HashSet<StringPair>(deletes),
            };
            
            _metadataStorage.Put(JournalPath, "master", journal);

            if (_metadataStorage != _dataStorage)
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

                storage.Put(destination, item.Key.Tenant, item.Value);
            }

            foreach (var item in deletes)
            {
                storage.Delete(item.Path, item.Tenant);
            }
            
            await storage.FlushAsync();
        }

        private void Clean()
        {
            _dataWriteCache.Clear();
            _deletes.Clear();
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
            _dataStorage.Reset();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(long[] parameters, string tenant)
        {
            return _dataStorage.SearchAsync(parameters, tenant);
        }

        public virtual void Delete(string path, string tenant)
        {
            var key = new StringPair(path, tenant);

            _changed = true;
            _dataWriteCache.Remove(key);
            _deletes.Add(key);
        }
    }
}