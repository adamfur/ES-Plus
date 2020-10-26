using System;
using System.Threading;
using ESPlus.Misc;

namespace ESPlus.Timeout
{
    public class AlarmHandler : IAlarmHandler
    {
        private CancellationTokenSource _cancelationToken = new CancellationTokenSource();
        private Thread _workerThread;
        private object _mutex = new object();
        private Alarm _waitingOn = Alarm.Null;
        private readonly IAlarmRepository _repository;

        public AlarmHandler(IAlarmRepository repository)
        {
            this._repository = repository;
            _workerThread = new Thread(() => WorkerThread());
        }

        public void Start()
        {
            _workerThread.Start();
        }

        public void Stop()
        {
            _cancelationToken.Cancel();
        }

        public void CancelAlarm(string corralationId)
        {
            lock (_mutex)
            {
                _repository.Remove(corralationId);
                if (_waitingOn.CorrolationId == corralationId)
                {
                    Monitor.Pulse(_mutex);
                }
            }
        }

        public void SetAlarm(DateTime deadline, string corralationId)
        {
            lock (_mutex)
            {
                _repository.Put(new Alarm
                {
                    Deadline = deadline,
                    CorrolationId = corralationId
                });
                
                if (deadline <= _waitingOn.Deadline)
                {
                    Monitor.Pulse(_mutex);
                }
            }
        }

        private void WorkerThread()
        {
            while (!_cancelationToken.IsCancellationRequested)
            {
                try
                {
                    lock (_mutex)
                    {
                        _waitingOn = _repository.Top();

                        if (_waitingOn == Alarm.Null)
                        {
                            while (_waitingOn == Alarm.Null && !_cancelationToken.IsCancellationRequested)
                            {
                                Monitor.Wait(_mutex);
                            }
                        }

                        if (!_cancelationToken.IsCancellationRequested)
                        {
                            Monitor.Wait(_mutex, _waitingOn.Deadline - SystemTime.UtcNow);
                            CancelAlarm(_waitingOn.CorrolationId);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void Join()
        {
            _workerThread.Join();
        }
    }
}
