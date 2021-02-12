using System;
using System.Runtime.Serialization;

namespace ESPlus.MoonGoose
{
    public class MoonGooseException : Exception
    {
        public MoonGooseException()
        {
        }

        protected MoonGooseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public MoonGooseException(string message)
            : base(message)
        {
        }

        public MoonGooseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}