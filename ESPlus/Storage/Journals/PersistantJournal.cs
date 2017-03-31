using System.Collections.Generic;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public abstract class PersistantJournal : IJournaled
    {
        public const string JournalPath = "Journal/Journal.json";
        protected readonly IStorage _metadataStorage;
        protected readonly IStorage _dataStorage;
        public SubscriptionMode SubscriptionMode { get; private set; } = SubscriptionMode.RealTime;
        protected readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        protected readonly Dictionary<string, object> _writeCache = new Dictionary<string, object>();
        public long Checkpoint { get; set; } = 0L;
        protected bool _changed = false;

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
            var journal = (JournalLog) _metadataStorage.Get(JournalPath) ?? new JournalLog();

            Checkpoint = journal.Checkpoint;
            if (journal.Checkpoint == 0L)
            {
                SubscriptionMode = SubscriptionMode.Replay;
            }
            PlayJournal(journal);
            _changed = false;
        }

        protected abstract void PlayJournal(JournalLog journal);
        public abstract void Flush();

        public virtual void Put(string destination, object item)
        {

            _cache[destination] = item;
            _writeCache[destination] = item;

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
            foreach (var item in _writeCache)
            {
                var destination = item.Key;
                var payload = item.Value;

                storage.Put(destination, payload);
            }
            storage.Flush();
        }        

        protected virtual void Clean()
        {
            if (_changed == false)
            {
                return;
            }
            
            _writeCache.Clear();
            _changed = false;
        }
    }
}