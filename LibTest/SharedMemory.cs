using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace LibTest
{
    public static class SharedMemory
    {
        const int PageSize = 4096;
        const int PageCount = 16;
        public static MemoryMappedViewAccessor Self { get; set; }
        public static List<MemoryMappedViewAccessor> Others
        {
            get
            {
                var runningPIDs =
                    Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Select(p => p.Id);
                return pageTable
                    .Select((pid, index) => new { pid, index })
                    .Where(o => runningPIDs.Contains(o.pid) && o.index != selfIndex && o.index != 0)
                    .Select(o => GetPage(o.index))
                    .ToList();
            }
        }
        internal static int selfIndex;
        static int[] pageTable => page0.ReadArray<int>(0, PageCount);
        static MemoryMappedFile mmf =
            MemoryMappedFile.CreateOrOpen("GameWindowsLoop", PageSize * PageCount);
        static MemoryMappedViewAccessor page0 = mmf.CreateViewAccessor(0, PageSize);

        static SharedMemory()
        {
            
        }

        internal static void Init()
        {
            Self = mmf.CreateViewAccessor((selfIndex = AllocatePage()) * PageSize, PageSize);
        }

        public static MemoryMappedViewAccessor GetPage(int index) =>
            mmf.CreateViewAccessor(index * PageSize, PageSize);

        static int AllocatePage()
        {
            var pageTable = SharedMemory.pageTable;
            var runningPIDs = new HashSet<int>(
                Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Select(p => p.Id));
            Foo.Log($"Processes: {runningPIDs.Count}");
            for (var i = 1; i < PageCount; i++)
            {
                if (!runningPIDs.Contains(pageTable[i]))
                {
                    page0.Write(i * sizeof(int), Process.GetCurrentProcess().Id);
                    page0.Flush();
                    Foo.Log(SharedMemory.pageTable.Aggregate("", (a, b) => $"{a}{b} "));
                    return i;
                }
            }
            throw new Exception("Too many instances.");
        }

        public static T[] ReadArray<T>(this MemoryMappedViewAccessor accessor, int offset, int count)
            where T : struct
        {
            var array = new T[count];
            accessor.ReadArray(offset, array, 0, count);
            return array;
        }
    }
}
