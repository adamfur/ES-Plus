using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class ReplayableJournal : PersistantJournal
    {
        private readonly IStorage _stageStorage;

        public ReplayableJournal(IStorage metadataStorage, IStorage stageStorage, IStorage dataStorage)
            : base(metadataStorage, dataStorage)
        {
            _stageStorage = stageStorage;
        }

        public override void Flush()
        {
            if (!_changed)
            {
                return;
            }
            
            WriteTo(_stageStorage, _stageCache);
            WriteJournal(_map);
            WriteTo(_dataStorage, _writeCache);
            Clean();
        }

        protected override void PlayJournal(JournalLog journal)
        {
            if (journal.Map.Count == 0)
            {
                return;
            }

            foreach (var item in journal.Map)
            {
                var source = item.Key;
                var destination = item.Value;
                var payload = _stageStorage.Get(source);

                _dataStorage.Put(destination, payload);
            }
            _dataStorage.Flush();
        }
    }
}