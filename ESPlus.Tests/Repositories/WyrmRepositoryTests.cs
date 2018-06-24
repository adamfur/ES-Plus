using System;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using Xunit;

namespace ESPlus.Tests.Repositories
{
    public class DummyAggregate : AggregateBase
    {
        public DummyAggregate(string id)
            : base(id)
        {
            Poke();
        }

        public void Poke()
        {
            ApplyChange(new object());
        }

        protected void Apply(object @event)
        {
        }
    }

    public class WyrmRepositoryTests
    {
        private IRepository _repository;
        private IEventSerializer _eventSerializer;

        public WyrmRepositoryTests()
        {
            var connection = new WyrmConnection();
            _eventSerializer = new EventJsonSerializer();
            _repository = new WyrmRepository(connection, _eventSerializer);
        }

        [Fact]
        public async Task SaveAsync_InsertNewStream_Save()
        {
            var aggregate = new DummyAggregate(Guid.NewGuid().ToString());

            await _repository.SaveAsync(aggregate);
        }

        [Fact]
        public async Task SaveAsync_AppendToExistingStream_Save()
        {
            var aggregate = new DummyAggregate(Guid.NewGuid().ToString());

            await _repository.SaveAsync(aggregate);
            aggregate.Poke();
            await _repository.SaveAsync(aggregate);
        }     

        [Fact]
        public async Task SaveAsync_SameStream_Throw()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate1 = new DummyAggregate(id);
            var aggregate2 = new DummyAggregate(id);

            await _repository.SaveAsync(aggregate1);
            await Assert.ThrowsAsync<Exception>(() => _repository.SaveAsync(aggregate2));
        }          
    }
}