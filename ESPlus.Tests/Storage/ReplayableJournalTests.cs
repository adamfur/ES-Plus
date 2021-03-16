using System;
using System.Collections.Generic;
using ESPlus.Storage;
using NSubstitute;
using Xunit;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;
using ESPlus.Subscribers;

namespace ESPlus.Tests.Storage
{
    public class ReplayableJournalTests : JournalTests
    {
        protected override IJournaled Create()
        {
            return new ReplayableJournal(_metadataStorage, _stageStorage, _dataStorage);
        }

        [Fact]
        public async Task Flush_ReplayJournal_MoveFromStageToPersistant()
        {
            // Arrange
            var replayLog = new JournalLog
            {
                Checkpoint = Position.Start,
                Map = new Dictionary<StringPair, string>
                {
                    [new StringPair(null, "stage/1/file1")] = "prod/file1"
                }
            };

            _metadataStorage.GetAsync<JournalLog>("master", PersistentJournal.JournalPath, CancellationToken.None).Returns(replayLog);
            _stageStorage.GetAsync<object>(null, "stage/1/file1", CancellationToken.None).Returns(_payload);

            // Act
            await _journal.InitializeAsync();

            // Assert
            Received.InOrder(() =>
            {
                //_stageStorage.Received().Get<object>(Arg.Any<string>());
                _dataStorage.Received().Put(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
                _dataStorage.Received().FlushAsync(CancellationToken.None);
            });
        }

        [Fact]
        public async Task Flush_PutFile_WriteFirstToStageThenJournalThenStorage()
        {
            // Arrange
            var source = "stage/1/file1";
            var destination = "prod/file1";
            var payload = new object();

            _stageStorage.GetAsync<object>(null, source, CancellationToken.None).Returns(_payload);

            // Act
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(null, destination, payload);
            await _journal.FlushAsync(CancellationToken.None);

            // Assert
            Received.InOrder(() =>
            {
                _dataStorage.Received().FlushAsync(CancellationToken.None);
                _stageStorage.Received().Put(Arg.Any<string>(), Arg.Is<string>(p => p == "stage/prod/file1"), payload);
                _stageStorage.Received().FlushAsync(CancellationToken.None);
                _metadataStorage.Received().Put("master", PersistentJournal.JournalPath, Arg.Is<JournalLog>(p => true));
                _metadataStorage.Received().FlushAsync(CancellationToken.None);
                _dataStorage.Received().Put(Arg.Any<string>(), Arg.Is<string>(p => p == "prod/file1"), payload);
                _dataStorage.Received().FlushAsync(CancellationToken.None);
            });
        }

        [Fact]
        public async Task Flush_PutFile_StagePathAndDestinationPathInJournal()
        {
            // Arrange
            var destination = "prod/file1";
            var payload = new object();

            // Act
            await _journal.InitializeAsync();
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(null, destination, payload);
            await _journal.FlushAsync(CancellationToken.None);

            // Assert
            _metadataStorage.Received().Put("master", PersistentJournal.JournalPath, Arg.Is<JournalLog>(p =>
                p.Checkpoint.Equals(Position.Gen(12))
                && p.Map.Count == 1
                && p.Map.First().Key.Equals(new StringPair(null, "prod/file1"))
                && p.Map.First().Value == destination
            ));
        }

        [Fact]
        public async Task Get_RealTimeModeNoCache_GetFromStorage()
        {
            var path = "path/1";
            _dataStorage.GetAsync<object>(null, path, CancellationToken.None).Returns(_payload);

            var item = await _journal.GetAsync<object>(null, path, CancellationToken.None);

            Assert.Equal(_payload, item);
            await _dataStorage.Received().GetAsync<object>(null, path, CancellationToken.None);
        }

        // [Fact]
        // public async Task Get_Twice_GetFromCacheSecondTime()
        // {
        //     var path = "path/1";
        //     _dataStorage.GetAsync<object>(path, null).Returns(_payload);
        //
        //     await _journal.GetAsync<object>(path, null);
        //     await _journal.GetAsync<object>(path, null);
        //
        //     await _dataStorage.Received(1).GetAsync<object>(path, null);
        // }

        [Fact]
        public void Get_PutBefore_ReceiveFromCache()
        {
            var path = "path/1";
            _dataStorage.GetAsync<object>(null, path, CancellationToken.None).Returns(_payload);

            _journal.Put(null, path, _payload);
            _journal.GetAsync<object>(null, path, CancellationToken.None);

            _dataStorage.DidNotReceive().GetAsync<object>(null, path, CancellationToken.None);
        }

//        [Fact]
//        public void Get_ReplayMode_UnlessInCacheReturnNewT()
//        {
//            var path = "path/1";
//
//            _metadataStorage.Get<object>(PersistentJournal.JournalPath).Returns(new JournalLog { Checkpoint = Position.Start });
//            _dataStorage.Get<object>(path).Returns(_payload);
//
//            _journal.Initialize();
//            _journal.Get<object>(path);
//
//            Assert.Equal(SubscriptionMode.Replay, _journal.SubscriptionMode);
//            _dataStorage.DidNotReceive().Get<object>(path);
//        }

        [Fact]
        public async Task Flush_DontKeepMapFromPreviousCall_CleanSlate()
        {
            var path1 = "path/1";
            var path2 = "path/2";
            
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(null, path1, _payload);
            await _journal.FlushAsync(CancellationToken.None);
            _journal.Checkpoint = Position.Gen(13);
            _journal.Put(null, path2, _payload);
            await _journal.FlushAsync(CancellationToken.None);

            _metadataStorage.Received().Put("master", PersistentJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint.Equals(Position.Gen(12)) && p.Map.Count == 1));
            _metadataStorage.Received().Put("master", PersistentJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint.Equals(Position.Gen(13)) && p.Map.Count == 1));
        }

