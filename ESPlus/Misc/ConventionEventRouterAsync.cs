using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ESPlus.Misc
{
    public class ConventionEventRouterAsync
    {
        private readonly IDictionary<Type, Func<object, CancellationToken, Task>> _handlers = new Dictionary<Type, Func<object, CancellationToken, Task>>();
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
                .Where(m => m.Name == route && m.GetParameters().Length == 2 && m.ReturnParameter?.ParameterType == typeof(Task))
                .Where(m => m.GetParameters().Last().ParameterType == typeof(CancellationToken))
                .Select(m => new
                {
                    Method = m,
                    MessageType = m.GetParameters().First().ParameterType
                });

            foreach (var apply in applyMethods)
            {
                _handle.Add(apply.MessageType.FullName);
                _handlers[apply.MessageType] = (@event, cancellationToken) => (Task) apply.Method.Invoke(aggregate, new[] { @event, cancellationToken });
            }
        }

        public Task DispatchAsync(object eventMessage, CancellationToken cancellationToken)
        {
            if (_handlers.TryGetValue(eventMessage.GetType(), out var handler))
            {
                try
                {
                    return handler(eventMessage, cancellationToken);
                }
                catch (Exception)
                {
                    Console.WriteLine($"--- {eventMessage.GetType().FullName} --------------");
                    Console.WriteLine(JsonConvert.SerializeObject(eventMessage, Formatting.Indented));
                    throw;
                }
            }

            return Task.CompletedTask;
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
