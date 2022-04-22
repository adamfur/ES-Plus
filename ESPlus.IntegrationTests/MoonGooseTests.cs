using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using ESPlus.MoonGoose;
using ESPlus.Storage;
using Xunit;
using Xunit.Abstractions;

namespace ESPlus.IntegrationTests
{
    public class MoonGooseTests
    {
        private class IndexDocument : Document
        {
            public IndexDocument(string tenant, string path, object item, Operation operation, long[] keywords)
                : base(tenant, path, item, operation)
            {
                Keywords = keywords;
                Flags = Flags.Indexed;
            }

            public override long[] Keywords { get; }
        }

        private readonly ITestOutputHelper _testOutputHelper;
        private MoonGooseDriver _driver;
        private string _database;

        public MoonGooseTests(ITestOutputHelper testOutputHelper)
        {
            _database = Guid.NewGuid().ToString();
            _testOutputHelper = testOutputHelper;
            _driver = new MoonGooseDriver("localhost:9200");
        }

        [Fact]
        public async Task Checksum()
        {
            _testOutputHelper.WriteLine((await _driver.ChecksumAsync(_database, default)).ToString());
        }

        [Fact]
        public async Task Get_NonExistantFile_Empty() // Should throw
        {
            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(_database, "Tenant", "file", default));
        }

        [Fact]
        public async Task Search_NonExistantFile_Empty()
        {
            var any = false;

            await foreach (var item in _driver.SearchAsync(_database, "Tenant", new[] { 0L }, 0, 100, default))
            {
                any = true;
            }

            Assert.False(any);
        }

        [Fact]
        public async Task Put_Nothing_Pass()
        {
            await _driver.PutAsync(_database, new List<Document>(), Position.Start, Position.Start, default);
        }

