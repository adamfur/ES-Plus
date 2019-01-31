using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ESPlus.Interfaces;

namespace ESPlus.Specification
{
    public abstract class Specification<TAggregate> : IDisposable
        where TAggregate : IAggregate
    {
        private List<object> _emittedEvents = new List<object>();
        protected TAggregate Aggregate = default(TAggregate);
        private Action _given = null;
        private Action _when = null;
        private Action _then = null;
        private Exception _exception = null;
        private bool _faulted = false;
        private bool _catchedException = false;
        private bool _disposed = false;
        private int _index = 0;

        protected void Mute()
        {
            _faulted = true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_faulted)
            {
                return;
            }

            if (_exception != null)
            {
                throw _exception;
            }

            if (_given == null)
            {
                throw new Exception("Given was never set");
            }

            if (_then == null)
            {
                throw new Exception("Then was never set");
            }

            if (_index != _emittedEvents.Count)
            {
                var count = 0;
                var builder = new StringBuilder();

                foreach (var evt in _emittedEvents)
                {
                    builder.AppendLine($"{count++}. {evt.GetType().FullName}");
                }

                throw new Exception($"Did not match all events {_index} vs. {_emittedEvents.Count}\n{builder}");
            }
        }

        protected void Given(Action action)
        {
            if (_given != null)
            {
                _faulted = true;
                throw new Exception("Given has already been specified");
            }

            _given = action;

            try
            {
                _given();
                _emittedEvents.AddRange(Aggregate.TakeUncommittedEvents());
            }
            catch (Exception ex)
            {
                _exception = ex;
            }

            if (Aggregate == null)
            {
                _faulted = true;
                throw new Exception("No Aggregate wasn't assigned in Given");
            }
        }

        protected void When(Action action)
        {
            if (_given == null)
            {
                _faulted = true;
                throw new Exception("Given has to be specified before When");
            }

            if (_then != null)
            {
                _faulted = true;
                throw new Exception("When has to be specified before Then");
            }

            if (_when != null)
            {
                _faulted = true;
                throw new Exception("When has already been specified");
            }

            _when = action;

            try
            {
                _when();
                _emittedEvents.Clear();
                _emittedEvents.AddRange(Aggregate.TakeUncommittedEvents());
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        protected void ThenNothing()
        {
            Then(() => { });
        }

        protected void Then(Action action)
        {
            if (_given == null)
            {
                _faulted = true;
                throw new Exception("Given has to be specified before Then");
            }

            if (_then != null)
            {
                _faulted = true;
                throw new Exception("Then has already been specified");
            }

            _then = action;
            _then();
        }

        protected void ThenThrows<TException>()
            where TException : Exception
        {
            ThenThrows<TException>(p => true);
        }

        protected void ThenThrows<TException>(Expression<Predicate<TException>> expr)
            where TException : Exception
        {
            if (_catchedException)
            {
                throw new Exception("Exception has already been catched");
            }

            if (_exception == null)
            {
                throw new Exception("No exception has been thrown!");
            }

            if (_exception.GetType() != typeof(TException))
            {
                throw new Exception($"Exception: {_exception.GetType().Name} was thrown, expected: {typeof(TException).Name}");
            }

            if (!expr.Compile()((TException)_exception))
            {
                throw new Exception($"Exception: {expr}");
            }

            _faulted = true;
            _catchedException = true;
            _then = () => { };
        }

        protected void Is<T>()
        {
            Is<T>(e => true);
        }

        protected void Is<T>(Expression<Predicate<T>> expr)
        {
            var @event = _index < _emittedEvents.Count ? _emittedEvents[_index] : null;
            var compiled = expr.Compile();

            ++_index;

            if (@event == null)
            {
                _faulted = true;
                throw new Exception($"Expected event of type {typeof(T).FullName} got nothing");
            }

            if (@event.GetType() != typeof(T) || !compiled(((T)@event)))
            {
                _faulted = true;
                throw new Exception($"Didn't find event of type {typeof(T).FullName} w/ {expr}");
            }
        }
    }
}