using System.Collections.Generic;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class CheckpointJournal : PersistantJournal
    {
        public CheckpointJournal(IStorage metadataStorage, IStorage dataStorage)
            : base(metadataStorage, dataStorage)
        {
        }

        protected override void DoFlush()
        {
            WriteTo(_dataStorage, _dataWriteCache);
            WriteJournal(new Dictionary<string, string>());
        }
    }
}