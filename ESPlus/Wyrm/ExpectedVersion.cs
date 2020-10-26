namespace ESPlus.Wyrm
{
    public static class ExpectedVersion
    {
        public const long Any = -2;          // This write should not conflict with anything and should always succeed.
        public const long NoStream = -1;     // The stream being written to should not yet exist. If it does exist treat that as a concurrency problem.
        public const long EmptyStream = -1;  // The stream should exist and should be empty. If it does not exist or is not empty treat that as a concurrency problem.
        public const long StreamExists = -4; // The stream should exist. If it or a metadata stream does not exist treat that as a concurrency problem.
    }
}