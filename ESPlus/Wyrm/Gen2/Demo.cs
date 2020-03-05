using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public class Demo
    {
        public async Task Test()
        {
            var driver = default(IWyrmDriverExp);

            await foreach (var item in driver.ReadFrom(Position.Begin)
                .CreateEventFilter(new List<Type> {typeof(Bar)})
                .EventFilter(new List<Type> {typeof(Bar)})
                .StreamNameFilter(".*")
                .Skip(20)
                .Take(20)
                .ReadDirection(Direction.Backwards)
                .Subscribe()
                .GroupByStream()
                .QueryEventsAsync())
            {
                // nothing
                // item.Accept();
            }
        }
    }
    
    public class Bar
    {
    }
}