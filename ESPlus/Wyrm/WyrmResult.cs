namespace ESPlus.Wyrm
{
    public class WyrmResult
    {
        public Position Position { get; }
        public long Offset { get; }

        public WyrmResult(Position position, long offset)
        {
            Position = position;
            Offset = offset;
        }
    }
}