using System;

namespace ESPlus
{
    public interface IEventSerializer
    {
        byte[] Serialize<T>(T graph);
        object Deserialize(string eventType, byte[] buffer);
    }
}