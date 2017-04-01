using System.Collections.Generic;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class ReplayableJournal : PersistantJournal
    {
        private readonly IStorage _stageStorage;
        protected readonly Dictionary<string, object> _dataStageCache = new Dictionary<string, object>();
        protected readonly Dictionary<string, string> _map = new Dictionary<string, string>();

        public ReplayableJournal(IStorage metadataStorage, IStorage stageStorage, IStorage dataStorage)
            : base(metadataStorage, dataStorage)
        {
            _stageStorage = stageStorage;
        }

        protected override void DoFlush()
        {
            WriteTo(_stageStorage, _dataStageCache);
            WriteJournal(_map);
            WriteTo(_dataStorage, _dataWriteCache);
        }

        public override void Put(string destinationPath, object item)
        {
            var stagePath = $"Journal/{Checkpoint}/{destinationPath}";

            _dataStageCache[stagePath] = item;
            _map[stagePath] = destinationPath;
            base.Put(destinationPath, item);
        }        

        protected override void DoClean()
        {
            _map.Clear();
            _dataStageCache.Clear();
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