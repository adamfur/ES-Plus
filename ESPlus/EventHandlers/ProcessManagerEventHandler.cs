using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using ESPlus.Misc;

namespace ESPlus.EventHandlers
{
    public class ProcessManagerEventHandler<TContext, TProcessManager> : BasicEventHandler<TContext>
        where TProcessManager : ProcessManager
        where TContext : IEventHandlerContext
    {
        private readonly IRepository _repository;
        private readonly Dictionary<Type, Func<object, string>> _map = new Dictionary<Type, Func<object, string>>();

        public ProcessManagerEventHandler(TContext context, IEventTypeResolver eventTypeResolver, IRepository repository)
            : base(context, eventTypeResolver, new NullFlushPolicy())
        {
            _repository = repository;
        }

        protected override void RegisterRouter(ConventionEventRouter router) //Routes
        {
            //router.Register(_processManager, "Transition");
        }

        public override bool DispatchEvent(object @event)
        {
            throw new NotImplementedException();
            // System.Console.WriteLine($"ProcessManagerEventHandler {@event.GetType().Name}");
            // Task.Factory.StartNew(async () =>
            // {
            //     var aggregate = await _repository.GetByIdAsync<TProcessManager>("adamf#bonus");

            //     var router = new ConventionEventRouter();
            //     router.Register(aggregate, "Transition");
            //     router.Dispatch(@event);

            //     //System.Console.WriteLine($" *** SaveAsync BEGIN {@event.GetType().Name}");
            //     await _repository.SaveAsync(aggregate);
            //     //System.Console.WriteLine(" *** SaveAsync END");
            // }).Wait();

            // return true;
        }

        public void AddMap<T>(Func<T, string> map)
        {
            Func<T, string> func = map;
            Func<object, string> func2 = x => func((T) x);

            _map[typeof(T)] = func2;
        }

        private string Map(object @event)
        {
            // if (!_map.ContainsKey(@event.GetType()))
            // {
            //     //throw new Exception($"No mapping for {@event.GetType().FullName}");
            // }
            return _map[@event.GetType()](@event);
        }

        public override async Task<bool> DispatchEventAsync(object @event)
        {
            if (!_map.ContainsKey(@event.GetType()))
            {
                //Console.WriteLine($"No mapping for {@event.GetType().FullName}");
                return false;
            }

            var aggregate = await _repository.GetByIdAsync<TProcessManager>(Map(@event));

            aggregate.Repository = _repository;

            var router = new ConventionEventRouterAsync();
            router.Register(aggregate, "TransitionAsync");
            await router.DispatchAsync(@event);
            await _repository.SaveAsync(aggregate);

            return true;
        }
    }
}
