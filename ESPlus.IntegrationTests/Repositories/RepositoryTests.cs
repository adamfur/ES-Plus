using System;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Exceptions;
using ESPlus.Interfaces;
using ESPlus.Misc;
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

            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
        }
        
        [Fact]
        public async Task GetAsync_xxxxxxxxxxxxxxx()
        {
            await Assert.ThrowsAsync<AggregateNotFoundException>(() => Repository.GetByIdAsync<DummyAggregate>(_id));
        }

        [Fact]
        public void SubscribeAll()
        {
            var connection = new WyrmDriver("192.168.1.2:9999", new EventJsonSerializer(new EventTypeResolver()), "key");

            foreach (var item in connection.SubscribeAll(Position.Start))
            {
                Console.WriteLine(item.GetHashCode());
            }
        }

        [Fact]
        public async Task SaveAsync_AppendToExistingStream_Save()
        {
            var aggregate = new DummyAggregate(_id);

            aggregate.Poke();
            await Repository.SaveAsync(aggregate);
            aggregate.Poke();
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
                await transaction.SaveAsync(aggregate); //, expectedVersion: ExpectedVersion.Any
                aggregate.Poke();
                await transaction.SaveAsync(aggregate);
                var result = await transaction.Commit();
            }
        }

        [Fact]
        public async Task SaveAsync_ReadExistingAndSave_Save()
        {
            var id = _id;
            var aggregate = new DummyAggregate(id, true);

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
            var aggregate1 = new DummyAggregate(_id, true);
            var aggregate2 = new DummyAggregate(_id, true);

            await Repository.SaveAsync(aggregate1);
            //await Assert.ThrowsAsync<WrongExpectedVersionException>(() => Repository.SaveAsync(aggregate2));
            await Assert.ThrowsAsync<WyrmException>(() => Repository.SaveAsync(aggregate2));
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task SaveAsync_SameStream_Throwx(int times)
        {
            var aggregate = new DummyAggregate(_id, true);

            for (var i = 0; i < times; ++i)
            {
                aggregate.Poke();
            }
           
            await Repository.SaveAsync(aggregate);
        }

        [Fact]
        public async Task DeleteAsync_DeleteExistingStream_Pass()
        {
            var id = _id;
            var aggregate = new DummyAggregate(id);

            await Repository.SaveAsync(aggregate);
            await Repository.DeleteStreamAsync(id, aggregate.Version);
        }

        //[Fact]
        //public async Task DeleteAsync_DeleteNonexistingStream_Pass()
        //{
        //    var id = Guid.NewGuid().ToString();
        //
        //    await Repository.DeleteAsync(id);
        //}

        [Fact]
        public async Task GetAsync_ReadOneEventFromExistingStream_Pass()
        {
            var id = _id;
            var aggregate = new DummyAggregate(id, true);

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
            var aggregate = new DummyAggregate(_id, true);

            aggregate.AttachFile();
            await Repository.SaveAsync(aggregate);
            var copy = await Repository.GetByIdAsync<DummyAggregate>(_id);

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
            var aggregate = new DummyAggregate(_id, true);

            aggregate.AttachFile();
            await Repository.SaveAsync(aggregate);
            await Repository.DeleteStreamAsync(_id, aggregate.Version);
            await Assert.ThrowsAsync<AggregateNotFoundException>(() => Repository.GetByIdAsync<DummyAggregate>(_id));
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