using MemoryHierarchySimulator.Models;

namespace MemoryHierarchySimulator.Services;

public interface IReplacementStrategy
{
    string Name { get; }
    string Description { get; }
    CacheBlock SelectVictim(CacheSet set, long currentTime);
    void OnAccess(CacheBlock block, long currentTime, bool isNewBlock);
}

public class LruStrategy : IReplacementStrategy
{
    public string Name => "LRU";
    public string Description => "Least Recently Used: بلوکی که مدت بیشتری از آخرین دسترسی گذشته جایگزین می‌شود.";

    public CacheBlock SelectVictim(CacheSet set, long currentTime)
    {
        CacheBlock? victim = null;
        long minTime = long.MaxValue;
        foreach (var block in set.Blocks)
        {
            if (!block.IsValid) return block;
            if (block.LastAccessTime < minTime) { minTime = block.LastAccessTime; victim = block; }
        }
        return victim ?? set.Blocks[0];
    }

    public void OnAccess(CacheBlock block, long currentTime, bool isNewBlock) => block.LastAccessTime = currentTime;
}

public class FifoStrategy : IReplacementStrategy
{
    public string Name => "FIFO";
    public string Description => "First In First Out: قدیمی‌ترین بلوک جایگزین می‌شود.";

    public CacheBlock SelectVictim(CacheSet set, long currentTime)
    {
        CacheBlock? victim = null;
        long minTime = long.MaxValue;
        foreach (var block in set.Blocks)
        {
            if (!block.IsValid) return block;
            if (block.InsertionTime < minTime) { minTime = block.InsertionTime; victim = block; }
        }
        return victim ?? set.Blocks[0];
    }

    public void OnAccess(CacheBlock block, long currentTime, bool isNewBlock)
    {
        if (isNewBlock) block.InsertionTime = currentTime;
    }
}

public class RandomStrategy : IReplacementStrategy
{
    private readonly Random _random = new();
    public string Name => "Random";
    public string Description => "تصادفی: یک بلوک به صورت تصادفی انتخاب می‌شود.";

    public CacheBlock SelectVictim(CacheSet set, long currentTime)
    {
        foreach (var block in set.Blocks)
            if (!block.IsValid) return block;
        return set.Blocks[_random.Next(set.Blocks.Length)];
    }

    public void OnAccess(CacheBlock block, long currentTime, bool isNewBlock) { }
}

public class LfuStrategy : IReplacementStrategy
{
    public string Name => "LFU";
    public string Description => "Least Frequently Used: بلوک با کمترین تعداد دسترسی جایگزین می‌شود.";

    public CacheBlock SelectVictim(CacheSet set, long currentTime)
    {
        CacheBlock? victim = null;
        int minCount = int.MaxValue;
        foreach (var block in set.Blocks)
        {
            if (!block.IsValid) return block;
            if (block.AccessCount < minCount) { minCount = block.AccessCount; victim = block; }
        }
        return victim ?? set.Blocks[0];
    }

    public void OnAccess(CacheBlock block, long currentTime, bool isNewBlock)
    {
        block.AccessCount = isNewBlock ? 1 : block.AccessCount + 1;
        block.LastAccessTime = currentTime;
    }
}

public class MruStrategy : IReplacementStrategy
{
    public string Name => "MRU";
    public string Description => "Most Recently Used: بلوکی که اخیراً استفاده شده جایگزین می‌شود.";

    public CacheBlock SelectVictim(CacheSet set, long currentTime)
    {
        CacheBlock? victim = null;
        long maxTime = long.MinValue;
        foreach (var block in set.Blocks)
        {
            if (!block.IsValid) return block;
            if (block.LastAccessTime > maxTime) { maxTime = block.LastAccessTime; victim = block; }
        }
        return victim ?? set.Blocks[0];
    }

    public void OnAccess(CacheBlock block, long currentTime, bool isNewBlock) => block.LastAccessTime = currentTime;
}

