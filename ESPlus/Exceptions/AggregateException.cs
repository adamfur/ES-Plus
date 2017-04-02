using System;

namespace ESPlus
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

        // protected AggregateException(SerializationInfo info, StreamingContext context)
        //     : base(info, context)
        // {
        // }
    }      
}