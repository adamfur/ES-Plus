using System.Collections.Generic;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;
using EventStore.ClientAPI;

namespace ESPlus.Storage
{
    public abstract class PersistantJournal : IJournaled
    {
        public const string JournalPath = "000Journal/000Journal.json";
        protected readonly IStorage _metadataStorage;
        protected readonly IStorage _dataStorage;
        public SubscriptionMode SubscriptionMode { get; private set; } = SubscriptionMode.RealTime;
        protected readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        protected readonly Dictionary<string, object> _dataWriteCache = new Dictionary<string, object>();
        public Position Checkpoint { get; set; }
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
            var journal = (JournalLog)_metadataStorage.Get(JournalPath) ?? new JournalLog();

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

        public virtual void Put(string destination, object item)
        {
            _cache[destination] = item;
            _dataWriteCache[destination] = item;
            _changed = true;
        }

        public object Get(string path)
        {
            if (_cache.ContainsKey(path))
            {
                return _cache[path];
            }

            if (SubscriptionMode == SubscriptionMode.Replay)
            {
                return null;
            }

            var data = _dataStorage.Get(path);

            _cache[path] = data;
            return data;
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
        }

        protected void WriteTo(IStorage storage, Dictionary<string, object> cache)
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
    }
}