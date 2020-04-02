using System.Collections.Generic;
using System.Linq;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class InMemoryStorage : IStorage
    {
        private readonly Dictionary<string, HasObjectId> _data = new Dictionary<string, HasObjectId>();

        public Dictionary<string, HasObjectId> Internal => _data;

        public void Delete(string path)
        {
            _data.Remove(path);
        }

        public void Flush()
        {
        }

        public T Get<T>(string path)
            where T : HasObjectId
        {
            if (_data.ContainsKey(path))
            {
                return (T) _data[path];
            }
            
            return default;
        }

        public void Put(string path, HasObjectId item)
        {
            _data[path] = item;
        }

        public void Reset()
        {
            _data.Clear();
        }
    }
}