using System;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using Xunit;

namespace ESPlus.IntegrationTests.Repositories
{
    public abstract class RepositoryTests
    {
        public class DummyAggregate : AggregateBase
        {
            public Guid Guid { get; set; }

            public DummyAggregate(string id)
                : base(id)
            {
                Poke();
            }

            public void Poke()
            {
                ApplyChange(new DummyEvent());
            }

            public void AttachFile()
            {
                ApplyChange(new FileMetadataAddedEvent());
                ApplyChange(new FileAddedEvent());
            }

            public void AddGuid(Guid guid)
            {
                ApplyChange(new GuidAddedEvent
                {
                    Guid = guid
                });
            }

            protected void Apply(GuidAddedEvent @event)
            {
                Guid = @event.Guid;
            }            

            protected void Apply(DummyEvent @event)
            {
            }

            [NoReplay]
            protected void Apply(FileAddedEvent @event)
            {
            }

            protected void Apply(FileMetadataAddedEvent @event)
            {
            }
        }

        public class DummyEvent
        {
        }

        public class GuidAddedEvent
        {
            public Guid Guid { get; set; }
        }

        public class FileAddedEvent
        {
        }

        public class FileMetadataAddedEvent
        {
        }

        protected IRepository Repository;

        protected abstract IRepository Create();

        public RepositoryTests()
        {
            Repository = Create();
        }

        [Fact]
        public async Task SaveAsync_InsertNewStream_Save()
        {
            var aggregate = new DummyAggregate(Guid.NewGuid().ToString());

            await Repository.SaveAsync(aggregate);
        }

        [Fact]
        public async Task SaveAsync_AppendToExistingStream_Save()
        {
            var aggregate = new DummyAggregate(Guid.NewGuid().ToString());

            await Repository.SaveAsync(aggregate);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
        }

        [Fact]
        public async Task SaveAsync_AppendToExistingStreamV2_Save()
        {
            var aggregate = new DummyAggregate(Guid.NewGuid().ToString());

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
            var aggregate1 = new DummyAggregate(id);
            var aggregate2 = new DummyAggregate(id);

            await Repository.SaveAsync(aggregate1);
            await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Repository.SaveAsync(aggregate2));
        }

        [Fact]
        public async Task DeleteAsync_DeleteExistingStream_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new DummyAggregate(id);

            await Repository.SaveAsync(aggregate);
            await Repository.DeleteAsync(id, aggregate.Version);
        }

        // [Fact]
        // public async Task DeleteAsync_DeleteNonexistingStream_Pass()
        // {
        //     var id = Guid.NewGuid().ToString();

        //     await Repository.DeleteAsync(id);
        // }

        [Fact]
        public async Task GetAsync_ReadOneEventFromExistingStream_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new DummyAggregate(id);

            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<DummyAggregate>(id);

            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_ReadTwoEventsFromExistingStream_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new DummyAggregate(id);

            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<DummyAggregate>(id);

            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_WithNoReplayAttribute_Pass()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new DummyAggregate(id);

            aggregate.AttachFile();
            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<DummyAggregate>(id);

            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_ContainsData_Same()
        {
            var data = Guid.NewGuid();
            var id = Guid.NewGuid().ToString();
            var aggregate = new DummyAggregate(id);

            aggregate.AddGuid(data);
            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<DummyAggregate>(id);

            Assert.Equal(data, copy.Guid);
        }        

        [Fact]
        public async Task DeleteAsync_DeleteExistingStream_Gone()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = new DummyAggregate(id);

            aggregate.AttachFile();
            await Repository.SaveAsync(aggregate);
            await Repository.DeleteAsync(id, aggregate.Version);
            await Assert.ThrowsAsync<AggregateNotFoundException>(async () => await Repository.GetByIdAsync<DummyAggregate>(id));
        }        
    }
}