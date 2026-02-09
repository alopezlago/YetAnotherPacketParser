using BenchmarkDotNet.Running;

namespace BenchmarkSuite1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
