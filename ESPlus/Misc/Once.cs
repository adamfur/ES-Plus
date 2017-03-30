using System;

namespace ESPlus.Storage
{
    public class Once
    {
        private readonly Action action;
        private bool _executed = false;

        public Once(Action action)
        {
            this.action = action;
        }

        public void Execute()
        {
            if (_executed)
            {
                return;
            }
            _executed = true;
            action();
        }
    }
}