namespace ESPlus.Wyrm
{
    public static class OperationType
    {
        public static byte READ_STREAM_FORWARD = 1;
        public static byte READ_STREAM_BACKWARD = 2;
        public static byte READ_ALL_FORWARD = 3;
        public static byte READ_ALL_BACKWARD = 4;
        public static byte SUBSCRIBE = 5;
        public static byte PUT = 6;
        public static byte DELETE = 7;
        public static byte LIST_STREAMS = 8;
        public static byte PULL = 9;
        public static byte READ_ALL_STREAMS_FORWARD = 10;
        public static byte FLOOD = (byte)'a';
    }
}