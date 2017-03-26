using System.Collections.Generic;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public interface IJournaled : IStorage
    {
        long Checkpoint { get; set; }
        SubscriptionMode SubscriptionMode { get; }
    }

    public abstract class PersistantJournal : IJournaled
    {
        private readonly IStorage _journalMetadataStorage;
        private readonly IStorage _storage;
        public SubscriptionMode SubscriptionMode { get; private set; }
        private long _checkpoint = 0L;
        private Once _loadJournalOnce;
        protected readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        protected readonly Dictionary<string, object> _writeCache = new Dictionary<string, object>();
        protected JournalLog _journal;

        public PersistantJournal(IStorage journalMetadata, IStorage storage)
        {
            _storage = storage;
            _journalMetadataStorage = journalMetadata;
            _loadJournalOnce = new Once(() => this.LoadJournal());
        }

        public long Checkpoint
        {
            get
            {
                _loadJournalOnce.Execute();
                return _checkpoint;
            }
            set
            {
                _loadJournalOnce.Execute();
                if (value == 0L)
                {
                    SubscriptionMode = SubscriptionMode.Replay;
                }
                else
                {
                    SubscriptionMode = SubscriptionMode.RealTime;
                }
                _checkpoint = value;
            }
        }

        private void LoadJournal()
        {
            _journal = _journalMetadataStorage.Get<JournalLog>("Journal") ?? new JournalLog();
            PlayJournal();
            Checkpoint = _journal.Checkpoint;
        }

        protected abstract void PlayJournal();
        public abstract void Flush();

        public void Put(string path, object item)
        {
            _cache[path] = _writeCache[path] = item;
        }

        public T Get<T>(string path)
        {
            if (_cache.ContainsKey(path))
            {
                return (T)_cache[path];
            }

            return _storage.Get<T>(path);
        }

        protected void PutJournal(Dictionary<string, object> map)
        {
        }

        protected void Write()
        {

        }
    }

    public class CheckpointJournal : PersistantJournal
    {
        public CheckpointJournal(IStorage journalMetadata, IStorage storage)
            : base(journalMetadata, storage)
        {
        }

        public override void Flush()
        {
            WriteToStorage();
            PutJournal(new Dictionary<string, object>());
        }

        private void WriteToStorage()
        {
            // Write()
        }

        protected override void PlayJournal()
        {
        }
    }

    public class ReplayableJournal : PersistantJournal
    {
        private readonly IStorage _journalStorage;

        public ReplayableJournal(IStorage journalMetadata, IStorage joural, IStorage storage)
            : base(journalMetadata, storage)
        {
        }

        public override void Flush()
        {
            WriteToStage();
            PlayJournal();
            PutJournal(_cache);
        }

        private void WriteToStage()
        {
            // Write()
        }

        protected override void PlayJournal()
        {
            foreach (var item in _journal.Map)
            {
                var key = item.Key;
                var value = item.Value;
            }
            _journal = new JournalLog();
        }
    }
}