using System;
using ESPlus.Misc;
using MessagePack;

namespace ESPlus
{
    public class EventMessagePackSerializer : IEventSerializer
    {
        private readonly IEventTypeResolver _typeResolver;

        public EventMessagePackSerializer(IEventTypeResolver typeResolver)
        {
            _typeResolver = typeResolver;
        }
        
        public byte[] Serialize<T>(T graph)
        {
            return MessagePackSerializer.Serialize(graph);
        }

        public object Deserialize(string eventType, byte[] buffer)
        {
            var type = _typeResolver.ResolveType(eventType);
            
            return MessagePackSerializer.NonGeneric.Deserialize(type, buffer);
        }
    }
}