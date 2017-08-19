using System;

namespace ESPlus.Timeout
{
    public interface IAlarmHandler
    {
        void SetAlarm(DateTime deadline, string corralationId);
        void CancelAlarm(string corralationId);
        void Start();
        void Stop();
        void Join();
    }
}
