using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.FlushPolicies;
using ESPlus.Interfaces;
using ESPlus.Misc;

namespace ESPlus.EventHandlers
{
    public class ProcessManagerEventHandler<TContext, TProcessManager> : BasicEventHandler<TContext>
        where TProcessManager : ProcessManager
        where TContext : class, IEventHandlerContext
    {
        private readonly IRepository _repository;
        private readonly Dictionary<Type, Func<object, string>> _map = new Dictionary<Type, Func<object, string>>();

        public ProcessManagerEventHandler(TContext context, IEventTypeResolver eventTypeResolver, IRepository repository, IEventSerializer eventSerializer)
            : base(context, eventTypeResolver, new NullFlushPolicy(), eventSerializer)
        {
            _repository = repository;
        }

        protected override void RegisterRouter(ConventionEventRouterAsync router) //Routes
        {
            //router.Register(_processManager, "Transition");
        }

        public override Task<bool> DispatchEventAsync(object @event, CancellationToken cancellationToken)
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

        private string Map(object @event)
        {
            // if (!_map.ContainsKey(@event.GetType()))
            // {
            //     //throw new Exception($"No mapping for {@event.GetType().FullName}");
            // }
            return _map[@event.GetType()](@event);
        }

        // public override async Task<bool> DispatchEventAsync(object @event)
        // {
        //     if (!_map.ContainsKey(@event.GetType()))
        //     {
        //         //Console.WriteLine($"No mapping for {@event.GetType().FullName}");
        //         return false;
        //     }
        //
        //     var aggregate = await _repository.GetByIdAsync<TProcessManager>(Map(@event));
        //
        //     aggregate.Repository = _repository;
        //
        //     var router = new ConventionEventRouterAsync();
        //     router.Register(aggregate, "TransitionAsync");
        //     await router.DispatchAsync(@event);
        //     await _repository.SaveAsync(aggregate);
        //
        //     return true;
        // }
    }
}
