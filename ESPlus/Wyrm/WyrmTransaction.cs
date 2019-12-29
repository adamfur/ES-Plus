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

        public WyrmTransaction(IWyrmDriver wyrmDriver)
            : base(wyrmDriver)
        {
            _wyrmDriver = wyrmDriver;
        }

        public void Dispose()
        {
            _bundles.Clear();
        }

        public Task<WyrmResult> Commit(CommitPolicy policy = CommitPolicy.All)
        {
            return _wyrmDriver.Append(new Bundle
            {
                Encrypt = true, // bad place
                Policy = policy,
                Items = _bundles,
            });
        }

        protected override async Task<WyrmResult> Apply(BundleItem item)
        {
            _bundles.Add(item);
            return WyrmResult.Empty();
        }
    }
}