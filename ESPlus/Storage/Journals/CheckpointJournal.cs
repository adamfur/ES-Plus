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

        public override void Flush()
        {
            if (!_changed)
            {
                return;
            }
            
            WriteTo(_dataStorage, _writeCache);
            WriteJournal(new Dictionary<string, string>());
            Clean();
        }

        protected override void PlayJournal(JournalLog journal)
        {
        }
    }
}