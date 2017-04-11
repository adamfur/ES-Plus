using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public static class PositionExtentions
    {
        public static Position ToPosition(this long value)
        {
            return new Position(value, 0);
        }
    }
}