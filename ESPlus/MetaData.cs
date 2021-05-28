using System;
using System.Collections.Generic;
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
        [Key("Tenant")]
        public string Tenant { get; set; }
        [Key("CorrelationId")]
        public string CorrelationId { get; set; }
        [Key("RequestId")]
        public string RequestId { get; set; }
    }
    
    public class MetaData : IMetaObject
    {
        private readonly Lazy<MetaObject> _lazyMetaObject;

        public MetaData()
        {
            _lazyMetaObject = new Lazy<MetaObject>(() => null);
        }

        public MetaData(byte[] eventMeta, IEventSerializer serializer)
        {
            _lazyMetaObject = new Lazy<MetaObject>(() =>
            {
                try
                {
                    var obj = (MetaObject) serializer.Deserialize(typeof(MetaObject), eventMeta);

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
        public string Tenant => _lazyMetaObject.Value?.Tenant;
        public string RequestId => _lazyMetaObject.Value?.RequestId;
    }
}