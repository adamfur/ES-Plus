using System;
using System.Text;
using MessagePack;
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

    [MessagePackObject]
    public class MetaObject : IMetaObject
    {
        [Key("Subject")]
        public string Subject { get; set; }
        [Key("IP")]
        public string IP { get; set; }
        [Key("GivenName")]
        public string GivenName { get; set; }
        [Key("TimestampUtc")]
        public DateTime TimestampUtc { get; set; }
    }
    
    public class MetaData : IMetaObject
    {
        private readonly IEventSerializer _serializer;
        private readonly Lazy<MetaObject> _lazyMetaObject;
        
        public MetaData(byte[] eventMeta, IEventSerializer serializer)
        {
            _serializer = serializer;
            _lazyMetaObject = new Lazy<MetaObject>(() =>
            {
                try
                {
                    var obj = (MetaObject) _serializer.Deserialize(typeof(MetaObject), eventMeta);

                    return obj;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public string Subject => _lazyMetaObject.Value?.Subject;
        public string IP => _lazyMetaObject.Value?.IP;
        public string GivenName => _lazyMetaObject.Value?.GivenName;
        public DateTime TimestampUtc => _lazyMetaObject.Value?.TimestampUtc ?? DateTime.MinValue;
    }
}