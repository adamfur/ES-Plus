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
            await WriteToAsync(_stageStorage, _dataStageCache, Deletes, "stage/");
            await WriteJournalAsync(_map, Deletes);
            await WriteToAsync(DataStorage, DataWriteCache, Deletes);
        }

        public override void Put<T>(string tenant, string path, T item)
        {
            var key = new StringPair(tenant, path);
            
            _dataStageCache[key] = item;
            _map[key] = path;
            base.Put(tenant, path, item);
        }

        public override void Delete(string tenant, string path)
        {
            var key = new StringPair(tenant, path);
            
            _dataStageCache.Remove(key);
            _map.Remove(key);
            base.Delete(tenant, path);
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
                    var payload = await _stageStorage.GetAsync<JournalLog>(source.Tenant, source.Path);

                    DataStorage.Put(source.Tenant, destination, payload);
                }
            }
            
            if (journal.Deletes.Count != 0)
            {
                foreach (var item in journal.Deletes)
                {
                    DataStorage.Delete(item.Tenant, item.Path);
                }
            }

            await DataStorage.FlushAsync();
        }
    }
}