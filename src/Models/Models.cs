namespace MemoryHierarchySimulator.Models;

public class CacheBlock
{
    public long Tag { get; set; }
    public bool IsValid { get; set; }
    public bool IsDirty { get; set; }
    public byte[] Data { get; set; }
    public long LastAccessTime { get; set; }
    public int AccessCount { get; set; }
    public long InsertionTime { get; set; }
    public bool ReferenceBit { get; set; }

    public CacheBlock(int blockSize)
    {
        Data = new byte[blockSize];
        Reset();
    }

    public void Reset()
    {
        IsValid = false;
        IsDirty = false;
        Tag = -1;
        LastAccessTime = 0;
        AccessCount = 0;
        InsertionTime = 0;
        ReferenceBit = false;
        Array.Clear(Data, 0, Data.Length);
    }
}

public class CacheSet
{
    public CacheBlock[] Blocks { get; }
    public int CircularPointer { get; set; }

    public CacheSet(int associativity, int blockSize)
    {
        Blocks = new CacheBlock[associativity];
        for (int i = 0; i < associativity; i++)
            Blocks[i] = new CacheBlock(blockSize);
        CircularPointer = 0;
    }

    public CacheBlock? FindBlock(long tag)
    {
        foreach (var block in Blocks)
            if (block.IsValid && block.Tag == tag)
                return block;
        return null;
    }

    public CacheBlock? FindEmptyBlock()
    {
        foreach (var block in Blocks)
            if (!block.IsValid)
                return block;
        return null;
    }

    public void Reset()
    {
        foreach (var block in Blocks)
            block.Reset();
        CircularPointer = 0;
    }
}

public enum ReplacementPolicy { LRU, FIFO, Random, LFU, MRU, RoundRobin, SecondChance, LFRU }
public enum AccessType { Read, Write }
public enum AccessPattern { Sequential, Random, Locality, Stride, Loop, Mixed, Manual }

public class MemoryAccess
{
    public long Address { get; set; }
    public AccessType Type { get; set; }
    public long Timestamp { get; set; }

    public MemoryAccess(long address, AccessType type, long timestamp)
    {
        Address = address;
        Type = type;
        Timestamp = timestamp;
    }
}

public class AccessResult
{
    public long Address { get; set; }
    public AccessType AccessType { get; set; }
    public bool IsHit { get; set; }
    public int HitLevel { get; set; }
    public int TotalLatency { get; set; }
    public string Details { get; set; } = string.Empty;
    public string HitMissText => IsHit ? "Hit" : "Miss";
    public string AddressHex => $"0x{Address:X8}";
}

public class CacheLevelConfig
{
    public string Name { get; set; } = string.Empty;
    public int TotalSize { get; set; }
    public int BlockSize { get; set; }
    public int Associativity { get; set; }
    public int AccessLatency { get; set; }
    public int NumberOfSets => TotalSize / (BlockSize * Associativity);
}

public class SimulationStatistics
{
    public int TotalAccesses { get; set; }
    public int ReadAccesses { get; set; }
    public int WriteAccesses { get; set; }
    public List<CacheLevelStatistics> CacheLevelStats { get; set; } = new();
    public int MainMemoryAccesses { get; set; }
    public int SecondaryStorageAccesses { get; set; }
    public long TotalLatency { get; set; }
    public double AverageLatency => TotalAccesses > 0 ? (double)TotalLatency / TotalAccesses : 0;

    public void Reset()
    {
        TotalAccesses = 0;
        ReadAccesses = 0;
        WriteAccesses = 0;
        MainMemoryAccesses = 0;
        SecondaryStorageAccesses = 0;
        TotalLatency = 0;
        CacheLevelStats.Clear();
    }
}

public class CacheLevelStatistics
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Hits { get; set; }
    public int Misses { get; set; }
    public int TotalAccesses => Hits + Misses;
    public double HitRate => TotalAccesses > 0 ? (double)Hits / TotalAccesses * 100 : 0;
    public double MissRate => TotalAccesses > 0 ? (double)Misses / TotalAccesses * 100 : 0;
}
