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
        protected abstract TAggregate Given();
        protected TAggregate Aggregate;
        private List<object> _actalEvents;
        private Exception _exception;
        private bool _doAssert = true;
        private int _offset = 0;
        private int _skip = 0;
        private bool _scope = false;

        public Specification()
        {
            try
            {
                Aggregate = Given();
                _actalEvents = ((IAggregate)Aggregate).TakeUncommittedEvents().ToList();
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        private object CurrentEvent
        {
            get
            {
                if (_offset + _skip <= _actalEvents.Count)
                {
                    return _actalEvents[_offset + _skip];
                }
                throw new Exception("No more events!");
            }
        }

        protected IEnumerable<object> Events => _actalEvents.Skip(_skip);

        // protected void AssertOrder()
        // {
        // var max = Math.Max(_actalEvents.Count(), _expected.Count());

        // Console.WriteLine("----------------");
        // for (int i = 0; i < max; ++i)
        // {
        //     var actual = (i <= _actalEvents.Count() ? _actalEvents[i].GetType().ToString() : "N/A");
        //     var expected = (i <= _expected.Count() ? _expected[i].ToString() : "N/A");

        //     Console.WriteLine($"{i}) [{(actual == expected ? "x" : " ")}] {actual} vs. {expected}");
        // }
        //throw new Exception("string.Join(Environment.NewLine, list)");
        // }

        protected void Then()
        {
            Then(() => { });
        }

        protected void Then(Action action)
        {
            _scope = true;
            action();
            _scope = false;
        }

        protected void Any<T>()
        {
            Any<T>(e => true);
        }

        protected void Any<T>(Expression<Predicate<T>> expr)
        {
            AssertScope();
            var compiled = expr.Compile();

            _doAssert = false;
            if (!Events.OfType<T>().Any(e => compiled(e)))
            {
                throw new Exception($"Didn't find event of type {typeof(T).FullName} w/ {expr}");
            }
        }

        protected void Is<T>()
        {
            Is<T>(e => true);
        }

        protected void Is<T>(Expression<Predicate<T>> expr)
        {
            AssertScope();
            var compiled = expr.Compile();
            var @event = CurrentEvent;

            if (@event.GetType() != typeof(T) || !compiled(((T)@event)))
            {
                _doAssert = false;
                throw new Exception($"Didn't find event of type {typeof(T).FullName} w/ {expr}");
            }
            ++_offset;
        }

        private void AssertScope()
        {
            if (!_scope)
            {
                throw new Exception("Must be in a Then-scope");
            }
        }

        protected void When(Action<TAggregate> ar)
        {
            try
            {
                ar(Aggregate);
                _skip = _actalEvents.Count;
                _actalEvents.AddRange(((IAggregate)Aggregate).TakeUncommittedEvents().ToList());
            }
            catch (Exception ex)
            {
                _exception = _exception ?? ex;
            }
        }

        protected void ThenThrows<TException>()
            where TException : Exception
        {
            ThenThrows<TException>(p => true);
        }

        protected void ThenThrows<TException>(Expression<Predicate<TException>> expr)
            where TException : Exception
        {
            if (_exception == null)
            {
                _doAssert = false;
                throw new Exception("No exception has been thrown!");
            }

            if (_exception.GetType() != typeof(TException))
            {
                _doAssert = false;
                throw new Exception($"Exception: {_exception.GetType()} was thrown, expected: {typeof(TException)}");
            }

            if (!expr.Compile()((TException)_exception))
            {
                _doAssert = false;
                throw new Exception($"Exception: {expr}");
            }
            _doAssert = false;
        }

        void IDisposable.Dispose()
        {
            if (_doAssert)
            {
                if (_offset + _skip != _actalEvents.Count)
                {
                    throw new Exception($"Missmatching number of events, got: {_offset + _skip}, expected: {_actalEvents.Count - _skip}");
                }
            }
        }
    }
}