        [Fact]
        public async Task Flush_WipeWriteCacheBetweenWrites_CleanSlate1()
        {
            var path1 = "path/1";
            var path2 = "path/2";
            
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(null, path1, _payload);
            await _journal.FlushAsync(CancellationToken.None);
            _journal.Checkpoint = Position.Gen(13);
            _journal.Put(null, path2, _payload);
            await _journal.FlushAsync(CancellationToken.None);

            _dataStorage.Received(1).Put(null, path1, Arg.Any<object>());
            _dataStorage.Received(1).Put(null, path2, Arg.Any<object>());
        }

        [Fact]
        public async Task Flush_WipeStageCacheBetweenWrites_CleanSlate2()
        {
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(null, "path/1", _payload);
            await _journal.FlushAsync(CancellationToken.None);
            _journal.Checkpoint = Position.Gen(13);
            _journal.Put(null, "path/2", _payload);
            await _journal.FlushAsync(CancellationToken.None);

            _stageStorage.Received(1).Put(null, "stage/path/1", Arg.Any<object>());
            _stageStorage.Received(1).Put(null, "stage/path/2", Arg.Any<object>());
        } 

        [Fact]
        public async Task Flush_NoChange_NoFlush()
        {
            await _journal.FlushAsync(CancellationToken.None);

            await _metadataStorage.DidNotReceive().FlushAsync(CancellationToken.None);
        }         

        [Fact]
        public async Task Flush_NoChange_NoFlush2()
        {
            var path = "path/1";
            
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(null, path, _payload);            
            await _journal.FlushAsync(CancellationToken.None);
            await _journal.FlushAsync(CancellationToken.None);

            await _metadataStorage.Received(1).FlushAsync(CancellationToken.None);
        }  

        // public void TEMPTEMPTEMP()
        // {
        //     var meta = new FileSystemStorage("meta");
        //     var stage = new FileSystemStorage("stage");
        //     var storage = new FileSystemStorage("storage");
        //     var journal = new ReplayableJournal(meta, stage, storage);
        //     var path1 = "path/1";
            
        //     journal.Checkpoint = 13L.ToPosition();
        //     journal.Put(path1, _payload);
        //     journal.Flush();
        // }                     
    }
}