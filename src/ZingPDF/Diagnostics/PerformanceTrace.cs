using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace ZingPDF.Diagnostics;

/// <summary>
/// Lightweight opt-in timing aggregator for local performance investigations.
/// </summary>
public static class PerformanceTrace
{
    private static readonly ConcurrentDictionary<string, TraceStats> _stats = new(StringComparer.Ordinal);
    private static int _isEnabled = ReadInitialEnabledState() ? 1 : 0;

    public static bool IsEnabled => Volatile.Read(ref _isEnabled) == 1;

    public static void SetEnabled(bool enabled) => Volatile.Write(ref _isEnabled, enabled ? 1 : 0);

    public static void Reset() => _stats.Clear();

    public static TraceScope Measure(string name)
    {
        if (!IsEnabled)
        {
            return default;
        }

        return new TraceScope(name, Stopwatch.GetTimestamp());
    }

    public static string GetSummary(int maxEntries = 20)
    {
        var snapshot = _stats
            .Select(kvp => new
            {
                Name = kvp.Key,
                Count = kvp.Value.Count,
                TotalTicks = kvp.Value.TotalTicks,
                MaxTicks = kvp.Value.MaxTicks,
            })
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.TotalTicks)
            .ThenByDescending(x => x.Count)
            .Take(maxEntries)
            .ToList();

        if (snapshot.Count == 0)
        {
            return "Performance trace captured no samples.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("Performance trace summary");
        builder.AppendLine("Method | Calls | Total ms | Avg us | Max us");
        builder.AppendLine("--- | ---: | ---: | ---: | ---:");

        foreach (var entry in snapshot)
        {
            double totalMs = entry.TotalTicks * 1000d / Stopwatch.Frequency;
            double avgUs = entry.TotalTicks * 1_000_000d / Stopwatch.Frequency / entry.Count;
            double maxUs = entry.MaxTicks * 1_000_000d / Stopwatch.Frequency;
            builder.AppendLine($"{entry.Name} | {entry.Count} | {totalMs:F3} | {avgUs:F3} | {maxUs:F3}");
        }

        return builder.ToString();
    }

    public static void WriteSummary(TextWriter writer, int maxEntries = 20)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine(GetSummary(maxEntries));
    }

    internal static void Record(string name, long elapsedTicks)
    {
        if (elapsedTicks <= 0)
        {
            return;
        }

        var stats = _stats.GetOrAdd(name, static _ => new TraceStats());
        stats.Add(elapsedTicks);
    }

    private static bool ReadInitialEnabledState()
    {
        var value = Environment.GetEnvironmentVariable("ZINGPDF_PERF_TRACE");
        return value is "1" or "true" or "TRUE" or "True";
    }

    public readonly struct TraceScope : IDisposable
    {
        private readonly string? _name;
        private readonly long _startTimestamp;

        internal TraceScope(string name, long startTimestamp)
        {
            _name = name;
            _startTimestamp = startTimestamp;
        }

        public void Dispose()
        {
            if (_name is null)
            {
                return;
            }

            PerformanceTrace.Record(_name, Stopwatch.GetTimestamp() - _startTimestamp);
        }
    }

    private sealed class TraceStats
    {
        private long _count;
        private long _totalTicks;
        private long _maxTicks;

        public long Count => Volatile.Read(ref _count);
        public long TotalTicks => Volatile.Read(ref _totalTicks);
        public long MaxTicks => Volatile.Read(ref _maxTicks);

        public void Add(long elapsedTicks)
        {
            Interlocked.Increment(ref _count);
            Interlocked.Add(ref _totalTicks, elapsedTicks);

            long currentMax;
            do
            {
                currentMax = Volatile.Read(ref _maxTicks);
                if (elapsedTicks <= currentMax)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref _maxTicks, elapsedTicks, currentMax) != currentMax);
        }
    }
}
