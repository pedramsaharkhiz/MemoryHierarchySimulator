using MemoryHierarchySimulator.Models;

namespace MemoryHierarchySimulator.Services;

public class CacheLevel
{
    private readonly CacheSet[] _sets;
    private readonly CacheLevelConfig _config;
    private readonly IReplacementStrategy _strategy;
    private long _time;

    public CacheLevelConfig Config => _config;
    public int Level { get; }
    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public CacheLevel(CacheLevelConfig config, int level, ReplacementPolicy policy)
    {
        _config = config;
        Level = level;
        _strategy = ReplacementStrategyFactory.Create(policy);
        _sets = new CacheSet[config.NumberOfSets];
        for (int i = 0; i < config.NumberOfSets; i++)
            _sets[i] = new CacheSet(config.Associativity, config.BlockSize);
    }

    public (bool IsHit, long EvictedTag) Access(long address, bool isWrite)
    {
        _time++;
        long blockAddr = address / _config.BlockSize;
        int setIdx = (int)(blockAddr % _config.NumberOfSets);
        long tag = blockAddr / _config.NumberOfSets;
        var set = _sets[setIdx];
        var block = set.FindBlock(tag);

        if (block != null)
        {
            Hits++;
            _strategy.OnAccess(block, _time, false);
            if (isWrite) block.IsDirty = true;
            return (true, -1);
        }

        Misses++;
        var victim = _strategy.SelectVictim(set, _time);
        long evictedTag = victim.IsValid ? victim.Tag : -1;
        victim.Tag = tag;
        victim.IsValid = true;
        victim.IsDirty = isWrite;
        _strategy.OnAccess(victim, _time, true);
        return (false, evictedTag);
    }

    public void Reset()
    {
        foreach (var set in _sets) set.Reset();
        Hits = 0; Misses = 0; _time = 0;
    }

    public CacheLevelStatistics GetStatistics() => new()
    {
        Name = _config.Name, Level = Level, Hits = Hits, Misses = Misses
    };
}

public class MainMemory
{
    public int AccessLatency { get; }
    public int Accesses { get; private set; }

    public MainMemory(int latency) => AccessLatency = latency;
    public void Access() => Accesses++;
    public void Reset() => Accesses = 0;
}

public class SecondaryStorage
{
    public int AccessLatency { get; }
    public int Accesses { get; private set; }

    public SecondaryStorage(int latency) => AccessLatency = latency;
    public void Access() => Accesses++;
    public void Reset() => Accesses = 0;
}

public class MemoryAccessGenerator
{
    private readonly Random _random = new();

    public List<MemoryAccess> Generate(AccessPattern pattern, int count, double writeRatio = 0.2)
    {
        return pattern switch
        {
            AccessPattern.Sequential => GenSequential(count, writeRatio),
            AccessPattern.Random => GenRandom(count, writeRatio),
            AccessPattern.Locality => GenLocality(count, writeRatio),
            AccessPattern.Stride => GenStride(count, 64, writeRatio),
            AccessPattern.Loop => GenLoop(count, writeRatio),
            AccessPattern.Mixed => GenMixed(count, writeRatio),
            _ => GenSequential(count, writeRatio)
        };
    }

    private List<MemoryAccess> GenSequential(int count, double wr)
    {
        var list = new List<MemoryAccess>();
        for (int i = 0; i < count; i++)
            list.Add(new MemoryAccess(i * 4, _random.NextDouble() < wr ? AccessType.Write : AccessType.Read, i));
        return list;
    }

    private List<MemoryAccess> GenRandom(int count, double wr)
    {
        var list = new List<MemoryAccess>();
        for (int i = 0; i < count; i++)
            list.Add(new MemoryAccess(_random.Next(10000000) / 4 * 4, _random.NextDouble() < wr ? AccessType.Write : AccessType.Read, i));
        return list;
    }

    private List<MemoryAccess> GenLocality(int count, double wr)
    {
        var list = new List<MemoryAccess>();
        long baseAddr = 0;
        for (int i = 0; i < count; i++)
        {
            if (i % 50 == 0) baseAddr = _random.Next(1000000);
            list.Add(new MemoryAccess(baseAddr + _random.Next(256) / 4 * 4, _random.NextDouble() < wr ? AccessType.Write : AccessType.Read, i));
        }
        return list;
    }

    private List<MemoryAccess> GenStride(int count, int stride, double wr)
    {
        var list = new List<MemoryAccess>();
        for (int i = 0; i < count; i++)
            list.Add(new MemoryAccess(i * stride, _random.NextDouble() < wr ? AccessType.Write : AccessType.Read, i));
        return list;
    }

