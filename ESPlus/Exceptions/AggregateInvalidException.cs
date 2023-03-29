using System;

namespace ESPlus.Exceptions
{
    public class AggregateInvalidException : AggregateException
    {
        public AggregateInvalidException()
        {
        }

        public AggregateInvalidException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AggregateInvalidException(string message)
            : base(message)
        {
        }
    }
}