using Snet.Core.cache.share;

public class ShareCacheFreeListAllocator
{
    private readonly SortedDictionary<long, ShareCacheFree> freeBlocks = new();
    private long currentPosition = 0;

    /// <summary>
    /// 分配内存（近似最佳适配）
    /// </summary>
    public long Allocate(int size)
    {
        ShareCacheFree? bestFit = null;
        long bestKey = -1;

        foreach (var kv in freeBlocks)
        {
            var block = kv.Value;
            if (block.Length >= size)
            {
                if (bestFit == null || block.Length < bestFit.Length)
                {
                    bestFit = block;
                    bestKey = kv.Key;
                }
            }
        }

        if (bestFit != null)
        {
            long pos = bestFit.Position;
            if (bestFit.Length == size)
            {
                freeBlocks.Remove(bestKey);
            }
            else
            {
                bestFit.Position += size;
                bestFit.Length -= size;
            }
            return pos;
        }

        // 没找到合适的，直接分配新空间
        long newPos = currentPosition;
        currentPosition += size;
        return newPos;
    }

    /// <summary>
    /// 释放并合并
    /// </summary>
    public void Free(long position, int size)
    {
        var block = new ShareCacheFree { Position = position, Length = size };
        freeBlocks[position] = block;

        // 尝试和前后块合并
        Merge(block);
    }

    private void Merge(ShareCacheFree block)
    {
        // 找前一个块
        if (freeBlocks.TryGetValue(block.Position - 1, out var prev) &&
            prev.Position + prev.Length == block.Position)
        {
            block.Position = prev.Position;
            block.Length += prev.Length;
            freeBlocks.Remove(prev.Position);
        }

        // 找后一个块
        if (freeBlocks.TryGetValue(block.Position + block.Length, out var next))
        {
            block.Length += next.Length;
            freeBlocks.Remove(next.Position);
        }

        freeBlocks[block.Position] = block;
    }

    public void Reset()
    {
        freeBlocks.Clear();
        currentPosition = 0;
    }
}
