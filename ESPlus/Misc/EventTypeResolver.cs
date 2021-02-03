using System;
using System.Linq;
using System.Collections.Generic;

namespace ESPlus.Misc
{
    public class EventTypeResolver : IEventTypeResolver
    {
        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();

        public void RegisterTypes(Type[] types)
        {
            foreach (var type in types)
            {
                _types[type.FullName] = type;
            }
        }

        public Type ResolveType(string fullName)
        {
            return FindByFullName(fullName) ?? throw new ArgumentException($"Unabel to resolve type '{fullName}'!");
        }

        private Type FindByFullName(string type)
        {
            if (_types.TryGetValue(type, out var resolved))
            {
                return resolved;
            }
            
            return null;
        }
    }
}