        [Fact]
        public async Task Put_SaveOneDocument_Read()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, default);
            var file = await _driver.GetAsync(_database, "Tenant", "file", default);

            Assert.NotEmpty(file);
        }

        [Fact]
        public async Task Put_SaveManyDocument_Read()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file1", new { Hello = "World" }, Operation.Save),
                new("Tenant", "file2", new { Hello = "Dad" }, Operation.Save),
            }, Position.Start, Position.Start, default);
            var file1 = await _driver.GetAsync(_database, "Tenant", "file1", default);
            var file2 = await _driver.GetAsync(_database, "Tenant", "file2", default);

            var str1 = Encoding.UTF8.GetString(file1);
            var str2 = Encoding.UTF8.GetString(file2);

            Assert.NotEmpty(file1);
            Assert.NotEmpty(file2);
        }

        [Fact]
        public async Task Put_SaveManyDocument_Read2()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new IndexDocument("Tenant", "file1", new { Hello = "World" }, Operation.Save, new[] { 1L, 3L, }),
            }, Position.Start, Position.Start, default);

            Assert.True(_driver.SearchAsync(_database, "Tenant", new[] { 1L }, 0, 100, default).ToListAsync().Result.Any());
            Assert.False(_driver.SearchAsync(_database, "Tenant", new[] { 2L }, 0, 100, default).ToListAsync().Result.Any());
            Assert.True(_driver.SearchAsync(_database, "Tenant", new[] { 3L }, 0, 100, default).ToListAsync().Result.Any());

            await _driver.PutAsync(_database, new List<Document>
            {
                new IndexDocument("Tenant", "file1", new { Hello = "World" }, Operation.Save, new[] { 2L, 3L, }),
            }, Position.Start, Position.Start, default);

            Assert.False(_driver.SearchAsync(_database, "Tenant", new[] { 1L }, 0, 100, default).ToListAsync().Result.Any());
            Assert.True(_driver.SearchAsync(_database, "Tenant", new[] { 2L }, 0, 100, default).ToListAsync().Result.Any());
            Assert.True(_driver.SearchAsync(_database, "Tenant", new[] { 3L }, 0, 100, default).ToListAsync().Result.Any());
        }

        [Fact]
        public async Task Put_SaveManyDocument_Read3()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new IndexDocument("Tenant", "file1", new { Hello = "World" }, Operation.Save, new[] { 1L, 3L, 4L }),
            }, Position.Start, Position.Start, default);

            Assert.True(_driver.SearchAsync(_database, "Tenant", new[] { 1L }, 0, 100, default).ToListAsync().Result.Any());
            Assert.False(_driver.SearchAsync(_database, "Tenant", new[] { 2L }, 0, 100, default).ToListAsync().Result.Any());
            Assert.True(_driver.SearchAsync(_database, "Tenant", new[] { 3L }, 0, 100, default).ToListAsync().Result.Any());

            await _driver.PutAsync(_database, new List<Document>
            {
                new IndexDocument("Tenant", "file1", new { Hello = "World" }, Operation.Save, new[] { 2L, 3L }),
            }, Position.Start, Position.Start, default);

            Assert.False(_driver.SearchAsync(_database, "Tenant", new[] { 1L }, 0, 100, default).ToListAsync().Result.Any());
            Assert.True(_driver.SearchAsync(_database, "Tenant", new[] { 2L }, 0, 100, default).ToListAsync().Result.Any());
            Assert.True(_driver.SearchAsync(_database, "Tenant", new[] { 3L }, 0, 100, default).ToListAsync().Result.Any());
        }

        [Fact]
        public async Task Put_SaveOneDocumentOtherTenant_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, default);

            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(_database, "Tenant-2", "file", default));
        }

        [Fact]
        public async Task Put_SaveOneDocumentOtherDatabase_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, default);

            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(Guid.NewGuid().ToString(), "Tenant-2", "file", default));
        }

        [Fact]
        public async Task ThrowsAsync_DoThrow_Throws()
        {
            var exception = await Assert.ThrowsAsync<MoonGooseException>(() => _driver.SimulateExceptionThrow(default));

            Assert.Equal("I'm a teapot", exception.Message);
        }

        [Fact]
        public async Task Delete_NonExisting_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Delete),
            }, Position.Start, Position.Start, default);

            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(_database, "Tenant", "file", default));
        }

        [Fact]
        public async Task Delete_Existing_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, default);
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Delete),
            }, Position.Start, Position.Start, default);

            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(_database, "Tenant", "file", default));
        }

        [Fact]
        public void Foo()
        {
            var map = new Dictionary<StringPair, bool>();

            map[new StringPair("jesper", "adam")] = true;

            Assert.True(map.ContainsKey(new StringPair("jesper", "adam")));
            Assert.False(map.ContainsKey(new StringPair("jesper", "adam-2")));
        }

        [Fact]
        public async Task Search_ExistantFile_NotEmpty()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new IndexDocument("Tenant", "file", new { }, Operation.Save, new[] { 1337L }),
            }, Position.Start, Position.Start, default);

            Assert.NotEmpty(await _driver.SearchAsync(_database, "Tenant", new[] { 1337L }, 0, 100, default).ToListAsync());
        }

        [Fact]
        public async Task Search_Deleted_Empty()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, default);
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Delete),
            }, Position.Start, Position.Start, default);

            Assert.False((await _driver.SearchAsync(_database, "Tenant", new[] { 1337L }, 0, 100, default).ToListAsync()).Any());
        }

        [Fact]
        public async Task List()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, default);

            Assert.NotEmpty(_driver.ListAsync(_database, "Tenant", 100, 0, null, default).ToListAsync().Result);
        }
        
        [Fact]
        public async Task ConcurrencyException()
        {
            var document = new Document("Tenant", "file", null, Operation.Save);
            await _driver.PutAsync(_database, document.AsList(), Position.Start, Position.Gen(2), default);

            await Assert.ThrowsAsync<MoonGooseConcurrencyException>(() => _driver.PutAsync(_database, document.AsList(), Position.Gen(3), Position.Gen(5), default));
        }
    }
}