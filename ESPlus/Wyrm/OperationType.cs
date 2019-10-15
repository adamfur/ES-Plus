namespace ESPlus.Wyrm
{
    public static class OperationType
    {
        public static int READ_STREAM_FORWARD = 1 + 1000;
        public static int READ_STREAM_BACKWARD = 2 + 1000;
        public static int READ_ALL_FORWARD = 3 + 1000;
        public static int READ_ALL_BACKWARD = 4 + 1000;
        public static int SUBSCRIBE = 5 + 1000;
        public static int DELETE = 7 + 1000;
        public static int LIST_STREAMS = 8 + 1000;
        public static int PULL = 9 + 1000;
        public static int READ_ALL_STREAMS_FORWARD = 10 + 1000;
        public static int FLOOD = 'a' + 1000;
        public static int LAST_CHECKPOINT = 11 + 1000;

        // WYRM2
        public static int PUT = 4;
    }
}