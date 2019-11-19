using System;

namespace ESPlus.Wyrm
{
    public class WyrmException : Exception
    {
        public int Code { get; }
        public string Info { get; }

        public WyrmException()
        {
        }

        public WyrmException(int code, string message, string info)
            : base(message)
        {
            Code = code;
            Info = info;
        }

        public WyrmException(int code, string message, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
        }
    }
}