using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESPlus.Interfaces;
using Newtonsoft.Json;

namespace ESPlus.Storage
{
    public class FileSystemStorage : IStorage
    {
        private string BasePath { get; }
        private readonly HashSet<string> _pathExits = new HashSet<string>();
        private string _container;
        private Dictionary<string, object> _writeCache = new Dictionary<string, object>();

        public FileSystemStorage(string container, string basePath = "/tmp/esplus")
        {
            BasePath = basePath;
            _container = container;
        }

        public void Put(string path, object item)
        {
            var relativePath = Combine(_container, path);

            CreatePath(relativePath);
            File.WriteAllText(Combine(BasePath, relativePath), JsonConvert.SerializeObject(item));
        }

        public object Get(string path)
        {
            var absolutePath = Combine(BasePath, _container, path);
            var text = File.ReadAllText(absolutePath);

            return text;
        }

        public void Flush()
        {
            /**/
            _writeCache.Values
                .AsParallel()
                .Select(x => JsonConvert.SerializeObject(x));
            /**/
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

        /*
                public void SetContainer(string container)
                {
                    _container = container;
                }

                public void Copy(Dictionary<string, string> map)
                {
                    foreach (var pair in map)
                    {
                        var source = Combine(BasePath, _container, pair.Key);
                        var destination = Combine(_container, pair.Value);

                        CreatePath(destination);

                        File.Copy(source, Combine(BasePath, destination), true);
                    }
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

                public T Get<T>(string path) where T : new()
                {
                    try
                    {
                        var absolutePath = Combine(BasePath, _container, path);
                        var text = File.ReadAllText(absolutePath);

                        return JsonConvert.DeserializeObject<T>(text);
                    }
                    finally
                    {

                    }
                    // catch (DirectoryNotFoundException ex)
                    // {
                    //     throw new BlobNotFoundException(ex.Message, ex);
                    // }
                    // catch (FileNotFoundException ex)
                    // {
                    //     throw new BlobNotFoundException(ex.Message, ex);
                    // }
                }

                public void Put(List<MetaBlob> metas)
                {
                    metas.ForEach(m =>
                    {
                        var relativePath = Combine(_container, m.Path);

                        CreatePath(relativePath);
                        File.WriteAllText(Combine(BasePath, relativePath), JsonConvert.SerializeObject(m.Graph));
                    });
                }


         */
    }
}