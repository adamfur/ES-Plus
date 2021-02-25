using System;
using System.Runtime.Serialization;

namespace ESPlus.MoonGoose
{
    public class MoonGooseNotFoundException : MoonGooseException 
    {
        public MoonGooseNotFoundException()
        {
        }

        protected MoonGooseNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public MoonGooseNotFoundException(string message)
            : base(message)
        {
        }

        public MoonGooseNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}