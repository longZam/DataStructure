namespace DataStructure;


public sealed class IdGenerator
{
    private readonly SortedSet<uint> disposedIds;
    private uint recent;
    

    public IdGenerator()
    {
        this.disposedIds = [];
        this.recent = 0;
    }

    public uint Generate()
    {
        if (disposedIds.Count > 0)
        {
            var result = disposedIds.Min;
            disposedIds.Remove(result);
            return result;
        }

        return recent++;
    }

    public bool DisposeId(uint id)
    {
        if (recent <= id)
            return false;
        
        return disposedIds.Add(id);
    }
}