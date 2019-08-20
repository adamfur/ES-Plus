using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ESPlus.Aggregates
{
    public class ConventionEventRouterAsync
    {
        readonly bool throwOnApplyNotFound;
        private readonly IDictionary<Type, Func<object, Task>> handlers = new Dictionary<Type, Func<object, Task>>();
        private readonly ISet<string> _handle = new HashSet<string>();
        private object registered;

        public ConventionEventRouterAsync() : this(true)
        {
        }

        public ConventionEventRouterAsync(bool throwOnApplyNotFound)
        {
            this.throwOnApplyNotFound = throwOnApplyNotFound;
        }

        public ConventionEventRouterAsync(bool throwOnApplyNotFound, object aggregate) : this(throwOnApplyNotFound)
        {
            Register(aggregate);
        }

        public virtual void Register(object aggregate, string route = "Apply")
        {
            if (aggregate == null)
                throw new ArgumentNullException("aggregate");

            this.registered = aggregate;

            // Get instance methods named Apply with one parameter returning void
            var applyMethods = aggregate.GetType()
                .GetRuntimeMethods()// (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == route && m.GetParameters().Length == 1 && m.ReturnParameter.ParameterType == typeof(Task))
                .Select(x =>
                {
                    _handle.Add(x.GetParameters().Single().ParameterType.FullName);
                    return x;
                })
                .Select(m => new
                {
                    Method = m,
                    MessageType = m.GetParameters().Single().ParameterType
                });


            foreach (var apply in applyMethods)
            {
                _handle.Add(apply.MessageType.FullName);

                //MethodInfo applyMethod = apply.Method;
                //this.handlers.Add(apply.MessageType, m => applyMethod.Invoke(aggregate, new[] { m as object }));
                this.handlers.Add(apply.MessageType, Build((dynamic)Activator.CreateInstance(apply.MessageType), aggregate, apply.Method));
            }
        }
        
        private Func<object, Task> Build<T>(T typeInstance, object instance, MethodInfo applyMethod)
        {
            var specificDelegate = ((Func<T, Task>)Delegate.CreateDelegate(typeof(Func<T, Task>), instance, applyMethod));
            var genericDelegate = (Func<object, Task>)(x => specificDelegate((T)x));

            return genericDelegate;
        }

        public async Task DispatchAsync(object eventMessage)
        {
            //if (eventMessage == null)
            //throw new ArgumentNullException("eventMessage");

            Func<object, Task> handler;

            if (this.handlers.TryGetValue(eventMessage.GetType(), out handler))
            {
                await handler(eventMessage);
            }
            //else if (this.throwOnApplyNotFound)
            //this.registered.ThrowHandlerNotFound(eventMessage);
        }

        private void Register(Type messageType, Func<object, Task> handler)
        {
            this.handlers[messageType] = handler;
        }

        public bool CanHandle(string type)
        {
            return _handle.Contains(type);
        }
    }
}

