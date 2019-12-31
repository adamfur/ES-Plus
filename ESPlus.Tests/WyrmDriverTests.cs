using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Misc;
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
            _wyrmDriver = new WyrmDriver("192.168.1.2:9999", new EventJsonSerializer(new EventTypeResolver()), "api-key");
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
        public async Task Beaver()
        {
            await _wyrmDriver.Append(new Bundle
            {
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new EventsBundleItem
                    {
                        StreamName = "beaver",
                        StreamVersion = -2,
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

        private Task<WyrmResult> Append()
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
            _wyrmDriver.Reset();
            var result = await Append();

            Assert.NotEmpty(_wyrmDriver.ReadAllForward(Position.Start).ToList());
        }

        [Fact]
        public async Task ReadAllBackward()
        {
            var result = await Append();

            Assert.NotEmpty(_wyrmDriver.ReadAllBackward(Position.End).ToList());
        }

        [Fact]
        public async Task SuscribeAll()
        {
            await Append();
            await DeleteExistingStream();
//            var result = await Append();

            var list = _wyrmDriver.SubscribeAll(Position.Start).ToList();
            int x = 13;
        }

        [Fact]
        public async Task TestException2()
        {
            var exception = await Assert.ThrowsAsync<WyrmException>(() => _wyrmDriver.CreateStreamAsync(""));
            var x = 12;
//            Assert.Equal("hello world", exception.Message);
        }

        [Fact]
        public async Task Ping()
        {
            var result = _wyrmDriver.Ping();
            var result2 = _wyrmDriver.Ping();
            var x = 13;
        }

        [Fact]
        public async Task Feed()
        {
            for (var i = 0; i < 10||false; ++i)
            {
                var id = Guid.NewGuid().ToString();

                await _wyrmDriver.Append(new Bundle
                {
                    Policy = CommitPolicy.All,
                    Items = new List<BundleItem>
                    {
                        new EventsBundleItem
                        {
                            StreamName = id,
                            StreamVersion = 0,
                            Encrypt = true,
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
                            Encrypt = false,
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
        }

//        [Fact]
        public void Feed2()
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
                            }
                        }).Wait();
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

        [Fact]
        public async Task Reset()
        {
            var result = await Append();
            _wyrmDriver.Reset();

            Assert.Empty(_wyrmDriver.ReadAllForward(Position.Start));
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

        [Fact]
        public async Task Adam()
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
                                EventType = $"EventType: [{Guid.NewGuid()}]",
                                Metadata = Encoding.UTF8.GetBytes($"Metadata: [{Guid.NewGuid()}]"),
                                Body = Encoding.UTF8.GetBytes($"Body: [{Guid.NewGuid()}]"),
                            }
                        }
                    },
                }
            });

            Assert.NotEmpty(_wyrmDriver.ReadAllBackward(Position.End).ToList());
        }
    }
}