using System.Diagnostics;

namespace DustyEngine;

public class ResourceMonitor
{
    private static Process process;
    private static TimeSpan prevCpuTime;
    private static DateTime prevTime;
    private static int processorCount;

    public static void Init()
    {
        process = Process.GetCurrentProcess();
        processorCount = Environment.ProcessorCount;

        prevCpuTime = process.TotalProcessorTime;
        prevTime = DateTime.Now;
    }

    public static void Update()
    {
        process.Refresh();
        TimeSpan currentCpuTime = process.TotalProcessorTime;
        DateTime currentTime = DateTime.Now;

        double cpuUsedMs = (currentCpuTime - prevCpuTime).TotalMilliseconds;
        double elapsedMs = (currentTime - prevTime).TotalMilliseconds;

        double cpuUsage = (cpuUsedMs / (elapsedMs * processorCount)) * 100;

        long memoryMB = process.PrivateMemorySize64 / (1024 * 1024);

        Console.Clear();
        Console.WriteLine("=== Resource Monitor ===");
        Console.WriteLine($"CPU Usage: {cpuUsage:F2}%");
        Console.WriteLine($"RAM Usage: {memoryMB} MB");

        prevCpuTime = currentCpuTime;
        prevTime = currentTime;
    }
}