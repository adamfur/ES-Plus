using System;
using System.Collections.Generic;
using System.IO;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;
using System.Runtime.CompilerServices;
using ESPlus.Extentions;

namespace ESPlus.Storage
{
    public abstract class PersistentJournal : IJournaled
    {
        public const string JournalPath = "000Journal/000Journal.json";
        private readonly IStorage _metadataStorage;
        protected readonly IStorage _dataStorage;
        public SubscriptionMode SubscriptionMode { get; private set; } = SubscriptionMode.RealTime;
        // private readonly ConditionalWeakTable<string, HasObjectId> _cache = new ConditionalWeakTable<string, HasObjectId>();
        protected readonly Dictionary<string, object> _dataWriteCache = new Dictionary<string, object>();
        protected HashSet<string> _deletes { get; set; } = new HashSet<string>();
        
        public Position Checkpoint
        {
            get => _checkpoint;
            set
            {
                _changed = true;
                _checkpoint = value;
            }
        }

        private bool _changed = false;
        private Position _checkpoint;

        protected PersistentJournal(IStorage metadataStorage, IStorage dataStorage)
        {
            _metadataStorage = metadataStorage;
            _dataStorage = dataStorage;
        }

        public void Initialize()
        {
            LoadJournal();
        }

        private void LoadJournal()
        {
            JournalLog journal;

            Console.WriteLine("LoadJournal()");
            try
            {
                journal = _metadataStorage.Get<JournalLog>(JournalPath);
                Console.WriteLine($"IsNull {journal == null}");
                journal = journal ?? new JournalLog();
                Console.WriteLine($"Journal Read Success: {journal.Checkpoint.AsHexString()}");
            }
            catch (Exception)
            {
                journal = new JournalLog();
                Console.WriteLine($"Journal Read Failed: {journal.Checkpoint}");
            }
            
            Checkpoint = journal.Checkpoint;

            if (journal.Checkpoint.Equals(Position.Start))
            {
                SubscriptionMode = SubscriptionMode.Replay;
            }
            
            PlayJournal(journal);
        }

        protected virtual void PlayJournal(JournalLog journal)
        {
        }

        public void Flush()
        {
            // Console.WriteLine(" -- Journal flush");
            if (_changed == false)
            {
                return;
            }

            DoFlush();
            Clean();
        }

        public virtual void Put<T>(string path, T item)
        {
            // _cache.AddOrUpdate(destination, item);
            _dataWriteCache[path] = item;
            _deletes.Remove(path);
            _changed = true;
        }

        public T Get<T>(string path)
        {
            try
            {
                if (_dataWriteCache.TryGetValue(path, out object item1))
                {
                    return (T) item1;
                }

                // if (_cache.TryGetValue(path, out HasObjectId item2))
                // {
                //     return item2 as T;
                // }

                if (SubscriptionMode == SubscriptionMode.Replay)
                {
//                    return default;
                }

                var data = _dataStorage.Get<T>(path);

                // _cache.AddOrUpdate(path, data);
                return data;
            }
            catch (DirectoryNotFoundException)
            {
                return default;
            }
        }

        public void Update<T>(string path, Action<T> action)
        {
            var model = Get<T>(path);

            if (model is null)
            {
                throw new Exception($"{nameof(PersistentJournal)}::Update, Path: {path}. model is null");
            }

            action(model);
            Put(path, model);
        }

        protected void WriteJournal(Dictionary<string, string> map, HashSet<string> deletes)
        {
            var journal = new JournalLog
            {
                Checkpoint = Checkpoint,
                Map = new Dictionary<string, string>(map),
                Deletes = new HashSet<string>(deletes),
            };
            
            _metadataStorage.Put(JournalPath, journal);

            if (_metadataStorage != _dataStorage)
            {
                _metadataStorage.Flush();
            }

            // Console.WriteLine($"Put Journal {Checkpoint}");
        }

        protected void WriteTo(IStorage storage, Dictionary<string, object> cache, HashSet<string> deletes,
            string prefix = "")
        {
            foreach (var item in cache)
            {
                var destination = $"{prefix}{item.Key}";
                var payload = item.Value;

                storage.Put(destination, payload);
            }

            foreach (var item in deletes)
            {
                storage.Delete(item);
            }
            
            storage.Flush();
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

        protected virtual void DoFlush()
        {
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(long[] parameters)
        {
            return _dataStorage.SearchAsync(parameters);
        }

        public virtual void Delete(string path)
        {
//            Console.WriteLine($"PersistantJournal delete: {path}");
            _changed = true;
            // _cache.Remove(path);
            _dataWriteCache.Remove(path);
            _deletes.Add(path);
        }
    }
}