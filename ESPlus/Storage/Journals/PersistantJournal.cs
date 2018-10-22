using System;
using System.Collections.Generic;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;
using Newtonsoft.Json;

namespace ESPlus.Storage
{
    public abstract class PersistantJournal : IJournaled
    {
        public const string JournalPath = "000Journal/000Journal.json";
        protected readonly IStorage _metadataStorage;
        protected readonly IStorage _dataStorage;
        public SubscriptionMode SubscriptionMode { get; private set; } = SubscriptionMode.RealTime;
        protected readonly Dictionary<string, WeakReference> _cache = new Dictionary<string, WeakReference>();
        protected readonly Dictionary<string, HasObjectId> _dataWriteCache = new Dictionary<string, HasObjectId>();
        public byte[] Checkpoint { get; set; }
        private bool _changed = false;

        public PersistantJournal(IStorage metadataStorage, IStorage dataStorage)
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
                Console.WriteLine($"Journal Read Success: {journal.Checkpoint}");
            }
            catch (Exception e)
            {
                journal = new JournalLog();
                Console.WriteLine($"Journal Read Failed: {journal.Checkpoint}");
            }
            Checkpoint = journal.Checkpoint;

            if (journal.Checkpoint == Position.Start)
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
            _cache[destination] = new WeakReference(item, false);
            _dataWriteCache[destination] = item;
            _changed = true;
        }

        public T Get<T>(string path)
            where T : HasObjectId
        {
            try
            {
                if (_cache.ContainsKey(path))
                {
                    return _cache[path].Target as T;
                }

                if (SubscriptionMode == SubscriptionMode.Replay)
                {
                    return default(T);
                }

                var data = _dataStorage.Get<T>(path);

                _cache[path] = new WeakReference(data, false);
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
            Console.WriteLine($"Put Journal {Checkpoint}");
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
    }
}