using System.Collections.Generic;
using System.Linq;

namespace ESPlus.Timeout
{
    public class AlarmRepository : IAlarmRepository
    {
        public List<Alarm> _alarms = new List<Alarm>();

        public void Put(Alarm alarm)
        {
            _alarms.Add(alarm);
            _alarms.Sort((x, y) => x.Deadline.CompareTo(y.Deadline));
        }

        public void Remove(string corrolationId)
        {
            _alarms.RemoveAll(x => x.CorrolationId == corrolationId);
        }

        public Alarm Top()
        {
            return _alarms.FirstOrDefault() ?? Alarm.Null;
        }
    }
}
