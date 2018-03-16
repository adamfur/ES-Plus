using ESPlus.Interfaces;
using Xunit;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using ESPlus.Aggregates;

namespace ESPlus.Specification
{
    public abstract class Specification<TAggregate> : IDisposable
        where TAggregate : IAggregate
    {
        protected TAggregate Aggregate;
        private Exception _exception;
        private List<object> _emittedEvents = new List<object>();
        private bool _muteException = false;
        private int _offset = 0;
        private bool _doThrow = false;

        public IEnumerable<object> Events => _emittedEvents;

        protected void Given(Action act)
        {
            _doThrow = true;
            act();
        }

        protected void When(Action when)
        {
            _doThrow = true;
            try
            {
                Aggregate.TakeUncommittedEvents();
                when();
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        protected void Then(Action then)
        {
            if (_exception != null)
            {
                _muteException = true;
                throw _exception;
            }

            _doThrow = false;
            _emittedEvents.AddRange(Aggregate.TakeUncommittedEvents());
            then();
            _muteException = true;
        }

        public void ThenExecute()
        {
            _doThrow = false;
        }

        protected void ThenNothing()
        {
            Then(() => { });
        }

        protected void ThenThrows<TException>()
            where TException : Exception
        {
            ThenThrows<TException>(p => true);
        }

        protected void ThenThrows<TException>(Expression<Predicate<TException>> expr)
            where TException : Exception
        {
            _muteException = true;
            _doThrow = false;

            if (_exception == null)
            {
                throw new Exception("No exception has been thrown!");
            }

            if (_exception.GetType() != typeof(TException))
            {
                throw new Exception($"Exception: {_exception.GetType()} was thrown, expected: {typeof(TException)}");
            }

            if (!expr.Compile()((TException)_exception))
            {
                throw new Exception($"Exception: {expr}");
            }
        }

        protected void Is<T>()
        {
            Is<T>(e => true);
        }

        private object CurrentEvent
        {
            get
            {
                return _emittedEvents[_offset];
            }
        }

        protected void Is<T>(Expression<Predicate<T>> expr)
        {
            var compiled = expr.Compile();
            var @event = CurrentEvent;

            if (@event.GetType() != typeof(T) || !compiled(((T)@event)))
            {
                _muteException = true;
                throw new Exception($"Didn't find event of type {typeof(T).FullName} w/ {expr}");
            }
            ++_offset;
            //_expectedEvents.Add(@event);
        }

        protected void Any<T>()
        {
            Any<T>(e => true);
        }

        protected void Any<T>(Expression<Predicate<T>> expr)
        {
            var compiled = expr.Compile();

            if (!Events.OfType<T>().Any(e => compiled(e)))
            {
                throw new Exception($"Didn't find event of type {typeof(T).FullName} w/ {expr}");
            }
        }

        void IDisposable.Dispose()
        {
            if (!_muteException && _exception != null)
            {
                throw _exception;
            }

            if (_doThrow)
            {
                throw new Exception("Then()-statement expected");
            }
        }
    }
}