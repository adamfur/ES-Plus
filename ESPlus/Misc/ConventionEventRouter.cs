using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace ESPlus.Misc
{
    public class ConventionEventRouter
    {
        private readonly IDictionary<Type, Action<object>> _handlers = new Dictionary<Type, Action<object>>();
        private readonly ISet<string> _handle = new HashSet<string>();

        public void Register(object aggregate, string route = "Apply")
        {
            if (aggregate == null)
            {
                throw new ArgumentNullException(nameof(aggregate));
            }

            // Get instance methods named Apply with one parameter returning void
            var applyMethods = aggregate.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == route && m.GetParameters().Length == 1 && m.ReturnParameter.ParameterType == typeof(void))
                .Select(m => new
                {
                    Method = m,
                    MessageType = m.GetParameters().Single().ParameterType
                });

            foreach (var apply in applyMethods)
            {
                // Console.WriteLine($"foreach (var apply in applyMethods) {apply.MessageType.Name}");
                _handle.Add(apply.MessageType.FullName);
                _handlers[apply.MessageType] = payload => apply.Method.Invoke(aggregate, new[] { payload });
                // _handlers[apply.MessageType] = payload => { Console.WriteLine(payload); };
            }
        }

        public void Dispatch(object eventMessage)
        {
            if (_handlers.TryGetValue(eventMessage.GetType(), out var handler))
            {
                try
                {
                    handler(eventMessage);
                }
                catch (Exception)
                {
                    Console.WriteLine("--- Json --------------");
                    Console.WriteLine(eventMessage.GetType().FullName);
                    Console.WriteLine(JsonConvert.SerializeObject(eventMessage, Formatting.Indented));
                    throw;
                }
            }
        }

        public IEnumerable<long> Filter()
        {
            var algorithm = xxHashFactory.Instance.Create(new xxHashConfig { HashSizeInBits = 64 });

            return _handle.Select(x => BitConverter.ToInt64(algorithm.ComputeHash(Encoding.UTF8.GetBytes(x)).Hash, 0))
                .OrderBy(x => x);
        }

        public bool CanHandle(string type)
        {
            return _handle.Contains(type);
        }
    }
}
