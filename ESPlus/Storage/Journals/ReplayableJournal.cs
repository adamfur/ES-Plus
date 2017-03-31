using System.Collections.Generic;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class ReplayableJournal : PersistantJournal
    {
        private readonly IStorage _stageStorage;
        protected readonly Dictionary<string, object> _stageCache = new Dictionary<string, object>();
        protected readonly Dictionary<string, string> _map = new Dictionary<string, string>();

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

        public override void Put(string destination, object item)
        {
            var source = $"Journal/{Checkpoint}/{destination}";

            _stageCache[source] = item;
            _map[source] = destination;
            base.Put(destination, item);
        }        

        protected override void Clean()
        {
            if (_changed == false)
            {
                return;
            }

            base.Clean();
            _map.Clear();
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