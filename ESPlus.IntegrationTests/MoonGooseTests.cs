using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using ESPlus.MoonGoose;
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
            _testOutputHelper.WriteLine((await _driver.ChecksumAsync(_database, CancellationToken.None)).ToString());
        }
        
        [Fact]
        public async Task Get_NonExistantFile_Empty() // Should throw
        {
            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(_database, "Tenant", "file", CancellationToken.None));
        }
        
        [Fact]
        public async Task Search_NonExistantFile_Empty()
        {
            var any = false;
            
            await foreach (var item in _driver.SearchAsync(_database, "Tenant", new[] {0L}, 0, 100, CancellationToken.None))
            {
                any = true;
            }
        
            Assert.False(any);
        }
        
        [Fact]
        public async Task Put_Nothing_Pass()
        {
            await _driver.PutAsync(_database, new List<Document>(), Position.Start, Position.Start, CancellationToken.None);
        }     
        
        [Fact]
        public async Task Put_SaveOneDocument_Read()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, CancellationToken.None);
            var file = await _driver.GetAsync(_database, "Tenant", "file", CancellationToken.None);
            
            Assert.NotEmpty(file);
        }    
        
        [Fact]
        public async Task Put_SaveOneDocumentOtherTenant_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, CancellationToken.None);
            
            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(_database, "Tenant-2", "file", CancellationToken.None));
        } 
        
        [Fact]
        public async Task Put_SaveOneDocumentOtherDatabase_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, CancellationToken.None);

            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(Guid.NewGuid().ToString(), "Tenant-2", "file", CancellationToken.None));
        }     
        
        [Fact]
        public async Task ThrowsAsync_DoThrow_Throws()
        {
            var exception = await Assert.ThrowsAsync<MoonGooseException>(() => _driver.SimulateExceptionThrow(CancellationToken.None));
            
            Assert.Equal("I'm a teapot", exception.Message);
        }          
        
        [Fact]
        public async Task Delete_NonExisting_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Delete),
            }, Position.Start, Position.Start, CancellationToken.None);
            
            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(_database, "Tenant", "file", CancellationToken.None));
        }    
        
        [Fact]
        public async Task Delete_Existing_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, CancellationToken.None);            
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Delete),
            }, Position.Start, Position.Start, CancellationToken.None);
        
            await Assert.ThrowsAsync<MoonGooseNotFoundException>(() => _driver.GetAsync(_database, "Tenant", "file", CancellationToken.None));
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
                new IndexDocument("Tenant", "file", new {}, Operation.Save, new[] {1337L}),
            }, Position.Start, Position.Start, CancellationToken.None);

            Assert.NotEmpty(await _driver.SearchAsync(_database, "Tenant", new[] {1337L}, 0, 100, CancellationToken.None).ToListAsync());
        }    
        
        [Fact]
        public async Task Search_Deleted_Empty()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            }, Position.Start, Position.Start, CancellationToken.None);
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Delete),
            }, Position.Start, Position.Start, CancellationToken.None);            
            
            Assert.False((await _driver.SearchAsync(_database, "Tenant", new[] {1337L}, 0, 100, CancellationToken.None).ToListAsync()).Any());
        }          
    }
}