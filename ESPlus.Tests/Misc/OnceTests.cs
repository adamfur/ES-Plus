using ESPlus.Storage;
using Xunit;

namespace ESPlus.Tests.Misc
{
    public class OnceTests
    {
        private int _counter;
        private Once _once;

        public OnceTests()
        {
            _counter = 0;
            _once = new Once(() => ++_counter);
        }

        [Fact]
        public void Execute_Once_CounterIsIncreasedToOne()
        {
            _once.Execute();
        }

        [Fact]
        public void Execute_Twice_CounterIsIncreasedToOne()
        {
            _once.Execute();
            _once.Execute();
        }        
    }
}