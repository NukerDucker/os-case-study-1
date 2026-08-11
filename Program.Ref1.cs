// Case Study 01 - Ref1: Naive / race-condition implementation
// Reference version — demonstrates what NOT to do
// Updated: 2026-08-09
//
// Ref1: naively throw threads at shared state.
// Multiple threads read/write the same `result` and `index`
// with zero synchronization → race condition → wrong result.
//
// To swap in: rename this to Program.cs (backup the proper one first)
// Expected result: 4686980924312.00 — Ref1 will NOT match it.

using System;
using System.IO;
using CalculatingFunctions;
using System.Threading;
using System.Diagnostics;

class Program
{
    const decimal expected_result = 4686980924312.00m;
    const long data_elements    = 11000001;
    const long max_elements     = 10000000;
    const int  worker_count     = 8;

    static decimal[] data   = new decimal[data_elements];
    static decimal   result = 0;   // SHARED — race condition
    static long      index  = 0;   // SHARED — race condition

    private static void ThreadWork()
    {
        CalClass cf = new CalClass();
        for (int i = 0; i < 30; i++)
        {
            index = 0;                         // every thread resets this — chaos
            while (index < max_elements)
                result += cf.Calculate1(ref data, ref index);  // unsynchronized +=
        }
    }

    private static void LoadData()
    {
        Console.WriteLine("Loading data...");
        using FileStream fs = new FileStream("data.bin", FileMode.Open);
        using BinaryReader br = new BinaryReader(fs);
        for (int i = 0; i < data.Length; i++)
        {
            float f = br.ReadSingle();
            data[i] = (decimal)(f * 36); // float multiply intentional
        }
        Console.WriteLine("Data loaded.\n");
    }

    private static void Main(string[] args)
    {
        Console.WriteLine($"[CHEESY] Workers: {worker_count} | races guaranteed");
        LoadData();

        Thread[] workers = new Thread[worker_count];
        for (int i = 0; i < worker_count; i++)
            workers[i] = new Thread(ThreadWork);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < worker_count; i++) workers[i].Start();
        for (int i = 0; i < worker_count; i++) workers[i].Join();

        sw.Stop();

        Console.WriteLine($"Finished in {sw.ElapsedMilliseconds} ms. Result: {result:F8}");
        if (result != expected_result)
            Console.WriteLine($"WRONG result (expected {expected_result:F8}) — race condition confirmed.");
        else
            Console.WriteLine("Result OK (got lucky — run again, it will differ).");
    }
}
