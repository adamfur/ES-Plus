using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Wyrm;
using Xunit;

namespace ESPlus.Tests
{
    public class WyrmDriverTests
    {
        private WyrmDriver _wyrmDriver;
        private string _id;

        public WyrmDriverTests()
        {
            _wyrmDriver = new WyrmDriver("192.168.1.2:8888", new EventJsonSerializer());
            _id = Guid.NewGuid().ToString();
        }

        [Fact]
        public void CreateNewStream()
        {
            _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new CreateBundleItem
                    {
                        StreamName = _id
                    }
                }
            });
        }

        [Fact]
        public async Task DeleteNonExistingStream()
        {
            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new DeleteBundleItem
                    {
                        StreamName = _id
                    }
                }
            });
        }

        [Fact]
        public async Task DeleteExistingStream()
        {
            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new CreateBundleItem
                    {
                        StreamName = _id
                    }
                }
            });

            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new DeleteBundleItem
                    {
                        StreamName = _id
                    }
                }
            });
        }

        [Fact]
        public async Task AppendNewStream()
        {
            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new EventsBundleItem
                    {
                        StreamName = _id,
                        StreamVersion = 0,
                        Events = new List<BundleEvent>
                        {
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = Guid.NewGuid().ToString(),
                                Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                            }
                        }
                    }
                }
            });
        }

        [Fact]
        public async Task AppendExistingStream()
        {
            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new EventsBundleItem
                    {
                        StreamName = _id,
                        StreamVersion = 0,
                        Events = new List<BundleEvent>
                        {
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = Guid.NewGuid().ToString(),
                                Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                            }
                        }
                    }
                }
            });

            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new EventsBundleItem
                    {
                        StreamName = _id,
                        StreamVersion = 1,
                        Events = new List<BundleEvent>
                        {
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = Guid.NewGuid().ToString(),
                                Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                            },
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = Guid.NewGuid().ToString(),
                                Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                            }
                        }
                    }
                }
            });
        }

        [Fact]
        public async Task AppendExistingStream_OneTransaction()
        {
            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new EventsBundleItem
                    {
                        StreamName = _id,
                        StreamVersion = 0,
                        Events = new List<BundleEvent>
                        {
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = Guid.NewGuid().ToString(),
                                Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                            }
                        }
                    },
                    new EventsBundleItem
                    {
                        StreamName = _id,
                        StreamVersion = 1,
                        Events = new List<BundleEvent>
                        {
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = Guid.NewGuid().ToString(),
                                Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                            },
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = Guid.NewGuid().ToString(),
                                Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                            }
                        }
                    }
                }
            });
        }

        [Fact]
        public async Task ReadStreamForward()
        {
            var result = await Append();

            Assert.Equal(3, _wyrmDriver.ReadStreamForward(_id).Count());
        }

        private Task<Position> Append()
        {
            return _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new EventsBundleItem
                    {
                        StreamName = _id,
                        StreamVersion = 0,
                        Events = new List<BundleEvent>
                        {
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = $"EventType: [{Guid.NewGuid()}]",
                                Metadata = Encoding.UTF8.GetBytes($"Metadata: [{Guid.NewGuid()}]"),
                                Body = Encoding.UTF8.GetBytes($"Body: [{Guid.NewGuid()}]"),
                            }
                        }
                    },
                    new EventsBundleItem
                    {
                        StreamName = _id,
                        StreamVersion = 1,
                        Events = new List<BundleEvent>
                        {
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = $"EventType: [{Guid.NewGuid()}]",
                                Metadata = Encoding.UTF8.GetBytes($"Metadata: [{Guid.NewGuid()}]"),
                                Body = Encoding.UTF8.GetBytes($"Body: [{Guid.NewGuid()}]"),
                            },
                            new BundleEvent
                            {
                                EventId = Guid.NewGuid(),
                                EventType = $"EventType: [{Guid.NewGuid()}]",
                                Metadata = Encoding.UTF8.GetBytes($"Metadata: [{Guid.NewGuid()}]"),
                                Body = Encoding.UTF8.GetBytes($"Body: [{Guid.NewGuid()}]"),
                            }
                        }
                    }
                }
            });
        }

        [Fact]
        public async Task ReadStreamBackward()
        {
            var result = await Append();

            Assert.Equal(3, _wyrmDriver.ReadStreamBackward(_id).Count());
        }

        [Fact]
        public async Task ReadAllForward()
        {
            var result = await Append();

            Assert.True(_wyrmDriver.ReadAllForward(Position.Start).Any());
        }

        [Fact]
        public async Task ReadAllBackward()
        {
            var result = await Append();

            Assert.True(_wyrmDriver.ReadAllBackward(Position.End).Any());
        }

        [Fact]
        public async Task TestException2()
        {
            var exception = await Assert.ThrowsAsync<Exception>(() => _wyrmDriver.CreateStreamAsync(""));

//            Assert.Equal("hello world", exception.Message);
        }

