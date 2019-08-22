using System;
using System.Text;
using Newtonsoft.Json;

namespace ESPlus
{
    public interface IMetaObject
    {
        string Subject { get; }
        string IP { get; }
        string GivenName { get; }
        DateTime TimestampUtc { get; }
    }

    public class MetaObject : IMetaObject
    {
        public string Subject { get; set; }
        public string IP { get; set; }
        public string GivenName { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
    
    public class MetaData : IMetaObject
    {
        private readonly Lazy<MetaObject> _lazyMetaObject;
        
        public MetaData(byte[] eventMeta)
        {
            _lazyMetaObject = new Lazy<MetaObject>(() =>
            {
                var json = Encoding.UTF8.GetString(eventMeta);
                var obj = JsonConvert.DeserializeObject<MetaObject>(json);

                return obj;
            });
        }

        public string Subject => _lazyMetaObject.Value.Subject;
        public string IP => _lazyMetaObject.Value.IP;
        public string GivenName => _lazyMetaObject.Value.GivenName;
        public DateTime TimestampUtc => _lazyMetaObject.Value.TimestampUtc;
    }
}