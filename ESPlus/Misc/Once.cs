using System;

namespace ESPlus.Storage
{
    public class Once
    {
        private readonly Action _action;
        private bool _executed = false;

        public Once(Action action)
        {
            this._action = action;
        }

        public void Execute()
        {
            if (_executed)
            {
                return;
            }
            
            _executed = true;
            _action();
        }
    }
}