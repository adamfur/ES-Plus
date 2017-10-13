using System;
using System.Linq;
using System.Collections.Generic;

namespace ESPlus.Misc
{
    public class EventTypeResolver : IEventTypeResolver
    {
        private Dictionary<string, Type> _typesByEventId = new Dictionary<string, Type>();
        private Dictionary<string, Type> _typesByFullName = new Dictionary<string, Type>();
        private Dictionary<string, Type> _typesByNameName = new Dictionary<string, Type>();

        public void RegisterTypes(Type[] types)
        {
            foreach (var type in types)
            {
                var eventId = ExtractEventId(type);

                if (eventId != null)
                {
                    _typesByEventId[eventId] = type;
                }

                _typesByFullName[type.FullName] = type;
                _typesByNameName[type.Name] = type;
            }
        }

        private string ExtractEventId(Type type)
        {
            var attribute = type.GetCustomAttributes(typeof(EventIdentifierAttribute), true).FirstOrDefault() as EventIdentifierAttribute;

            return attribute?.EventId;
        }

        public Type ResolveType(string fullName, string name = "", string eventId = "")
        {
            return FindByEventId(eventId) ?? FindByFullName(fullName) ?? FindByName(name) ?? throw new ArgumentException("Unabel to resolve type!!!");
        }

        private Type FindByName(string type)
        {
            if (_typesByNameName.ContainsKey(type))
            {
                return _typesByNameName[type];
            }

            return null;
        }

        private Type FindByFullName(string type)
        {
            if (_typesByFullName.ContainsKey(type))
            {
                return _typesByFullName[type];
            }

            return null;
        }

        private Type FindByEventId(string eventId)
        {
            if (_typesByEventId.ContainsKey(eventId))
            {
                return _typesByEventId[eventId];
            }

            return null;
        }
    }
}
