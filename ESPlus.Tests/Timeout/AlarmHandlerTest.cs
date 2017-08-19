using System;
using ESPlus.Timeout;
using NSubstitute;
using Xunit;

namespace ESPlus.Tests.Timeout
{
    public class AlarmHandlerTest
    {
        private IAlarmHandler _handler;
        private IAlarmRepository _repository;
        private DateTime _deadline;
        private string _corrolationId;

        public AlarmHandlerTest()
        {
            _repository = Substitute.For<IAlarmRepository>();
            _handler = new AlarmHandler(_repository);
            _deadline = DateTime.Now;
            _corrolationId = Guid.NewGuid().ToString();
        }

        [Fact]
        public void Fact1()
        {
            _handler.Start();
            _handler.Stop();
            _handler.Join();
        }

        [Fact]
        public void SetAlarm_IsAddToRepository_Yes()
        {
            _handler.SetAlarm(_deadline, _corrolationId);
            _repository.Received().Put(Arg.Is<Alarm>(a => a.Deadline == _deadline && a.CorrolationId == _corrolationId));
        }

        [Fact]
        public void CancelAlarm_IsRemovedFromRepository_Yes()
        {
            _handler.CancelAlarm(_corrolationId);
            _repository.Received().Remove(Arg.Is<string>(a => a == _corrolationId));
        }
    }
}