using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NucleoMemoryUsage
{
    class MemoryInfo
    {
        public int FlushSize { get; set; }
        public int RamSize { get; set; }
    }

    class MemoryUsage
    {
        public int TextSize { get; set; }
        public int DataSize { get; set; }
        public int BssSize { get; set; }
    }

    class Program
    {
        static Dictionary<string, MemoryInfo> chipData = new Dictionary<string, MemoryInfo>()
        {
            { "/F401RE", new MemoryInfo() { FlushSize = 512 * 1024, RamSize = 96 * 1024 } },
            { "/F746NG", new MemoryInfo() { FlushSize = 1024 * 1024, RamSize = 340 * 1024 } }
        };

        static void Main(string[] args)
        {
            try
            {
                MemoryInfo curChip = chipData["/F401RE"];

                foreach (var arg in args)
                {
                    if (chipData.ContainsKey(arg))
                    {
                        curChip = chipData[arg];
                    }
                    else if (File.Exists(arg) && Path.GetExtension(arg) == ".elf")
                    {
                        DisplayMemoryUsage(curChip, GetMemoryUsage(arg));
                    }
                    else
                    {
                        throw new ApplicationException("コマンドライン引数が不正です");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }
        }

        static MemoryUsage GetMemoryUsage(string elfPath)
        {
            var info = new ProcessStartInfo("arm-none-eabi-size");
            info.Arguments = elfPath;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;

            string result;

            using (var proc = new Process())
            {
                proc.StartInfo = info;
                proc.Start();
                proc.StandardOutput.ReadLine();
                result = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                proc.Close();
            }

            var splitted = result.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var textSize = int.Parse(splitted[0]);
            var dataSize = int.Parse(splitted[1]);
            var bssSize = int.Parse(splitted[2]);

            return new MemoryUsage()
            {
                TextSize = textSize,
                DataSize = dataSize,
                BssSize = bssSize
            };
        }

        static void DisplayMemoryUsage(MemoryInfo info, MemoryUsage usage)
        {
            Console.WriteLine("Memory Usage");

            int usedFlush = usage.TextSize;
            int maxFlush = info.FlushSize;
            Console.WriteLine("Flush\t{0:0.00}%\t({1}B/{2}B)",
                (double)usedFlush / maxFlush * 100, usedFlush, maxFlush);

            int usedRam = (usage.DataSize + usage.BssSize);
            int maxRam = info.RamSize;
            Console.WriteLine("RAM  \t{0:0.00}%\t({1}B/{2}B)",
                (double)usedRam / maxRam * 100, usedRam, maxRam);
        }
    }
}
