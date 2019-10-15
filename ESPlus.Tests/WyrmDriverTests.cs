using System;
using System.Collections.Generic;
using System.Text;
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
        public void DeleteNonExistingStream()
        {
            _wyrmDriver.Append(new Bundle
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
        public void DeleteExistingStream()
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

            _wyrmDriver.Append(new Bundle
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
        public void AppendNewStream()
        {
            _wyrmDriver.Append(new Bundle
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
        public void AppendExistingStream()
        {
            _wyrmDriver.Append(new Bundle
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

            _wyrmDriver.Append(new Bundle
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
        public void AppendExistingStream_OneTransaction()
        {
            _wyrmDriver.Append(new Bundle
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
    }
}