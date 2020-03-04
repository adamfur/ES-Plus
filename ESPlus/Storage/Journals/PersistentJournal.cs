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
        private readonly ConditionalWeakTable<string, HasObjectId> _cache = new ConditionalWeakTable<string, HasObjectId>();
        protected readonly Dictionary<string, HasObjectId> _dataWriteCache = new Dictionary<string, HasObjectId>();
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

            if (journal.Checkpoint.Equals(Position.Begin))
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
            if (_changed == false)
            {
                return;
            }

            DoFlush();
            Clean();
        }

        public virtual void Put(string destination, HasObjectId item)
        {
            _cache.AddOrUpdate(destination, item);
            _dataWriteCache[destination] = item;
            _deletes.Remove(destination);
            _changed = true;
        }

        public T Get<T>(string path)
            where T : HasObjectId
        {
            try
            {
                if (_dataWriteCache.TryGetValue(path, out HasObjectId item1))
                {
                    return item1 as T;
                }

                if (_cache.TryGetValue(path, out HasObjectId item2))
                {
                    return item2 as T;
                }

                if (SubscriptionMode == SubscriptionMode.Replay)
                {
//                    return default;
                }

                var data = _dataStorage.Get<T>(path);

                _cache.AddOrUpdate(path, data);
                return data;
            }
            catch (DirectoryNotFoundException)
            {
                return default;
            }
        }

        public void Update<T>(string path, Action<T> action) where T : HasObjectId
        {
            var model = Get<T>(path);
//
//            if (model is null)
//            {
//                throw new Exception($"{nameof(PersistentJournal)}::Update, Path: {path}. model is null");
//            }

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
            _metadataStorage.Flush();
            // Console.WriteLine($"Put Journal {Checkpoint}");
        }

        protected void WriteTo(IStorage storage, Dictionary<string, HasObjectId> cache, HashSet<string> deletes,
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

        public virtual void Delete(string path)
        {
//            Console.WriteLine($"PersistantJournal delete: {path}");
            _changed = true;
            _cache.Remove(path);
            _dataWriteCache.Remove(path);
            _deletes.Add(path);
        }
    }
}