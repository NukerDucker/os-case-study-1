// Case Study 01 - Multithreading
// Updated: 2026-08-09
using System;
using System.IO;
using System.Linq;
using CalculatingFunctions;
using System.Threading;
using System.Diagnostics;

class Program
{
    const decimal expected_result = 4686980924312.00m;
    const long data_elements = 11000001;
    const long max_accessible_elements = 10000000; // last ~1M elements intentionally excluded
    const int iterations = 30;

    static decimal[] data = new decimal[data_elements];

    // Dynamic scheduling (Anderson & Dahlin §7.2.2): atomic counter replaces fixed slices.
    // Fast P-cores claim more slices; slower E-cores claim fewer. No core sits idle.
    private const int SliceSize = 8_192;  // benchmarked vs 4096/16384 on M2 Air

    // Calculate1 spec: value[i] *= 0.1 each call.
    // When |value[i]| < 5: e.g. (int)4.9 % 2=0 → sum=0.98 → Math.Round(0.49)=0.
    // All remaining rounds return 0 → skip them (adds 0 to result, safe).
    private const decimal ZeroCutoff = 5m;

    private static int workHead = 0;  // shared atomic work pointer

    // Algorithm of Calculate1(ref decimal[] value, ref long idx)
    /*
        i = idx;
        if (i >= value.Length) i = value.Length - 1;

        if      ((int)value[i] % 2 == 0) sum = (decimal)((double)value[i] * 0.2);
        else if ((int)value[i] % 3 == 0) sum = (decimal)((double)value[i] * 0.3);
        else if ((int)value[i] % 5 == 0) sum = (decimal)((double)value[i] * 0.5);
        else if ((int)value[i] % 7 == 0) sum = (decimal)((double)value[i] * 0.7);
        else                              sum = (decimal)((double)value[i] * 0.1);

        result = ((long)sum % 2 == 0)
            ? Math.Round(sum * 0.5m)
            : Math.Round(sum * -0.3m);

        value[i] *= 0.1m;  // mutates data in-place
        idx++;
        return result;
    */

    private static decimal WorkerFunc(int workerIndex, int workerCount)
    {
        CalClass cf = new CalClass();
        decimal localResult = 0m;

        while (true)
        {
            // Claim next slice atomically — Anderson & Dahlin §5.1.2 Atomic Operations.
            int sliceStart = Interlocked.Add(ref workHead, SliceSize) - SliceSize;
            if (sliceStart >= max_accessible_elements) break;
            int sliceEnd = (int)Math.Min((long)sliceStart + SliceSize, max_accessible_elements);

            for (int i = sliceStart; i < sliceEnd; i++)
            {
                // Loop inversion: value-outer, rounds-inner.
                // Calculate1 only reads/writes data[i] — per-element independent,
                // so inversion is mathematically equivalent to original rounds-outer loop.
                // Enables early exit per-element once value decays near zero.
                for (int round = 0; round < iterations; round++)
                {
                    if (Math.Abs(data[i]) < ZeroCutoff) break;  // returns 0 anyway — safe to skip
                    long at = i;
                    localResult += cf.Calculate1(ref data, ref at);
                }
            }
        }
        // ponytail: workerResults[] false sharing — one write per thread at end, not worth padding
        return localResult;
    }

    private static void LoadData()
    {
        Console.WriteLine("Loading data...");
        try
        {
            using FileStream fs = new FileStream("data.bin", FileMode.Open);
            using BinaryReader br = new BinaryReader(fs);
            for (int i = 0; i < data.Length; i++)
            {
                float f = br.ReadSingle();
                data[i] = (decimal)(f * 36); // float multiply intentional — (decimal)f * 36m changes result
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load data.bin: {ex.Message}");
            Environment.Exit(1);
        }
        Console.WriteLine("Data loaded successfully.\n");
    }

    private static void Main(string[] args)
    {
        int workerCount = (args.Length > 0 && int.TryParse(args[0], out int n) && n > 0)
            ? n
            : Environment.ProcessorCount;

        Console.WriteLine($"Workers: {workerCount} | Build: "
#if DEBUG
            + "Debug (use -c Release for timing)"
#else
            + "Release"
#endif
        );

        LoadData(); // outside stopwatch — measure compute only
        Console.WriteLine("Calculation start ...");

        decimal[] workerResults = new decimal[workerCount];
        Thread[] workers = new Thread[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            int wi = i; // closure capture — replaces ThreadParameter class
            workers[i] = new Thread(() => workerResults[wi] = WorkerFunc(wi, workerCount));
        }

        workHead = 0;  // reset atomic pointer before workers start

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < workerCount; i++) workers[i].Start();
        for (int i = 0; i < workerCount; i++) workers[i].Join();

        sw.Stop();

        decimal result = workerResults.Sum();

        Console.WriteLine($"Calculation finished in {sw.ElapsedMilliseconds} ms. Result: {result:F8}");
        if (result != expected_result)
            Console.WriteLine($"Invalid result, expected {expected_result:F8}");
        else
            Console.WriteLine("Final result OK.");
    }
}