public class RoundRobinStrategy : IReplacementStrategy
{
    public string Name => "Round Robin";
    public string Description => "چرخشی: بلوک‌ها به ترتیب جایگزین می‌شوند.";

    public CacheBlock SelectVictim(CacheSet set, long currentTime)
    {
        foreach (var block in set.Blocks)
            if (!block.IsValid) return block;
        var victim = set.Blocks[set.CircularPointer];
        set.CircularPointer = (set.CircularPointer + 1) % set.Blocks.Length;
        return victim;
    }

    public void OnAccess(CacheBlock block, long currentTime, bool isNewBlock) { }
}

public class SecondChanceStrategy : IReplacementStrategy
{
    public string Name => "Second Chance";
    public string Description => "شانس دوم: FIFO بهبود یافته با بیت مرجع.";

    public CacheBlock SelectVictim(CacheSet set, long currentTime)
    {
        foreach (var block in set.Blocks)
            if (!block.IsValid) return block;

        int maxIter = set.Blocks.Length * 2;
        for (int i = 0; i < maxIter; i++)
        {
            var block = set.Blocks[set.CircularPointer];
            if (!block.ReferenceBit)
            {
                set.CircularPointer = (set.CircularPointer + 1) % set.Blocks.Length;
                return block;
            }
            block.ReferenceBit = false;
            set.CircularPointer = (set.CircularPointer + 1) % set.Blocks.Length;
        }
        return set.Blocks[set.CircularPointer];
    }

    public void OnAccess(CacheBlock block, long currentTime, bool isNewBlock)
    {
        block.ReferenceBit = true;
        block.LastAccessTime = currentTime;
        if (isNewBlock) block.InsertionTime = currentTime;
    }
}

public class LfruStrategy : IReplacementStrategy
{
    public string Name => "LFRU";
    public string Description => "ترکیب LFU و LRU برای بهترین عملکرد.";

    public CacheBlock SelectVictim(CacheSet set, long currentTime)
    {
        CacheBlock? victim = null;
        double minScore = double.MaxValue;
        int maxCount = 1;
        long maxRecency = 1;

        foreach (var block in set.Blocks)
        {
            if (!block.IsValid) return block;
            maxCount = Math.Max(maxCount, block.AccessCount);
            maxRecency = Math.Max(maxRecency, currentTime - block.LastAccessTime);
        }

        foreach (var block in set.Blocks)
        {
            double freqScore = (double)block.AccessCount / maxCount;
            double recScore = 1.0 - ((double)(currentTime - block.LastAccessTime) / maxRecency);
            double score = 0.6 * freqScore + 0.4 * recScore;
            if (score < minScore) { minScore = score; victim = block; }
        }
        return victim ?? set.Blocks[0];
    }

    public void OnAccess(CacheBlock block, long currentTime, bool isNewBlock)
    {
        block.LastAccessTime = currentTime;
        block.AccessCount = isNewBlock ? 1 : block.AccessCount + 1;
        if (isNewBlock) block.InsertionTime = currentTime;
    }
}

public static class ReplacementStrategyFactory
{
    public static IReplacementStrategy Create(ReplacementPolicy policy) => policy switch
    {
        ReplacementPolicy.LRU => new LruStrategy(),
        ReplacementPolicy.FIFO => new FifoStrategy(),
        ReplacementPolicy.Random => new RandomStrategy(),
        ReplacementPolicy.LFU => new LfuStrategy(),
        ReplacementPolicy.MRU => new MruStrategy(),
        ReplacementPolicy.RoundRobin => new RoundRobinStrategy(),
        ReplacementPolicy.SecondChance => new SecondChanceStrategy(),
        ReplacementPolicy.LFRU => new LfruStrategy(),
        _ => new LruStrategy()
    };

    public static string GetDescription(ReplacementPolicy policy) => Create(policy).Description;
}
