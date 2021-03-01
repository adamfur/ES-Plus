using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using ESPlus.MoonGoose;
using Xunit;
using Xunit.Abstractions;

namespace ESPlus.IntegrationTests
{
    public class MoonGooseTests
    {
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
            _testOutputHelper.WriteLine((await _driver.ChecksumAsync(_database)).ToString());
        }
        
        [Fact]
        public async Task Get_NonExistantFile_Empty() // Should throw
        {
            var file = await _driver.GetAsync(_database, "Tenant", "file");
            
            Assert.Empty(file);
        }
        
        [Fact]
        public async Task Search_NonExistantFile_Empty()
        {
            var any = false;
            
            await foreach (var item in _driver.SearchAsync(_database, "Tenant", new[] {0L}))
            {
                any = true;
            }
        
            Assert.False(any);
        }
        
        [Fact]
        public async Task Put_Nothing_Pass()
        {
            await _driver.PutAsync(_database, new List<Document>());
        }     
        
        [Fact]
        public async Task Put_SaveOneDocument_Read()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            });
            var file = await _driver.GetAsync(_database, "Tenant", "file");
            
            Assert.NotEmpty(file);
        }    
        
        [Fact]
        public async Task Put_SaveOneDocumentOtherTenant_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            });
            
            await Assert.ThrowsAsync<MoonGooseException>(() => _driver.GetAsync(_database, "Tenant-2", "file"));
        } 
        
        [Fact]
        public async Task Put_SaveOneDocumentOtherDatabase_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            });
            var file = await _driver.GetAsync(Guid.NewGuid().ToString(), "Tenant-2", "file");
            
            Assert.Empty(file);
        }     
        
        [Fact]
        public async Task ThrowsAsync_DoThrow_Throws()
        {
            var exception = await Assert.ThrowsAsync<MoonGooseException>(() => _driver.SimulateExceptionThrow());
            
            Assert.Equal("I'm a teapot", exception.Message);
        }          
        
        [Fact]
        public async Task Delete_NonExisting_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Delete),
            });
            
            await Assert.ThrowsAsync<MoonGooseException>(() => _driver.GetAsync(_database, "Tenant", "file"));
        }    
        
        [Fact]
        public async Task Delete_Existing_Nothing()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Save),
            });            
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant", "file", null, Operation.Delete),
            });
        
            await Assert.ThrowsAsync<MoonGooseException>(() => _driver.GetAsync(_database, "Tenant", "file"));
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
                new("Tenant",
                    "file", null, Operation.Save),
            });
            var any = false;
            
            await foreach (var item in _driver.SearchAsync(_database, "Tenant", new[] {1337L}))
            {
                any = true;
            }
        
            Assert.True(any);
        }    
        
        [Fact]
        public async Task Search_Deleted_Empty()
        {
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant",
                    "file", null, Operation.Save),
            });
            await _driver.PutAsync(_database, new List<Document>
            {
                new("Tenant",
                    "file", null, Operation.Delete),
            });            
            var any = false;
            
            await foreach (var item in _driver.SearchAsync(_database, "Tenant", new[] {1337L}))
            {
                any = true;
            }
        
            Assert.False(any);
        }          
    }
}