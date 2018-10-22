using System;
using MessagePack;

namespace ESPlus
{
    public class EventMessagePackSerializer : IEventSerializer
    {
        public byte[] Serialize<T>(T graph)
        {
            return MessagePackSerializer.Serialize(graph);
        }

        public object Deserialize(Type type, byte[] buffer)
        {
            return MessagePackSerializer.NonGeneric.Deserialize(type, buffer);
        }
    }
}