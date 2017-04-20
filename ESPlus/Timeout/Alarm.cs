using System;
using System.Threading;

namespace ESPlus.Timeout
{
    // waiting for netstandard 2.0
    public interface IAlarm
    {
        void SetAlarm(DateTime deadline, string corralationId);
        void CancelAlarm(string corralationId);
    }

    public class Alarm : IAlarm
    {
        private CancellationTokenSource _cancelationToken = new CancellationTokenSource();
        private Thread _workerThread;

        public Alarm()
        {
            _workerThread = new Thread(() => WorkerThread());
            _workerThread.Start();
        }

        public void CancelAlarm(string corralationId)
        {
            //_workerThread.Interrupt();
        }

        public void SetAlarm(DateTime deadline, string corralationId)
        {
            //_workerThread.Interrupt();
        }

        private void WorkerThread()
        {
            while (!_cancelationToken.IsCancellationRequested)
            {
                try
                {
                    var alarm = GetNextOccuringAlarm();

                    try
                    {
                        Thread.Sleep(1);
                        // remove alarm
                    }
                    catch (ThreadInterruptedException)
                    {
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        private object GetNextOccuringAlarm()
        {
            throw new NotImplementedException();
        }
    }
}
