using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using Newtonsoft.Json;

namespace ESPlus.Storage
{
    public class FileSystemStorage : IStorage
    {
        private string BasePath { get; }
        private readonly HashSet<string> _pathExits = new HashSet<string>();
        private string _container;
        private Dictionary<string, HasObjectId> _writeCache = new Dictionary<string, HasObjectId>();

        public FileSystemStorage(string container, string basePath = "/tmp/esplus")
        {
            BasePath = basePath;
            _container = container;
        }

        public void Put(string path, HasObjectId item)
        {
            var relativePath = Combine(_container, path);

            CreatePath(relativePath);
            File.WriteAllText(Combine(BasePath, relativePath), JsonConvert.SerializeObject(item, new JsonSerializerSettings
            {
                ContractResolver = new CamelCaseExceptDictionaryKeysResolver()
            }));
        }

        public T Get<T>(string path)
            where T : HasObjectId
        {
            try
            {
                var absolutePath = Combine(BasePath, _container, path);
                var text = File.ReadAllText(absolutePath);

                return JsonConvert.DeserializeObject<T>(text);
            }
            catch (FileNotFoundException)
            {
                return default(T);
            }
        }

        public void Flush()
        {
            CreateFolders();
            WriteFiles();
            _writeCache.Clear();
        }

        private void WriteFiles()
        {
            var tasks = _writeCache
                .AsParallel()
                .Select(x => WriteFile(x.Key, x.Value))
                .ToArray();

            Task.WaitAll(tasks);
        }

        private void CreateFolders()
        {
            foreach (var path in _writeCache.Keys)
            {
                CreatePath(path);
            }
        }

        private async Task WriteFile(string path, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            using (var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        private void CreatePath(string relativePath)
        {
            if (_pathExits.Contains(relativePath))
            {
                return;
            }

            var parts = relativePath.Split('/');
            string path = BasePath;

            for (int i = 0; i < parts.Length - 1; ++i)
            {
                string part = parts[i];
                path = Combine(path, part);

                if (_pathExits.Contains(path))
                {
                    continue;
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                _pathExits.Add(path);
            }
        }

        private string Combine(params string[] list)
        {
            return Path.Combine(list).Replace("\\", "/");
        }

        public void Reset()
        {
            var absolutePath = Path.Combine(BasePath, _container);

            if (Directory.Exists(absolutePath))
            {
                Directory.Delete(absolutePath, true);
            }
            _pathExits.Clear();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(long[] parameters)
        {
            throw new System.NotImplementedException();
        }

        public void Delete(string path)
        {
            Directory.Delete(path);
        }
    }
}