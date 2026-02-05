using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoryHierarchySimulator.Models;
using MemoryHierarchySimulator.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace MemoryHierarchySimulator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly MemoryAccessGenerator _generator = new();
    private SimulatorService? _simulator;

    [ObservableProperty] private int _l1Size = 32, _l1BlockSize = 64, _l1Assoc = 8, _l1Latency = 4;
    [ObservableProperty] private bool _l1Enabled = true;
    [ObservableProperty] private int _l2Size = 256, _l2BlockSize = 64, _l2Assoc = 8, _l2Latency = 12;
    [ObservableProperty] private bool _l2Enabled = true;
    [ObservableProperty] private int _l3Size = 8192, _l3BlockSize = 64, _l3Assoc = 16, _l3Latency = 40;
    [ObservableProperty] private bool _l3Enabled = true;
    [ObservableProperty] private int _ramLatency = 100;
    [ObservableProperty] private int _accessCount = 10000;
    [ObservableProperty] private double _writeRatio = 0.2;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _status = "آماده";
    [ObservableProperty] private string _summary = "";
    [ObservableProperty] private string _policyDesc = "";
    [ObservableProperty] private int _totalHits, _totalMisses;
    [ObservableProperty] private double _hitRate, _avgLatency;
    [ObservableProperty] private int _selectedPolicyIndex = 0;
    [ObservableProperty] private int _selectedPatternIndex = 0;
    [ObservableProperty] private ISeries[] _chartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _yAxes = Array.Empty<Axis>();
    [ObservableProperty] private ISeries[] _comparisonChartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _comparisonXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _comparisonYAxes = Array.Empty<Axis>();

    public ObservableCollection<string> Policies { get; } = new() { "LRU", "FIFO", "Random", "LFU", "MRU", "RoundRobin", "SecondChance", "LFRU" };
    public ObservableCollection<string> Patterns { get; } = new() { "Sequential", "Random", "Locality", "Stride", "Loop", "Mixed" };
    public ObservableCollection<AccessResult> Results { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();

    public MainViewModel() => UpdatePolicyDesc();

    partial void OnSelectedPolicyIndexChanged(int value) => UpdatePolicyDesc();

    private void UpdatePolicyDesc()
    {
        var policy = (ReplacementPolicy)SelectedPolicyIndex;
        PolicyDesc = ReplacementStrategyFactory.GetDescription(policy);
    }

    [RelayCommand]
    private async Task Run()
    {
        if (IsRunning) return;
        IsRunning = true;
        Progress = 0;
        Results.Clear();
        Logs.Clear();

        try
        {
            var configs = new List<CacheLevelConfig>();
            if (L1Enabled) configs.Add(new() { Name = "L1", TotalSize = L1Size * 1024, BlockSize = L1BlockSize, Associativity = L1Assoc, AccessLatency = L1Latency });
            if (L2Enabled) configs.Add(new() { Name = "L2", TotalSize = L2Size * 1024, BlockSize = L2BlockSize, Associativity = L2Assoc, AccessLatency = L2Latency });
            if (L3Enabled) configs.Add(new() { Name = "L3", TotalSize = L3Size * 1024, BlockSize = L3BlockSize, Associativity = L3Assoc, AccessLatency = L3Latency });

            if (configs.Count == 0) { Status = "حداقل یک کش فعال کنید!"; IsRunning = false; return; }

            var policy = (ReplacementPolicy)SelectedPolicyIndex;
            var pattern = (AccessPattern)SelectedPatternIndex;

            _simulator = new SimulatorService(configs, policy, RamLatency, 10000);
            Logs.Add($"سیاست: {policy} | الگو: {pattern}");

            Status = "تولید دسترسی‌ها...";
            var accesses = _generator.Generate(pattern, AccessCount, WriteRatio);
            Logs.Add($"تعداد دسترسی: {accesses.Count}");

            Status = "شبیه‌سازی...";
            var progress = new Progress<int>(p => { Progress = p; Status = $"پیشرفت: {p}%"; });
            var results = await _simulator.RunAsync(accesses, progress);

            foreach (var r in results.TakeLast(500)) Results.Add(r);

            var stats = _simulator.Statistics;
            TotalHits = stats.CacheLevelStats.Sum(s => s.Hits);
            TotalMisses = stats.CacheLevelStats.LastOrDefault()?.Misses ?? 0;
            HitRate = stats.TotalAccesses > 0 ? (double)TotalHits / stats.TotalAccesses * 100 : 0;
            AvgLatency = stats.AverageLatency;
            Summary = _simulator.GetSummary();

            UpdateChart(stats);
            Status = "تمام!";
            Logs.Add(Summary);
        }
        catch (Exception ex) { Status = $"خطا: {ex.Message}"; }
        finally { IsRunning = false; Progress = 100; }
    }

    private void UpdateChart(SimulationStatistics stats)
    {
        var hits = stats.CacheLevelStats.Select(s => (double)s.Hits).ToList();
        var misses = stats.CacheLevelStats.Select(s => (double)s.Misses).ToList();
        var labels = stats.CacheLevelStats.Select(s => s.Name).ToList();

        ChartSeries = new ISeries[]
        {
            new ColumnSeries<double> { Name = "Hit", Values = hits, Fill = new SolidColorPaint(SKColor.Parse("#4CAF50")) },
            new ColumnSeries<double> { Name = "Miss", Values = misses, Fill = new SolidColorPaint(SKColor.Parse("#F44336")) }
        };
        XAxes = new Axis[] { new() { Labels = labels } };
        YAxes = new Axis[] { new() };
    }

    [RelayCommand]
    private async Task Compare()
    {
        if (IsRunning) return;
        IsRunning = true;
        Logs.Clear();
        Logs.Add("══════ مقایسه سیاست‌ها ══════");

        var configs = new List<CacheLevelConfig>();
        if (L1Enabled) configs.Add(new() { Name = "L1", TotalSize = L1Size * 1024, BlockSize = L1BlockSize, Associativity = L1Assoc, AccessLatency = L1Latency });
        if (L2Enabled) configs.Add(new() { Name = "L2", TotalSize = L2Size * 1024, BlockSize = L2BlockSize, Associativity = L2Assoc, AccessLatency = L2Latency });

        var pattern = (AccessPattern)SelectedPatternIndex;
        var accesses = _generator.Generate(pattern, AccessCount, WriteRatio);

        var results = new Dictionary<string, double>();
        foreach (ReplacementPolicy p in Enum.GetValues<ReplacementPolicy>())
        {
            Status = $"تست {p}...";
            var sim = new SimulatorService(configs, p, RamLatency, 10000);
            await sim.RunAsync(accesses);
            var hr = sim.Statistics.TotalAccesses > 0 ? (double)sim.Statistics.CacheLevelStats.Sum(s => s.Hits) / sim.Statistics.TotalAccesses * 100 : 0;
            results[p.ToString()] = hr;
            Logs.Add($"{p}: Hit Rate = {hr:F2}%");
        }

        var best = results.OrderByDescending(r => r.Value).First();
        Logs.Add($"══════ بهترین: {best.Key} ({best.Value:F2}%) ══════");
        Status = "مقایسه تمام!";

        // Update comparison chart
        var labels = results.Keys.ToList();
        var values = results.Values.Select(v => (double)v).ToList();
        ComparisonChartSeries = new ISeries[]
        {
            new ColumnSeries<double> { Name = "Hit Rate %", Values = values, Fill = new SolidColorPaint(SKColor.Parse("#42A5F5")) }
        };
        ComparisonXAxes = new Axis[] { new() { Labels = labels } };
        ComparisonYAxes = new Axis[] { new() { MinLimit = 0, MaxLimit = 100 } };

        IsRunning = false;
    }

    [RelayCommand]
    private void Reset()
    {
        Results.Clear();
        Logs.Clear();
        TotalHits = TotalMisses = 0;
        HitRate = AvgLatency = 0;
        Summary = "";
        ChartSeries = Array.Empty<ISeries>();
        ComparisonChartSeries = Array.Empty<ISeries>();
        ComparisonXAxes = Array.Empty<Axis>();
        ComparisonYAxes = Array.Empty<Axis>();
        Status = "آماده";
        Progress = 0;
    }
}
