using System;

namespace ESPlus.Aggregates
{
    public class ProcessManagerAlarm
    {
        public string CorrolationId { get; set; }
        public DateTime Alarm { get; set; }
        public string Token { get; set; }
    }
}