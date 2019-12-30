using System;

namespace ESPlus.Misc
{
    public interface IEventTypeResolver
    {
         Type ResolveType(string fullName, string name = "", string eventId = "");
         void RegisterType(Type type);
    }
}
