using System;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using Xunit;

namespace ESPlus.Tests
{
    public abstract class RepositoryTests
    {
        public class DummyReplayable : AggregateBase
        {
            public DummyReplayable(string id)
                : base(id)
            {
                ApplyChange(new DummyEvent());
            }
        }

        public class DummyAppendable : AggregateBase
        {
            public DummyAppendable(string id)
                : base(id)
            {
                ApplyChange(new DummyEvent());
            }
        }

        public class DummyEvent
        {
        }

        protected IRepository Repository;

        protected abstract IRepository Create();

        public RepositoryTests()
        {
            Repository = Create();
        }

        [Fact]
        public async void SaveAsync_SaveNewReplayableStream_Pass()
        {
            await Repository.SaveAsync(new DummyReplayable("abc"));
        }

        [Fact]
        public async void SaveAsync_SaveNewReplayableToExistingStream_Throws()
        {
            await Repository.SaveAsync(new DummyReplayable("abc"));
            await Assert.ThrowsAsync<AggregateVersionException>(async () =>
            {
                await Repository.SaveAsync(new DummyReplayable("abc"));
            });
        }

        [Fact]
        public async void SaveAsync_SaveNewAppendableStream_Pass()
        {
            await Repository.SaveAsync(new DummyAppendable("abc"));
        }

        [Fact]
        public async void SaveAsync_SaveNewAppendableToExistingStream_Pass()
        {
            await Repository.AppendAsync(new DummyAppendable("abc"));
            await Repository.AppendAsync(new DummyAppendable("abc"));
        }

        [Fact]
        public async void SaveNewAsync_SaveStreamAsNew_Pass()
        {
            await Repository.SaveNewAsync(new DummyAppendable("abc"));
        }

        [Fact]
        public async void SaveNewAsync_SaveAnotherStreamAsNew_Throws()
        {
            await Repository.SaveNewAsync(new DummyAppendable("abc"));
            await Assert.ThrowsAsync<AggregateVersionException>(async () =>
            {
                await Repository.SaveNewAsync(new DummyAppendable("abc"));
            });
        }
    }

    public class InMemoryRepositoryTests : RepositoryTests
    {
        protected override IRepository Create()
        {
            return new InMemoryRepository();
        }
    }
}