using System;
using System.Threading.Tasks;
using ESPlus.Exceptions;
using ESPlus.Interfaces;
using Xunit;

namespace ESPlus.IntegrationTests.Repositories
{
    public abstract class RepositoryTests
    {
        protected IRepository Repository;

        protected abstract IRepository Create();

        public RepositoryTests()
        {
            Repository = Create();
        }

        [Fact]
        public async Task SaveAsync_InsertNewStream_Save()
        {
            var aggregate = new Aggregates.DummyAggregate(Guid.NewGuid().ToString());

            await Repository.SaveAsync(aggregate);
        }

        [Fact]
        public async Task SaveAsync_AppendToExistingStream_Save()
        {
            var aggregate = new Aggregates.DummyAggregate(Guid.NewGuid().ToString());

            await Repository.SaveAsync(aggregate);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
        }

        [Fact]
        public async Task SaveAsync_ReadExistingAndSave_Save()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id);

            await Repository.SaveAsync(aggregate);

            aggregate = await this.Repository.GetByIdAsync<Aggregates.DummyAggregate>(id);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
        }        

        [Fact]
        public async Task SaveAsync_AppendToExistingStreamV2_Save()
        {
            var aggregate = new Aggregates.DummyAggregate(Guid.NewGuid().ToString());

            await Repository.SaveAsync(aggregate);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
        }

        [Fact]
        public async Task SaveAsync_SameStream_Throw()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate1 = new Aggregates.DummyAggregate(id);
            var aggregate2 = new Aggregates.DummyAggregate(id);

            await Repository.SaveAsync(aggregate1);
            await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Repository.SaveAsync(aggregate2));
        }

        [Fact]
        public async Task DeleteAsync_DeleteExistingStream_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id);

            await Repository.SaveAsync(aggregate);
            await Repository.DeleteAsync(id, aggregate.Version);
        }

        [Fact]
        public async Task DeleteAsync_DeleteNonexistingStream_Pass()
        {
            var id = Guid.NewGuid().ToString();
        
            await Repository.DeleteAsync(id);
        }

        [Fact]
        public async Task GetAsync_ReadOneEventFromExistingStream_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id);

            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id);

            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_ReadTwoEventsFromExistingStream_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id);

            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id);

            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_WithNoReplayAttribute_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id);

            aggregate.AttachFile();
            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id);

            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_ContainsData_Same()
        {
            var data = Guid.NewGuid();
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id);

            aggregate.AddGuid(data);
            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id);

            Assert.Equal(data, copy.Guid);
        }        

        [Fact]
        public async Task DeleteAsync_DeleteExistingStream_Gone()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id);

            aggregate.AttachFile();
            await Repository.SaveAsync(aggregate);
            await Repository.DeleteAsync(id, aggregate.Version);
            await Assert.ThrowsAsync<AggregateNotFoundException>(async () => await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id));
        }        
    }
}