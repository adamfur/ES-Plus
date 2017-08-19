using System;

namespace ESPlus.Timeout
{
    public class Alarm
    {
        public string CorrolationId { get; set; }
        public DateTime Deadline { get; set; }
        public string Type { get; set; }
        public string Event { get; set; }
        public static Alarm Null = new Alarm();
    }
}
