using System.Collections.Generic;
using ESPlus.Storage;
using NSubstitute;
using Xunit;
using System.Linq;
using ESPlus.EventHandlers;
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
        public void Flush_ReplayJournal_MoveFromStageToPersistant()
        {
            // Arrange
            var replayLog = new JournalLog
            {
                Checkpoint = Position.Start,
                Map = new Dictionary<string, string>
                {
                    ["stage/1/file1"] = "prod/file1"
                }
            };

            _metadataStorage.Get<JournalLog>(PersistentJournal.JournalPath).Returns(replayLog);
            _stageStorage.Get<HasObjectId>("stage/1/file1").Returns(_payload);

            // Act
            _journal.Initialize();

            // Assert
            Received.InOrder(() =>
            {
                //_stageStorage.Received().Get<object>(Arg.Any<string>());
                _dataStorage.Received().Put(Arg.Any<string>(), Arg.Any<HasObjectId>());
                _dataStorage.Received().Flush();
            });
        }

        [Fact]
        public void Flush_PutFile_WriteFirstToStageThenJournalThenStorage()
        {
            // Arrange
            var source = "stage/1/file1";
            var destination = "prod/file1";
            var payload = new HasObjectId();

            _stageStorage.Get<HasObjectId>(source).Returns(_payload);

            // Act
            _journal.Initialize();
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(destination, payload);
            _journal.Flush();

            // Assert
            Received.InOrder(() =>
            {
                _dataStorage.Received().Flush();
                _stageStorage.Received().Put(Arg.Is<string>(p => p == "stage/prod/file1"), payload);
                _stageStorage.Received().Flush();
                _metadataStorage.Received().Put(PersistentJournal.JournalPath, Arg.Is<JournalLog>(p => true));
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
            var payload = new HasObjectId();

            // Act
            _journal.Initialize();
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(destination, payload);
            _journal.Flush();

            // Assert
            _metadataStorage.Received().Put(PersistentJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint.Equals(Position.Gen(12))
               && p.Map.Count == 1
               && p.Map.First().Key == "prod/file1"
               && p.Map.First().Value == destination));
        }

        [Fact]
        public void Get_RealTimeModeNoCache_GetFromStorage()
        {
            var path = "path/1";
            _dataStorage.Get<HasObjectId>(path).Returns(_payload);

            var item = _journal.Get<HasObjectId>(path);

            Assert.Equal(_payload, item);
            _dataStorage.Received().Get<HasObjectId>(path);
        }

        [Fact]
        public void Get_Twice_GetFromCacheSecondTime()
        {
            var path = "path/1";
            _dataStorage.Get<HasObjectId>(path).Returns(_payload);

            _journal.Get<HasObjectId>(path);
            _journal.Get<HasObjectId>(path);

            _dataStorage.Received(1).Get<HasObjectId>(path);
        }

        [Fact]
        public void Get_PutBefore_ReceiveFromCache()
        {
            var path = "path/1";
            _dataStorage.Get<HasObjectId>(path).Returns(_payload);

            _journal.Put(path, _payload);
            _journal.Get<HasObjectId>(path);

            _dataStorage.DidNotReceive().Get<HasObjectId>(path);
        }

//        [Fact]
//        public void Get_ReplayMode_UnlessInCacheReturnNewT()
//        {
//            var path = "path/1";
//
//            _metadataStorage.Get<HasObjectId>(PersistentJournal.JournalPath).Returns(new JournalLog { Checkpoint = Position.Start });
//            _dataStorage.Get<HasObjectId>(path).Returns(_payload);
//
//            _journal.Initialize();
//            _journal.Get<HasObjectId>(path);
//
//            Assert.Equal(SubscriptionMode.Replay, _journal.SubscriptionMode);
//            _dataStorage.DidNotReceive().Get<HasObjectId>(path);
//        }

        [Fact]
        public void Flush_DontKeepMapFromPreviousCall_CleanSlate()
        {
            var path1 = "path/1";
            var path2 = "path/2";
            
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(path1, _payload);
            _journal.Flush();
            _journal.Checkpoint = Position.Gen(13);
            _journal.Put(path2, _payload);
            _journal.Flush();

            _metadataStorage.Received().Put(PersistentJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint.Equals(Position.Gen(12)) && p.Map.Count == 1));
            _metadataStorage.Received().Put(PersistentJournal.JournalPath, Arg.Is<JournalLog>(p => p.Checkpoint.Equals(Position.Gen(13)) && p.Map.Count == 1));
        }

        [Fact]
        public void Flush_WipeWriteCacheBetweenWrites_CleanSlate1()
        {
            var path1 = "path/1";
            var path2 = "path/2";
            
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(path1, _payload);
            _journal.Flush();
            _journal.Checkpoint = Position.Gen(13);
            _journal.Put(path2, _payload);
            _journal.Flush();

            _dataStorage.Received(1).Put(path1, Arg.Any<HasObjectId>());
            _dataStorage.Received(1).Put(path2, Arg.Any<HasObjectId>());
        }

        [Fact]
        public void Flush_WipeStageCacheBetweenWrites_CleanSlate2()
        {
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put("path/1", _payload);
            _journal.Flush();
            _journal.Checkpoint = Position.Gen(13);
            _journal.Put("path/2", _payload);
            _journal.Flush();

            _stageStorage.Received(1).Put("stage/path/1", Arg.Any<HasObjectId>());
            _stageStorage.Received(1).Put("stage/path/2", Arg.Any<HasObjectId>());
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
            
            _journal.Checkpoint = Position.Gen(12);
            _journal.Put(path, _payload);            
            _journal.Flush();
            _journal.Flush();

            _metadataStorage.Received(1).Flush();
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