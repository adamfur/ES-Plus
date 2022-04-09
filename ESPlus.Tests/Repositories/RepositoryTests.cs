using System;
using System.Threading.Tasks;
using ESPlus.Exceptions;
using ESPlus.Interfaces;
using Xunit;

namespace ESPlus.Tests.Repositories
{
    public abstract class RepositoryTests
    {
        protected readonly IWyrmRepository Repository;

        protected abstract IWyrmRepository Create();

        protected RepositoryTests()
        {
            Repository = Create();
        }

        [Fact]
        public async Task SaveAsync_InsertNewStream_Save()
        {
            var aggregate = new Aggregates.DummyAggregate(Guid.NewGuid().ToString(), 0);

            await Repository.SaveAsync(aggregate, null);
            Assert.Equal(1, aggregate.Count);
            
        }

        [Fact]
        public async Task SaveAsync_AppendToExistingStream_Save()
        {
            var aggregate = new Aggregates.DummyAggregate(Guid.NewGuid().ToString(), 0);

            await Repository.SaveAsync(aggregate, null);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate, null);
            Assert.Equal(2, aggregate.Count);
        }

        [Fact]
        public async Task SaveAsync_ReadExistingAndSave_Save()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id, 0);

            await Repository.SaveAsync(aggregate, null);

            aggregate = await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id, default);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate, null);
            Assert.Equal(2, aggregate.Count);
        }        

        [Fact]
        public async Task SaveAsync_AppendToExistingStreamV2_Save()
        {
            var aggregate = new Aggregates.DummyAggregate(Guid.NewGuid().ToString(), 0);

            await Repository.SaveAsync(aggregate, null);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate, null);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate, null);
            Assert.Equal(3, aggregate.Count);
        }

        [Fact]
        public async Task SaveAsync_SameStream_Throw()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate1 = new Aggregates.DummyAggregate(id, 0);
            var aggregate2 = new Aggregates.DummyAggregate(id, 0);

            await Repository.SaveAsync(aggregate1, null);
            Assert.Equal(1, aggregate1.Count);
            Assert.Equal(1, aggregate2.Count);
            await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Repository.SaveAsync(aggregate2, null));
        }

        [Fact]
        public async Task DeleteAsync_DeleteExistingStream_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id, 0);

            await Repository.SaveAsync(aggregate, null);
            Assert.Equal(1, aggregate.Count);
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
            var aggregate = new Aggregates.DummyAggregate(id, 0);

            Assert.Equal(1, aggregate.Count);
            await Repository.SaveAsync(aggregate, null);
            var copy = await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id);

            Assert.Equal(1, copy.Count);
            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_ReadTwoEventsFromExistingStream_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id, 0);

            aggregate.Poke();
            await Repository.SaveAsync(aggregate, null);
            Assert.Equal(2, aggregate.Count);
            var copy = await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id);

            Assert.Equal(2, copy.Count);
            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_WithNoReplayAttribute_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id, 0);

            aggregate.AttachFile();
            await Repository.SaveAsync(aggregate, null);
            // Assert.Equal(3, aggregate.Count);
            var copy = await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id, default);

            // Assert.Equal(2, copy.Count);
            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_ContainsData_Same()
        {
            var data = Guid.NewGuid();
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id, 0);

            aggregate.AddGuid(data);
            await Repository.SaveAsync(aggregate, null);
            Assert.Equal(2, aggregate.Count);
            var copy = await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id, default);

            Assert.Equal(2, copy.Count);
            Assert.Equal(data, copy.Guid);
        }        

        [Fact]
        public async Task DeleteAsync_DeleteExistingStream_Gone()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new Aggregates.DummyAggregate(id, 0);

            aggregate.AttachFile();
            Assert.Equal(3, aggregate.Count);
            await Repository.SaveAsync(aggregate, null);
            await Repository.DeleteAsync(id, aggregate.Version);
            await Assert.ThrowsAsync<AggregateNotFoundException>(async () => await Repository.GetByIdAsync<Aggregates.DummyAggregate>(id, default));
        }        
    }
}