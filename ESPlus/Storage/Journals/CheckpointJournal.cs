using System.Collections.Generic;
using System.Threading;
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

        protected override async Task DoFlushAsync(CancellationToken cancellationToken)
        {
            await WriteToAsync(DataStorage, DataWriteCache, Deletes, "", cancellationToken);
            await WriteJournalAsync(new Dictionary<StringPair, string>(), Deletes, cancellationToken);
        }
    }
}