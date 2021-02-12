using System;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Wyrm;
using Xunit;

namespace ESPlus.IntegrationTests
{
    public class DummyAggregate : AggregateBase
    {
        public DummyAggregate(string id)
            : base(id, typeof(DummyEvent))
        {
            ApplyChange(new DummyEvent());
        }

        protected void Apply(DummyEvent @event)
        {
        }
    }

    public class DummyEvent
    {
    }
    
    public class MultiTenantTests
    {
        private readonly WyrmRepository _repository;

        public MultiTenantTests()
        {
            _repository = new WyrmRepository(new WyrmDriver("192.168.1.2:8888", new EventJsonSerializer()), new WyrmAggregateZeroRenamer());
        }
        
        [Fact]
        public async Task Foo()
        {
            await _repository.SaveAsync(new DummyAggregate(Guid.NewGuid().ToString()), new {Tenant = "a-tenant"});
            var any = false;
            
            await foreach (var (aggregate, tentant) in _repository.GetAllByAggregateType<DummyAggregate>(typeof(DummyEvent)))
            {
                any = true;
                Assert.Equal("a-tenant", tentant);
            }
            
            Assert.True(any);
        }
    }
}