//        [Fact]
        public void Food()
        {
            Thread thread = null;

            for (var i = 0; i < 8; ++i)
            {
                thread = new Thread(() =>
                {
                    while (true)
                    {
                        var id = Guid.NewGuid().ToString();

                        _wyrmDriver.Append(new Bundle
                        {
                            Policy = CommitPolicy.All,
                            Items = new List<BundleItem>
                            {
                                new EventsBundleItem
                                {
                                    StreamName = id,
                                    StreamVersion = 0,
                                    Events = new List<BundleEvent>
                                    {
                                        new BundleEvent
                                        {
                                            EventId = Guid.NewGuid(),
                                            EventType = Guid.NewGuid().ToString(),
                                            Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                            Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                        }
                                    }
                                },
                                new EventsBundleItem
                                {
                                    StreamName = id,
                                    StreamVersion = 1,
                                    Events = new List<BundleEvent>
                                    {
                                        new BundleEvent
                                        {
                                            EventId = Guid.NewGuid(),
                                            EventType = Guid.NewGuid().ToString(),
                                            Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                            Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                        },
                                        new BundleEvent
                                        {
                                            EventId = Guid.NewGuid(),
                                            EventType = Guid.NewGuid().ToString(),
                                            Metadata = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                            Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                                        }
                                    }
                                }
                            }
                        });
                    }
                });

                thread.Start();
            }

            thread.Join();
        }

        [Fact]
        public async Task EnumerateStreams()
        {
            var result = await Append();
            
            var sum = _wyrmDriver.EnumerateStreams().ToList();
            var x = 13;
        }
        
        [Fact]
        public async Task ReadAllGroupByStream()
        {
            var result = await Append();
            
            var sum = _wyrmDriver.EnumerateAllGroupByStream().ToList();
            var x = 13;
        }

//        [Fact]
        public async Task AppendDeterministicStream()
        {
            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new EventsBundleItem
                    {
                        StreamName = "john",
                        StreamVersion = 0,
                        Events = new List<BundleEvent>
                        {
                            new BundleEvent
                            {
                                EventId = Guid.Empty,
                                EventType = "EventType",
                                Metadata = Encoding.UTF8.GetBytes("Metadata"),
                                Body = Encoding.UTF8.GetBytes("Body"),
                            }
                        }
                    }
                }
            });

            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new EventsBundleItem
                    {
                        StreamName = "john",
                        StreamVersion = 1,
                        Events = new List<BundleEvent>
                        {
                            new BundleEvent
                            {
                                EventId = Guid.Empty,
                                EventType = "EventType",
                                Metadata = Encoding.UTF8.GetBytes("Metadata"),
                                Body = Encoding.UTF8.GetBytes("Body"),
                            },
                            new BundleEvent
                            {
                                EventId = Guid.Empty,
                                EventType = "EventType",
                                Metadata = Encoding.UTF8.GetBytes("Metadata"),
                                Body = Encoding.UTF8.GetBytes("Body"),
                            }
                        }
                    }
                }
            });
        }
    }
}