using System;
using System.Collections.Generic;
using System.Linq;
using ESPlus.Subscribers;
using Xunit;

namespace ESPlus.Tests.Subscribers
{
    // public class EventFetcherCacheRowTests
    // {
    //     private EventFetcherCacheRow _row;

    //     public EventFetcherCacheRowTests()
    //     {
    //         _row = new EventFetcherCacheRow(2L.ToPosition(), 4L.ToPosition(), CreateEvents(2, 4));
    //     }

    //     private EventStream CreateEvents(long from, long to)
    //     {
    //         var result = new List<Event>();

    //         for (var i = from; i <= to; ++i)
    //         {
    //             result.Add(new Event(null) { Position = i.ToPosition() });
    //         }

    //         return new EventStream
    //         {
    //             Events = result
    //         };
    //     }

    //     [Theory]
    //     [InlineData(2, false)]
    //     [InlineData(3, true)]
    //     [InlineData(4, true)]
    //     [InlineData(6, true)]
    //     [InlineData(7, false)]
    //     [InlineData(8, false)]
    //     public void Within_Theory_AsExcepted(long digit, bool inRange)
    //     {
    //         var row = new EventFetcherCacheRow(3L.ToPosition(), 7L.ToPosition(), CreateEvents(3, 7));

    //         var result = row.Within(digit.ToPosition());

    //         Assert.Equal(inRange, result);
    //     }

    //     [Fact]
    //     public void Select_Theory_As_Expected()
    //     {
    //         var result = _row.Select(3L.ToPosition()).Events.ToList();

    //         Assert.Equal(3L.ToPosition(), result[0].Position);
    //         Assert.Equal(4L.ToPosition(), result[1].Position);
    //     }

    //     // [Fact]
    //     // public void Merge_adasd_asdasd()
    //     // {
    //     //     _row.Merge(CreateEvents(3, 5));

    //     //     Assert.Equal(4, _row.Select(0).Count());
    //     // }
    // }
}