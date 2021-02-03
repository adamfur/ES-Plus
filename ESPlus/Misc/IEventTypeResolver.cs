using System;

namespace ESPlus.Misc
{
    public interface IEventTypeResolver
    {
         Type ResolveType(string fullName);
    }
}
