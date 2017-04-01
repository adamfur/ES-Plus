using System.Collections.Generic;
using ESPlus.Storage;
using NSubstitute;
using Xunit;
using System.Linq;
using System;

namespace ESPlus.Tests.Storage
{
    public class ReplayableJournalTests : JournalTests
    {
        protected override IJournaled Create()
        {
            return new ReplayableJournal(_metadataStorage, _stageStorage, _dataStorage);
        }

        [Fact]
        public void Flush_ReplayJournal_MoveFromStageToPersistant()
        {
            // Arrange
            var source = "stage/1/file1";
            var destination = "prod/file1";
            var payload = new object();
            var replayLog = new JournalLog
            {
                Checkpoint = 0L,
                Map = new Dictionary<string, string>
                {
                    [source] = destination
                }
            };

            _metadataStorage.Get(PersistantJournal.JournalPath).Returns(replayLog);
            _stageStorage.Get(source).Returns(_payload);

            // Act
            _journal.Initialize();

            // Assert
            Received.InOrder(() =>
            {
                _stageStorage.Received().Get(Arg.Any<string>());
                _dataStorage.Received().Put(Arg.Any<string>(), Arg.Any<object>());
                _dataStorage.Received().Flush();
            });
        }

        [Fact]
        public void Flush_PutFile_WriteFirstToStageThenJournalThenStorage()
        {
            // Arrange
            var source = "stage/1/file1";
            var destination = "prod/file1";
            var payload = new object();

            _stageStorage.Get(source).Returns(_payload);

            // Act
            _journal.Initialize();
            _journal.Checkpoint = 12;
            _journal.Put(destination, payload);
            _journal.Flush();

            // Assert
            Received.InOrder(() =>
            {
                _stageStorage.Received().Put(Arg.Is<string>(p => p == "Journal/12/prod/file1"), payload);
                _stageStorage.Received().Flush();
                _metadataStorage.Received().Put(PersistantJournal.JournalPath, Arg.Is<JournalLog>(p => true));
                _metadataStorage.Received().Flush();
                _dataStorage.Received().Put(Arg.Is<string>(p => p == "prod/file1"), payload);
                _dataStorage.Received().Flush();
            });
        }

        [Fact]
        public void Flush_PutFile_StagePathAndDestinationPathInJournal()
        {
            // Arrange
            var destination = "prod/file1";
            var payload = new object();

            // Act
            _journal.Initialize();
            _journal.Checkpoint = 12L;
            _journal.Put(destination, payload);
            _journal.Flush();

            // Assert
            _metadataStorage.Received().Put(PersistantJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint == 12L
                && p.Map.Count == 1
                && p.Map.First().Key == "Journal/12/" + destination
                && p.Map.First().Value == destination));
        }

        [Fact]
        public void Get_RealTimeModeNoCache_GetFromStorage()
        {
            var path = "path/1";
            _dataStorage.Get(path).Returns(_payload);

            var item = _journal.Get(path);

            Assert.Equal(_payload, item);
            _dataStorage.Received().Get(path);
        }

        [Fact]
        public void Get_Twice_GetFromCacheSecondTime()
        {
            var path = "path/1";
            _dataStorage.Get(path).Returns(_payload);

            _journal.Get(path);
            _journal.Get(path);

            _dataStorage.Received(1).Get(path);
        }

        [Fact]
        public void Get_PutBefore_ReceiveFromCache()
        {
            var path = "path/1";
            _dataStorage.Get(path).Returns(_payload);

            _journal.Put(path, _payload);
            _journal.Get(path);

            _dataStorage.DidNotReceive().Get(path);
        }

        [Fact]
        public void Get_ReplayMode_UnlessInCacheReturnNewT()
        {
            _metadataStorage.Get(PersistantJournal.JournalPath).Returns(new JournalLog { Checkpoint = 0L });

            var path = "path/1";
            _dataStorage.Get(path).Returns(_payload);

            _journal.Initialize();
            _journal.Get(path);

            _dataStorage.DidNotReceive().Get(path);
        }

        [Fact]
        public void Flush_DontKeepMapFromPreviousCall_CleanSlate()
        {
            var path1 = "path/1";
            var path2 = "path/2";
            
            _journal.Checkpoint = 12L;
            _journal.Put(path1, _payload);
            _journal.Flush();
            _journal.Checkpoint = 13L;
            _journal.Put(path2, _payload);
            _journal.Flush();

            _metadataStorage.Received().Put(PersistantJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint == 12L && p.Map.Count == 1));
            _metadataStorage.Received().Put(PersistantJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint == 13L && p.Map.Count == 1));
        }

        [Fact]
        public void Flush_WipeWriteCacheBetweenWrites_CleanSlate1()
        {
            var path1 = "path/1";
            var path2 = "path/2";
            
            _journal.Checkpoint = 12L;
            _journal.Put(path1, _payload);
            _journal.Flush();
            _journal.Checkpoint = 13L;
            _journal.Put(path2, _payload);
            _journal.Flush();

            _dataStorage.Received(1).Put(path1, Arg.Any<object>());
            _dataStorage.Received(1).Put(path2, Arg.Any<object>());
        }

        [Fact]
        public void Flush_WipeStageCacheBetweenWrites_CleanSlate2()
        {
            var path1 = "path/1";
            var path2 = "path/2";
            
            _journal.Checkpoint = 12L;
            _journal.Put(path1, _payload);
            _journal.Flush();
            _journal.Checkpoint = 13L;
            _journal.Put(path2, _payload);
            _journal.Flush();

            _stageStorage.Received(1).Put("Journal/12/" + path1, Arg.Any<object>());
            _stageStorage.Received(1).Put("Journal/13/" + path2, Arg.Any<object>());
        } 

        [Fact]
        public void Flush_NoChange_NoFlush()
        {
            _journal.Flush();

            _metadataStorage.DidNotReceive().Flush();
        }         

        [Fact]
        public void Flush_NoChange_NoFlush2()
        {
            var path = "path/1";
            
            _journal.Checkpoint = 12L;
            _journal.Put(path, _payload);            
            _journal.Flush();
            _journal.Flush();

            _metadataStorage.Received(1).Flush();
        }  



        /********************************/
        [Fact]
        public void TEMPTEMPTEMP()
        {
            var meta = new FileSystemStorage("meta");
            var stage = new FileSystemStorage("stage");
            var storage = new FileSystemStorage("storage");
            var journal = new ReplayableJournal(meta, stage, storage);
            var path1 = "path/1";
            
            journal.Checkpoint = 13L;
            journal.Put(path1, _payload);
            journal.Flush();
        }                     
    }
}