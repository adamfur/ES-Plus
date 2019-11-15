namespace ESPlus.Wyrm
{
    public enum Commands
    {
        Protocol = 0,
        Ping = 1,
        Close = 3,
        Put = 4,
        AuthenticateJwt = 5,
        AuthenticateApiKey = 6,
        EventFilter = 7,
        ReadAllForward = 8,
        ReadAllBackward = 9,
        ReadStreamForward = 10,
        ReadStreamBackward = 11,
        ReadAllForwardFollow = 12,
        ReadStreamForwardFollow = 13,
        Pull = 14,
        PullFollow = 15,
        Reset = 16,
        ListStreams = 17,
        ReadAllForwardGroupByStream = 18,
        Checkpoint = 19,
        CreateFilter = 20,
    }
}