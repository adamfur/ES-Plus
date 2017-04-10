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
        protected abstract TAggregate Create();
        protected TAggregate Aggregate;
        private List<object> _actalEvents;
        private List<object> _expected;
        private Exception _exception;
        private bool _doAssert = true;
        private int _offset = 0;

        public Specification()
        {
            try
            {
                Aggregate = Create();
                When();
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
            _actalEvents = ((IAggregate) Aggregate).TakeUncommittedEvents().ToList();
            _expected = new List<object>();
        }

        protected void Is<T>()
        {
            Is<T>(e => true);
        }

        protected void AssertOrder()
        {
            var max = Math.Max(_actalEvents.Count(), _expected.Count());

            Console.WriteLine("----------------");
            for (int i = 0; i < max; ++i)
            {
                var actual = (i <= _actalEvents.Count() ? _actalEvents[i].GetType().ToString() : "N/A");
                var expected = (i <= _expected.Count() ? _expected[i].ToString() : "N/A");

                Console.WriteLine($"{i}) [{(actual == expected ? "x" : " ")}] {actual} vs. {expected}");
            }
            //throw new Exception("string.Join(Environment.NewLine, list)");
        }

        protected void Is<T>(Expression<Predicate<T>> expr)
        {
            _expected.Add(typeof (T));
            if (_actalEvents[_offset].GetType() != typeof (T))
            {
                throw new Exception($"Invalid event type: {_actalEvents[_offset].GetType()}, expected: {typeof (T)}");
            }

            if (!expr.Compile()((T) _actalEvents[_offset]))
            {
                throw new Exception($"Exception: {expr}");
            }            

            ++_offset;
        }

        protected virtual void When()
        {
        }

        protected void Throws<TException>()
            where TException : Exception
        {
            Throws<TException>(p => true);
        }

        protected void Throws<TException>(Expression<Predicate<TException>> expr)
            where TException : Exception
        {
            if (_exception == null)
            {
                throw new Exception("No exception has been thrown!");
            }

            if (_exception.GetType() != typeof (TException))
            {
                throw new Exception($"Exception: {_exception.GetType()} was thrown, expected: {typeof (TException)}");
            }

            if (!expr.Compile()((TException) _exception))
            {
                throw new Exception($"Exception: {expr}");
            }
            _doAssert = false;
        }        

        void IDisposable.Dispose()
        {
            if (_doAssert)
            {
                AssertOrder();
            }
        }
    }

    public class ThenAttribute : FactAttribute
    {
    }
}