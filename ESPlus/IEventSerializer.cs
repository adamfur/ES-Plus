using System;

namespace ESPlus
{
    public interface IEventSerializer
    {
        byte[] Serialize<T>(T graph);
        object Deserialize(Type type, byte[] buffer);
    }
}