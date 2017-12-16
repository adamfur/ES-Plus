using System.Threading.Tasks;
using ESPlus.Aggregates;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task SaveAsync(ReplayableObject aggregate);
        Task SaveAsync(AppendableObject aggregate);
        Task SaveNewAsync(IAggregate aggregate);
        Task<TAggregate> GetByIdAsync<TAggregate>(string id, int version = int.MaxValue) where TAggregate : IAggregate;
    }

    public static class WritePolicy
    {
        public static long Any = -2;
        public static long NoStream = -1;
        public static long EmptyStream = -1;
        public static long StreamExists = -4;
    }
}

/*
        /// <summary>
        /// This write should not conflict with anything and should always succeed.
        /// </summary>
        public const long Any = -2;
        /// <summary>
        /// The stream being written to should not yet exist. If it does exist treat that as a concurrency problem.
        /// </summary>
        public const long NoStream = -1;
        /// <summary>
        /// The stream should exist and should be empty. If it does not exist or is not empty treat that as a concurrency problem.
        /// </summary>
        public const long EmptyStream = -1;

        /// <summary>
        /// The stream should exist. If it or a metadata stream does not exist treat that as a concurrency problem.
        /// </summary>
        public const long StreamExists = -4;

-2 states that this write should never conflict with anything and should always succeed.
-1 states that the stream should not exist at the time of the writing (this write will create it)
0 states that the stream should exist but should be empty


ExpectedVersion.Any	This disables the optimistic concurrency check.
ExpectedVersion.NoStream	This specifies the expectation that target stream does not yet exist.
ExpectedVersion.EmptyStream	This specifies the expectation that the target stream has been explicitly created, but does not yet have any user events written in it.
ExpectedVersion.StreamExists	This specifies the expectation that the target stream or its metadata stream has been created, but does not expect the stream to be at a specific event number.
*/
