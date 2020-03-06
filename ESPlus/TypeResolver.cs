using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ESPlus.Interfaces;

namespace ESPlus
{
    public static class TypeResolver
    {
        private static readonly Dictionary<Type, List<Type>> Types = new Dictionary<Type, List<Type>>();

        static TypeResolver()
        {
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(IAggregate).IsAssignableFrom(x))
                .ToList()
                .ForEach(type => Types[type] = ResolveFor(type));
        }
        
        private static List<Type> ResolveFor(Type type)
        {
            return type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "Apply" && x.ReturnType == typeof(void))
                .Where(x => x.GetCustomAttribute(typeof(NoReplayAttribute)) == null)
                .Select(x => x.GetParameters().Single().ParameterType)
                .ToList();
        }

        public static IEnumerable<Type> Resolve(Type type)
        {
            return Types[type];
        }
    }
}