using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;

namespace ESPlus.Wyrm
{
    public class WyrmTransaction : WyrmStore, IRepositoryTransaction
    {
        private readonly List<BundleItem> _bundles = new List<BundleItem>(); 
        private readonly IWyrmDriver _wyrmDriver;

        public WyrmTransaction(IWyrmDriver wyrmDriver, IEventSerializer eventSerializer)
            : base(wyrmDriver, eventSerializer)
        {
            _wyrmDriver = wyrmDriver;
        }

        public void Dispose()
        {
            Clear();
        }

        public async Task<WyrmResult> CommitAsync(CommitPolicy policy = CommitPolicy.All)
        {
            var result = await _wyrmDriver.AppendAsync(new Bundle
            {
                Policy = policy,
                Items = _bundles,
            });
            
            Clear();
            return result;
        }

        private void Clear()
        {
            _bundles.Clear();
        }

        protected override Task<WyrmResult> Apply(BundleItem item)
        {
            _bundles.Add(item);
            return Task.FromResult(WyrmResult.Empty());
        }
    }
}