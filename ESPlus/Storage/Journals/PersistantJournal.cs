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
        protected readonly Dictionary<string, string> _map = new Dictionary<string, string>();
        public long Checkpoint { get; set; } = 0L;

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
        }

        protected abstract void PlayJournal(JournalLog journal);
        public abstract void Flush();

        public void Put(string destination, object item)
        {
            var source = $"Journal/{Checkpoint}/{destination}";

            _cache[destination] = _writeCache[destination] = item;
            _map[destination] = destination;
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

            return _dataStorage.Get(path);
        }

        protected void PutJournal(Dictionary<string, string> map)
        {
            var journal = new JournalLog
            {
                Checkpoint = Checkpoint,
                Map = new Dictionary<string, string>(map)
            };
            _metadataStorage.Put(JournalPath, journal);
            _metadataStorage.Flush();
        }

        protected void WriteTo(IStorage storage)
        {
            foreach (var item in _writeCache)
            {
                var destination = item.Key;
                var payload = item.Value;

                storage.Put(destination, payload);
            }
            storage.Flush();
        }        

        protected void Clean()
        {
            _writeCache.Clear();
            _map.Clear();
        }
    }
}