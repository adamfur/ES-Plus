using System;

namespace ESPlus.Wyrm
{
    public class WyrmException : Exception
    {
        public int Code { get; }

        public WyrmException()
        {
        }

        public WyrmException(int code, string message)
            : base(message)
        {
            Code = code;
        }

        public WyrmException(int code, string message, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
        }
    }
}