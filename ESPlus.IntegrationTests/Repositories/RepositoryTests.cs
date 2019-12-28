using System;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Exceptions;
using ESPlus.Interfaces;
using ESPlus.Wyrm;
using Xunit;

namespace ESPlus.IntegrationTests.Repositories
{
    public abstract class RepositoryTests
    {
        protected IRepository Repository;
        private readonly string _id;

        protected abstract IRepository Create();

        public RepositoryTests()
        {
            Repository = Create();
            _id = Guid.NewGuid().ToString();
        }

        [Fact]
        public async Task SaveAsync_InsertNewStream_Save()
        {

            var aggregate = new DummyAggregate(_id);

            await Repository.SaveAsync(aggregate);
        }

        [Fact]
        public async Task SaveAsync_AppendToExistingStream_Save()
        {
            var aggregate = new DummyAggregate(_id);

            await Repository.SaveAsync(aggregate);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
        }
        
        [Fact]
        public async Task SaveAsync_AppendToExistingStream_Save22222222222222222222222()
        {
            var aggregate = new DummyAggregate(_id, true);

            using (var transaction = Repository.BeginTransaction())
            {
                await transaction.CreateStreamAsync("hello-world");
//                await transaction.SaveAsync(aggregate, expectedVersion: ExpectedVersion.Any);
//                aggregate.Poke();
                await transaction.SaveAsync(aggregate);
                var result = await transaction.Commit();
            }
        }

        [Fact]
        public async Task SaveAsync_ReadExistingAndSave_Save()
        {
            var id = _id;
            var aggregate = new DummyAggregate(id);

            await Repository.SaveAsync(aggregate);

            aggregate = await this.Repository.GetByIdAsync<DummyAggregate>(id);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
        }        

        [Fact]
        public async Task SaveAsync_AppendToExistingStreamV2_Save()
        {
            var aggregate = new DummyAggregate(_id);

            await Repository.SaveAsync(aggregate);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
        }

        [Fact]
        public async Task SaveAsync_SameStream_Throw()
        {
            var id = _id;
            var aggregate1 = new DummyAggregate(id);
            var aggregate2 = new DummyAggregate(id);

            await Repository.SaveAsync(aggregate1);
            await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Repository.SaveAsync(aggregate2));
        }

        [Fact]
        public async Task DeleteAsync_DeleteExistingStream_Pass()
        {
            var id = _id;
            var aggregate = new DummyAggregate(id);

            await Repository.SaveAsync(aggregate);
            await Repository.DeleteStreamAsync(id, aggregate.Version);
        }

        // // //////////////////////////////[Fact]
        // // //////////////////////////////public async Task DeleteAsync_DeleteNonexistingStream_Pass()
        // // //////////////////////////////{
        // // //////////////////////////////    var id = Guid.NewGuid().ToString();
        // // //////////////////////////////
        // // //////////////////////////////    await Repository.DeleteAsync(id);
        // // //////////////////////////////}

        [Fact]
        public async Task GetAsync_ReadOneEventFromExistingStream_Pass()
        {
            var id = _id;
            var aggregate = new DummyAggregate(id);

            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<DummyAggregate>(id);

            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_ReadTwoEventsFromExistingStream_Pass()
        {
            var id = _id;
            var aggregate = new DummyAggregate(id);

            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<DummyAggregate>(id);

            Assert.Equal(aggregate.Version, copy.Version);
        }

        [Fact]
        public async Task GetAsync_WithNoReplayAttribute_Pass()
        {
            var id = _id;
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
            var id = _id;
            var aggregate = new DummyAggregate(id);

            aggregate.AddGuid(data);
            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<DummyAggregate>(id);

            Assert.Equal(data, copy.Guid);
        }        

        [Fact]
        public async Task DeleteAsync_DeleteExistingStream_Gone()
        {
            var id = _id;
            var aggregate = new DummyAggregate(id);

            aggregate.AttachFile();
            await Repository.SaveAsync(aggregate);
            await Repository.DeleteStreamAsync(id, aggregate.Version);
            await Assert.ThrowsAsync<AggregateNotFoundException>(async () => await Repository.GetByIdAsync<DummyAggregate>(id));
        }        
    }

    public class FileMetadataAddedEvent
    {
    }

    public class FileAddedEvent
    {
    }

    public class GuidAddedEvent
    {
        public Guid Guid { get; set; }
    }

    public class DummyAggregate : AggregateBase
    {
        public Guid Guid { get; set; }

        public DummyAggregate(string id)
            : base(id)
        {
        }
        
        public DummyAggregate(string id, bool poke)
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
}