using BenchmarkDotNet.Running;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;
namespace VectorBenchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var a = new VectorBenchmark();
            //a.Setup();

            //var sw = new Stopwatch();
            //sw.Start();

            //for (int i = 0; i < 50; i++)
            //    a.VectorSize256X4ReorderedAlligned();

            //sw.Stop();
            //Console.WriteLine(sw.Elapsed.ToString());

            ////Parallel.For(0, 12, (_) =>
            ////{
            ////    for (int i = 0; i < 50; i++)
            ////        a.VectorSize128ReorderedAlligned();
            ////});

            //a.Cleanup();
            BenchmarkRunner.Run<VectorBenchmark>();
        }
    }
}