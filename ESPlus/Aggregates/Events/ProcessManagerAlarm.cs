using System;

namespace ESPlus.Aggregates
{
    public class ProcessManagerAlarm
    {
        public string CorrolationId { get; }
        public DateTime Alarm { get; }
        public string Token { get; }

        public ProcessManagerAlarm(string corrolationId, DateTime alarm, string token)
        {
            CorrolationId = corrolationId;
            Alarm = alarm;
            Token = token;
        }
    }
}