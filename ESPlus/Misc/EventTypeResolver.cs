using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ESPlus.Interfaces;

namespace ESPlus.Misc
{
    public class EventTypeResolver : IEventTypeResolver
    {
        private readonly Dictionary<string, Type> _typesByFullName = new Dictionary<string, Type>();

        public void RegisterType(Type type)
        {
            _typesByFullName[type.FullName] = type;
        }

        public static IEventTypeResolver Default()
        {
            var instance = new EventTypeResolver();
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(IAggregate).IsAssignableFrom(x))
                .SelectMany(x => x.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                .Where(x => x.Name == "Apply" && x.ReturnType == typeof(void))
                .Where(x => x.GetCustomAttribute(typeof(NoReplayAttribute)) == null)
                .Select(x => x.GetParameters().First().ParameterType)
                .ToList()
                .ForEach(t => instance.RegisterType(t));

            return instance;
        }

        private string ExtractEventId(Type type)
        {
            var attribute = type.GetCustomAttributes(typeof(EventIdentifierAttribute), true).FirstOrDefault() as EventIdentifierAttribute;

            return attribute?.EventId;
        }

        public Type ResolveType(string fullName, string name = "", string eventId = "")
        {
            return FindByFullName(fullName) ?? throw new ArgumentException($"Unabel to resolve type '{fullName}'!");
        }

        private Type FindByFullName(string type)
        {
            if (_typesByFullName.ContainsKey(type))
            {
                return _typesByFullName[type];
            }

            return null;
        }
    }
}
