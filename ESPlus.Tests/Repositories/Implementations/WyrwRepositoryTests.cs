using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Wyrm;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace ESPlus.Tests.Repositories.Implementations
{
    public class WyrwRepositoryTests
    {
        private WyrmRepository _repository;
        private IWyrmDriver _driver;
        private TestEventSerializer _testEventSerializer;

        public WyrwRepositoryTests()
        {
            _driver = Substitute.For<IWyrmDriver>();

            _testEventSerializer = new TestEventSerializer();
            _driver.Serializer.Returns(_testEventSerializer);
            _repository = new WyrmRepository(_driver, new WyrmAggregateZeroRenamer());
        }

        [Fact]
        public async Task Save_Generic()
        {
            var aggregate = new TestAggregate(new TestId("Test"));
            aggregate.Test();
            await _repository.SaveAsync<TestAggregate, TestId>(aggregate, null);

            await _driver.Received().Append(Arg.Any<List<WyrmAppendEvent>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Save()
        {
            var aggregate = new StringTestAggregate("Test");
            aggregate.Test();
            await _repository.SaveAsync(aggregate, null);

            await _driver.Received().Append(Arg.Any<List<WyrmAppendEvent>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetById_Generic()
        {
            async IAsyncEnumerable<WyrmEvent2> GetEvents(CallInfo arg)
            {
                var testEvent = new TestEvent(10);
                yield return await Task.FromResult(new WyrmEvent2
                {
                    Data = _testEventSerializer.Serialize(testEvent),
                    EventType = testEvent.GetType().FullName
                });
            }

            _driver.EnumerateStream(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(GetEvents);

            var aggregate = await _repository.GetByIdAsync<TestAggregate, TestId>(new TestId("Test"), default);

            Assert.Equal(10, aggregate.Value);
        }

        [Fact]
        public async Task GetById()
        {
            async IAsyncEnumerable<WyrmEvent2> GetEvents(CallInfo arg)
            {
                var testEvent = new TestEvent(10);
                yield return await Task.FromResult(new WyrmEvent2
                {
                    Data = _testEventSerializer.Serialize(testEvent),
                    EventType = testEvent.GetType().FullName
                });
            }

            _driver.EnumerateStream(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(GetEvents);

            var aggregate = await _repository.GetByIdAsync<StringTestAggregate>("e");

            Assert.Equal(10, aggregate.Value);
        }

        [Fact]
        public void TestId()
        {
            var id = new TestId("e");

            var s = id.ToString();
        }
    }


    public class TestEventSerializer : IEventSerializer
    {
        public byte[] Serialize<T>(T graph)
        {
            var data = JsonConvert.SerializeObject(graph);

            return Encoding.UTF8.GetBytes(data);
        }

        public object Deserialize(Type type, byte[] buffer)
        {
            var json = Encoding.UTF8.GetString(buffer);
            return JsonConvert.DeserializeObject(json, type);
        }
    }

    public class TestAggregate : AggregateBase<TestId>
    {
        public int Value { get; set; }
        public TestAggregate(TestId id) : base(id, typeof(TestEvent))
        {
        }

        public void Test()
        {
            ApplyChange(new TestEvent(10));
        }

        protected void Apply(TestEvent @event)
        {
            Value = @event.Value;
        }
    }

    public class StringTestAggregate : AggregateBase
    {
        public int Value { get; set; }
        public StringTestAggregate(string id) : base(id, typeof(TestEvent))
        {
        }

        public void Test()
        {
            ApplyChange(new TestEvent(10));
        }

        protected void Apply(TestEvent @event)
        {
            Value = @event.Value;
        }
    }

    public record TestEvent(int Value);

    public record TestId(string Identifier)
    {
        public override string ToString()
        {
            return Identifier;
        }
    }
}