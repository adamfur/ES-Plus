using System;

namespace ESPlus.Wyrm
{
    public class WyrmAggregateTenantRenamer : IWyrmAggregateRenamer
    {
        private readonly string _tenant;

        public WyrmAggregateTenantRenamer(string tenant)
        {
            _tenant = tenant ?? throw new Exception(nameof(tenant));
        }
        
        public string Name(string name)
        {
            return $"{_tenant}/{name}";
        }
    }
}