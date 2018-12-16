namespace ESPlus.Wyrm
{
    public static class OperationType
    {
        public static byte READ_STREAM_FORWARD = 0x01;
        public static byte READ_STREAM_BACKWARD = 0x02;
        public static byte READ_ALL_FORWARD = 0x03;
        public static byte READ_ALL_BACKWARD = 0x04;
        public static byte SUBSCRIBE = 0x05;
        public static byte PUT = 0x06;
        public static byte DELETE = 0x07;
        public static byte LIST_STREAMS = 0x08;
        public static byte FLOOD = (byte)'a';
    }
}