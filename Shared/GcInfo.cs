namespace Shared;

public static class GcInfo
{
    public static string GetTotalMemoryFormatted()
    {
        var totalMb = GC.GetTotalMemory(false) / 1024d / 1024;

        return $"{totalMb:0.00} MB";
    }
}