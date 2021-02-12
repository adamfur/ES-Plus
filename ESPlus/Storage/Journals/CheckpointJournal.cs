using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class CheckpointJournal : PersistentJournal
    {
        public CheckpointJournal(IStorage metadataStorage, IStorage dataStorage)
            : base(metadataStorage, dataStorage)
        {
        }

        protected override async Task DoFlushAsync()
        {
            await WriteToAsync(_dataStorage, _dataWriteCache, _deletes);
            await WriteJournalAsync(new Dictionary<StringPair, string>(), _deletes);
        }
    }
}