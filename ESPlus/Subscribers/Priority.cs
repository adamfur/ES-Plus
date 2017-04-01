namespace ESPlus.Subscribers
{
    public enum Priority : long
    {
        RealTime = -1,
        High = 100,
        Normal = 10,
        Low = 1,
        Idle = 0
    }
    }
