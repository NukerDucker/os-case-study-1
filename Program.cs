// Case Study 01 - Multithreading 
// Updated: 2026-08-02
using System;
using System.IO;
using CalculatingFunctions;
using System.Threading;
using System;
using System.Text.Json;
using System.Diagnostics;

class Program
{
    static decimal[] data = new decimal[11000001];
    static decimal result = 0;
    static long index = 0;

    //Algorithm of Calculate1(ref decimal[] value, ref long idx)
    /*      {
              long i, _j, _value1;
              decimal _value, result = 0, sum = 0;

              i = idx;
              if (i >= value.Length)
              {
                  i = value.Length - 1;

              }
              if ((int)value[i] % 2 == 0)
              {
                  sum += (decimal)((double)value[i] * 0.2);
              }
              else if ((int)value[i] % 3 == 0)
              {
                  sum += (decimal)((double)value[i] * 0.3);
              }
              else if ((int)value[i] % 5 == 0)
              {
                  sum += (decimal)((double)value[i] * 0.5);
              }
              else if ((int)value[i] % 7 == 0)
              {
                  sum += (decimal)((double)value[i] * 0.7);
              }
              else
              {
                  sum += (decimal)((double)value[i] * 0.1);
              }

              if ((long)sum % 2 == 0)
              {
                  result = Math.Round(sum * (decimal)0.5);
              }
              else
              {
                  result = Math.Round((sum * (-1)) * (decimal)0.3);
              } 

              value[i] *= (decimal)0.1;
              idx++;
              return result;
          }
      */

    private static void ThreadWork()
    {
        CalClass CF = new CalClass();
        int i = 0;

        while (i < 30)
        {
            index = 0;
            while (index < 10000000)
            {
                result += CF.Calculate1(ref data, ref index);
            }
            i++;
        }
    }

    private static void LoadData()
    {
        Console.WriteLine("Loading data...");
        FileStream fs = new FileStream("data.bin", FileMode.Open);
        BinaryReader br = new BinaryReader(fs);
        for (int i = 0; i < data.Length; i++)
        {
            Single f = br.ReadSingle();
            data[i] = (decimal)(f * 36);
        }
        Console.WriteLine("Data loaded successfully.\n\n");
    }

    private static void Main(string[] args)
    {
        LoadData();
        Console.WriteLine("Calculation start ...");

        Thread Th1 = new Thread(ThreadWork);
        //Thread Th2 = new Thread(ThreadWork);

        Stopwatch _st = new Stopwatch();
        _st.Start();

        Th1.Start();
        //Th2.Start();
        Th1.Join(); // Wait for the thread to finish
        //Th2.Join(); // Wait for the thread to finish

        _st.Stop();
        Console.WriteLine($"Calculation finished in {_st.ElapsedMilliseconds} ms. Result: {(result).ToString("F2")}");
    }
}