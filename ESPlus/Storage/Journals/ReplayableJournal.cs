using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class ReplayableJournal : PersistentJournal
    {
        private readonly IStorage _stageStorage;
        private readonly Dictionary<StringPair, object> _dataStageCache = new Dictionary<StringPair, object>();
        private readonly Dictionary<StringPair, string> _map = new Dictionary<StringPair, string>();

        public ReplayableJournal(IStorage metadataStorage, IStorage stageStorage, IStorage dataStorage)
            : base(metadataStorage, dataStorage)
        {
            _stageStorage = stageStorage;
        }

        protected override async Task DoFlushAsync()
        {
            await WriteToAsync(_stageStorage, _dataStageCache, _deletes, "stage/");
            await WriteJournalAsync(_map, _deletes);
            await WriteToAsync(_dataStorage, _dataWriteCache, _deletes);
        }

        public override void Put<T>(string path, string tenant, T item)
        {
            var key = new StringPair(path, tenant);
            
            _dataStageCache[key] = item;
            _map[key] = path;
            base.Put(path, tenant, item);
        }

        public override void Delete(string path, string tenant)
        {
            var key = new StringPair(path, tenant);
            
            _dataStageCache.Remove(key);
            _map.Remove(key);
            base.Delete(path, tenant);
        }

        protected override void DoClean()
        {
            _map.Clear();
            _dataStageCache.Clear();
        }        

        protected override async Task PlayJournal(JournalLog journal)
        {
            if (journal.Map.Count != 0)
            {
                foreach (var item in journal.Map)
                {
                    var source = item.Key;
                    var destination = item.Value;
                    var payload = await _stageStorage.GetAsync<JournalLog>(source.Path, source.Tenant);

                    _dataStorage.Put(destination, source.Tenant, payload);
                }
            }
            
            if (journal.Deletes.Count != 0)
            {
                foreach (var item in journal.Deletes)
                {
                    _dataStorage.Delete(item.Path, item.Tenant);
                }
            }

            await _dataStorage.FlushAsync();
        }
    }
}