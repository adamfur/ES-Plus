using System;
using System.Runtime.Serialization;

namespace ESPlus.MoonGoose
{
    public class MoonGooseConcurrencyException : MoonGooseException 
    {
        public MoonGooseConcurrencyException()
        {
        }

        protected MoonGooseConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public MoonGooseConcurrencyException(string message)
            : base(message)
        {
        }

        public MoonGooseConcurrencyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}