using System;
using ESPlus;
using System.Threading;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            using (new AmbientSystemTimeScope(() => new DateTime(2022, 1, 1)))
            {
                new Thread(() => Foo()).Start();
            }
            using (new AmbientSystemTimeScope(() => new DateTime(2023, 1, 1)))
            {
                new Thread(() => Foo()).Start();
            }
            Console.ReadLine();
        }

        private static void Foo()
        {
            Console.WriteLine(SystemTime.UtcNow);
        }
    }
}
