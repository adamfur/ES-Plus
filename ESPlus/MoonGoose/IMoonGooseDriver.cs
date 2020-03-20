using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.MoonGoose
{
    public interface IMoonGooseDriver
    {
        Task<byte[]> GetAsync(string database, string key);
        Task PutAsync(string database, IEnumerable<Document> documents);
        IAsyncEnumerable<byte[]> SearchAsync(string database, long[] parameters);
    }
}