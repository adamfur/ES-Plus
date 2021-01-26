using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class ReplayableJournal : PersistentJournal
    {
        private readonly IStorage _stageStorage;
        private readonly Dictionary<string, object> _dataStageCache = new Dictionary<string, object>();
        private readonly Dictionary<string, string> _map = new Dictionary<string, string>();

        public ReplayableJournal(IStorage metadataStorage, IStorage stageStorage, IStorage dataStorage)
            : base(metadataStorage, dataStorage)
        {
            _stageStorage = stageStorage;
        }

        protected override void DoFlush()
        {
            WriteTo(_stageStorage, _dataStageCache, _deletes, "stage/");
            WriteJournal(_map, _deletes);
            WriteTo(_dataStorage, _dataWriteCache, _deletes);
        }

        public override void Put<T>(string path, T item)
        {
            var stagePath = path;

            _dataStageCache[stagePath] = item;
            _map[stagePath] = path;
            base.Put(path, item);
        }

        public override void Delete(string path)
        {
            _dataStageCache.Remove(path);
            _map.Remove(path);
            base.Delete(path);
        }

        protected override void DoClean()
        {
            _map.Clear();
            _dataStageCache.Clear();
        }        

        protected override void PlayJournal(JournalLog journal)
        {
            if (journal.Map.Count != 0)
            {
                foreach (var item in journal.Map)
                {
                    var source = item.Key;
                    var destination = item.Value;
                    var payload = _stageStorage.Get<JournalLog>(source);

                    _dataStorage.Put(destination, payload);
                }
            }
            
            if (journal.Deletes.Count != 0)
            {
                foreach (var item in journal.Deletes)
                {
                    _dataStorage.Delete(item);
                }
            }

            _dataStorage.Flush();
        }
    }
}