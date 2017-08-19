namespace ESPlus.Timeout
{
    public interface IAlarmRepository
    {
        void Put(Alarm alarm);
        void Remove(string corrolationId);
        Alarm Top();
    }
}
