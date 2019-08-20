using System;
using System.Collections.Generic;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;
using System.Runtime.CompilerServices;
using ESPlus.Extentions;

namespace ESPlus.Storage
{
    public abstract class PersistantJournal : IJournaled
    {
        public const string JournalPath = "000Journal/000Journal.json";
        protected readonly IStorage _metadataStorage;
        protected readonly IStorage _dataStorage;
        public SubscriptionMode SubscriptionMode { get; private set; } = SubscriptionMode.RealTime;
        protected readonly ConditionalWeakTable<string, HasObjectId> _cache = new ConditionalWeakTable<string, HasObjectId>();
        protected readonly Dictionary<string, HasObjectId> _dataWriteCache = new Dictionary<string, HasObjectId>();

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

        protected PersistantJournal(IStorage metadataStorage, IStorage dataStorage)
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
                    return default;
                }

                var data = _dataStorage.Get<T>(path);

                _cache.AddOrUpdate(path, data);
                return data;
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                return default(T);
            }
        }

        protected void WriteJournal(Dictionary<string, string> map)
        {
            var journal = new JournalLog
            {
                Checkpoint = Checkpoint,
                Map = new Dictionary<string, string>(map)
            };
            _metadataStorage.Put(JournalPath, journal);
            _metadataStorage.Flush();
            // Console.WriteLine($"Put Journal {Checkpoint}");
        }

        protected void WriteTo(IStorage storage, Dictionary<string, HasObjectId> cache)
        {
            foreach (var item in cache)
            {
                var destination = item.Key;
                var payload = item.Value;

                storage.Put(destination, payload);
            }
            storage.Flush();
        }

        protected virtual void Clean()
        {
            _dataWriteCache.Clear();
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

        public void Delete(string path)
        {
            Put(path, new HasObjectId());
        }
    }
}