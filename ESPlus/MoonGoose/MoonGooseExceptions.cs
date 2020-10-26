using System;
using System.Runtime.Serialization;

namespace ESPlus.MoonGoose
{
    public class MoonGooseExceptions : Exception
    {
        public MoonGooseExceptions()
        {
        }

        protected MoonGooseExceptions(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public MoonGooseExceptions(string message)
            : base(message)
        {
        }

        public MoonGooseExceptions(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}