using System;

namespace ESPlus.Timeout
{
    public interface IAlarm
    {
        void SetAlarm(DateTime deadline, string corralationId);
    }

    public class Alarm : IAlarm
    {
        public void SetAlarm(DateTime deadline, string corralationId)
        {
        }
    }
}
