namespace Shared;

public static class GcInfo
{
    public static string GetTotalMemoryFormatted()
    {
        var totalMb = GC.GetTotalMemory(false) / 1024d / 1024;

        return $"Heap: {totalMb:0.00} MB";
    }

    public static string GetCollectionCount()
        => $"Collections: " +
           $"G0={GC.CollectionCount(0)}, " +
           $"G1={GC.CollectionCount(1)}, " +
           $"G2={GC.CollectionCount(2)}";
}