using System;

namespace ESPlus.Exceptions
{
    public class AggregateException : Exception
    {
        public AggregateException()
        {
        }

        public AggregateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AggregateException(string message)
            : base(message)
        {
        }
    }      
}