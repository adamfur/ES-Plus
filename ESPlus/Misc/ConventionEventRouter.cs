using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ESPlus.Aggregates
{
    public class ConventionEventRouter
    {
        readonly bool throwOnApplyNotFound;
        private readonly IDictionary<Type, Action<object>> handlers = new Dictionary<Type, Action<object>>();
        private readonly ISet<string> _handle = new HashSet<string>();
        private object registered;

        public ConventionEventRouter() : this(true)
        {
        }

        public ConventionEventRouter(bool throwOnApplyNotFound)
        {
            this.throwOnApplyNotFound = throwOnApplyNotFound;
        }

        public ConventionEventRouter(bool throwOnApplyNotFound, object aggregate) : this(throwOnApplyNotFound)
        {
            Register(aggregate);
        }

        // public virtual void Register(object aggregate, string route = "Apply")
        // {
        //     if (aggregate == null)
        //         throw new ArgumentNullException("aggregate");

        //     this.registered = aggregate;

        //     // Get instance methods named Apply with one parameter returning void
        //     var applyMethods = aggregate.GetType()
        //         .GetRuntimeMethods()// (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        //         .Where(m => m.Name == route && m.GetParameters().Length == 1 && m.ReturnParameter.ParameterType == typeof(void))
        //         .Select(x =>
        //         {
        //             _handle.Add(x.GetParameters().Single().ParameterType.FullName);
        //             return x;
        //         })
        //         .Select(m => new
        //         {
        //             Method = m,
        //             MessageType = m.GetParameters().Single().ParameterType
        //         });

        //     foreach (var apply in applyMethods)
        //     {
        //         _handle.Add(apply.MessageType.FullName);

        //         //MethodInfo applyMethod = apply.Method;
        //         //this.handlers.Add(apply.MessageType, m => applyMethod.Invoke(aggregate, new[] { m as object }));
        //         this.handlers.Add(apply.MessageType, Build((dynamic) Activator.CreateInstance(apply.MessageType, true), aggregate, apply.Method));
        //     }
        // }

        public virtual void Register(object aggregate, string route = "Apply")
        {
            if (aggregate == null)
                throw new ArgumentNullException("aggregate");

            this.registered = aggregate;

            // Get instance methods named Apply with one parameter returning void
            var applyMethods = aggregate.GetType()
                .GetRuntimeMethods()// (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == route && m.GetParameters().Length == 1 && m.ReturnParameter.ParameterType == typeof(void))
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

                // var method = this.GetType().GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Instance);
                // Console.WriteLine($"apply.MessageType: {apply.MessageType.Name}");
                // var generic = method.MakeGenericMethod(apply.MessageType);

                // this.handlers.Add(apply.MessageType, (Action<object>)generic.Invoke(this, new object[] { apply.Method }));

                var applyMethod = apply.Method;
                this.handlers.Add(apply.MessageType, payload => applyMethod.Invoke(aggregate, new[] { payload }));

                // this.handlers.Add(apply.MessageType, (Action<object>)generic.Invoke(this, new object[] { apply.Method }));
            }
        }

        // private Action<object> Build<T>(MethodInfo applyMethod)
        // {
        //     // var specificDelegate = ((Action<T>)Delegate.CreateDelegate(typeof(Action<T>), payload, applyMethod));
        //     // var genericDelegate = (Action<object>)(x => specificDelegate((T)x));

        //     // return genericDelegate;

        //     var specificDelegate = (Action<T>)(payload => Delegate.CreateDelegate(typeof(Action<T>), payload, applyMethod));
        //     // var specificDelegate = (Action<T>)(payload => Console.WriteLine($"[::] {payload.GetType().Name} vs. {typeof(T).Name}"));
        //     var genericDelegate = (Action<object>)(payload => specificDelegate((T)payload));

        //     return genericDelegate;
        // }

        public virtual void Dispatch(object eventMessage)
        {
            //if (eventMessage == null)
            //throw new ArgumentNullException("eventMessage");

            Action<object> handler;
            if (this.handlers.TryGetValue(eventMessage.GetType(), out handler))
            {
                handler(eventMessage);
            }
            //else if (this.throwOnApplyNotFound)
            //this.registered.ThrowHandlerNotFound(eventMessage);
        }

        private void Register(Type messageType, Action<object> handler)
        {
            this.handlers[messageType] = handler;
        }

        public bool CanHandle(string type)
        {
            return _handle.Contains(type);
        }
    }
}

/*
delegate void OpenInstanceDelegate(A instance, int a);

class A
{
    public void Method(int a) {}

    static void Main(string[] args)
    {
        A a = null;
        MethodInfo method = typeof(A).GetMethod("Method");
        OpenInstanceDelegate action = (Action<object>)Delegate.CreateDelegate(typeof(Action<object>), a, method);

        PossiblyExecuteDelegate(action);
    }
}
*/