    private List<MemoryAccess> GenLoop(int count, double wr)
    {
        var list = new List<MemoryAccess>();
        int loopSize = Math.Min(count / 10, 100);
        int t = 0;
        for (int iter = 0; iter < 10; iter++)
            for (int i = 0; i < loopSize && t < count; i++, t++)
                list.Add(new MemoryAccess(i * 4, _random.NextDouble() < wr ? AccessType.Write : AccessType.Read, t));
        return list;
    }

    private List<MemoryAccess> GenMixed(int count, double wr)
    {
        var list = new List<MemoryAccess>();
        int remaining = count, t = 0;
        while (remaining > 0)
        {
            int n = Math.Min(_random.Next(20, 50), remaining);
            var pattern = (AccessPattern)_random.Next(5);
            var sub = Generate(pattern, n, wr);
            foreach (var a in sub) { a.Timestamp = t++; list.Add(a); }
            remaining -= n;
        }
        return list;
    }
}

public class SimulatorService
{
    private readonly List<CacheLevel> _caches;
    private readonly MainMemory _mainMem;
    private readonly SecondaryStorage _storage;
    private readonly SimulationStatistics _stats = new();

    public SimulationStatistics Statistics => _stats;

    public SimulatorService(List<CacheLevelConfig> configs, ReplacementPolicy policy, int mainMemLatency, int storageLatency)
    {
        _caches = new List<CacheLevel>();
        for (int i = 0; i < configs.Count; i++)
            _caches.Add(new CacheLevel(configs[i], i + 1, policy));
        _mainMem = new MainMemory(mainMemLatency);
        _storage = new SecondaryStorage(storageLatency);
    }

    public AccessResult Access(MemoryAccess ma)
    {
        var result = new AccessResult { Address = ma.Address, AccessType = ma.Type };
        bool isWrite = ma.Type == AccessType.Write;

        for (int i = 0; i < _caches.Count; i++)
        {
            var cache = _caches[i];
            result.TotalLatency += cache.Config.AccessLatency;
            var (isHit, _) = cache.Access(ma.Address, isWrite);
            if (isHit) { result.IsHit = true; result.HitLevel = i + 1; break; }
        }

        if (!result.IsHit)
        {
            result.TotalLatency += _mainMem.AccessLatency;
            _mainMem.Access();
            if (new Random().NextDouble() < 0.01)
            {
                result.TotalLatency += _storage.AccessLatency;
                _storage.Access();
            }
        }

        _stats.TotalAccesses++;
        if (ma.Type == AccessType.Read) _stats.ReadAccesses++; else _stats.WriteAccesses++;
        _stats.TotalLatency += result.TotalLatency;
        return result;
    }

    public async Task<List<AccessResult>> RunAsync(List<MemoryAccess> accesses, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            var results = new List<AccessResult>();
            for (int i = 0; i < accesses.Count; i++)
            {
                results.Add(Access(accesses[i]));
                if (progress != null && i % 100 == 0)
                    progress.Report((int)((i + 1) * 100.0 / accesses.Count));
            }
            UpdateStats();
            return results;
        });
    }

    private void UpdateStats()
    {
        _stats.CacheLevelStats.Clear();
        foreach (var c in _caches) _stats.CacheLevelStats.Add(c.GetStatistics());
        _stats.MainMemoryAccesses = _mainMem.Accesses;
        _stats.SecondaryStorageAccesses = _storage.Accesses;
    }

    public void Reset()
    {
        foreach (var c in _caches) c.Reset();
        _mainMem.Reset();
        _storage.Reset();
        _stats.Reset();
    }

    public string GetSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"تعداد کل دسترسی: {_stats.TotalAccesses:N0}");
        sb.AppendLine($"خواندن: {_stats.ReadAccesses:N0} | نوشتن: {_stats.WriteAccesses:N0}");
        sb.AppendLine();
        foreach (var s in _stats.CacheLevelStats)
            sb.AppendLine($"{s.Name}: Hit={s.Hits:N0} ({s.HitRate:F1}%) | Miss={s.Misses:N0}");
        sb.AppendLine();
        sb.AppendLine($"دسترسی RAM: {_stats.MainMemoryAccesses:N0}");
        sb.AppendLine($"میانگین تأخیر: {_stats.AverageLatency:F1} سیکل");
        return sb.ToString();
    }
}
