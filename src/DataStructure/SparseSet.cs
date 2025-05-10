using System.Collections;

namespace DataStructure;



public sealed class SparseSet<T> : IEnumerable<KeyValuePair<int, T>>
{
    private readonly int[] sparse;
    private readonly int[] dense;
    private readonly T[] items;
    private readonly int capacity;

    public int Count { get; private set; }


    public SparseSet(int capacity)
    {
        this.sparse = new int[capacity];
        this.dense = new int[capacity];
        this.items = new T[capacity];
        this.capacity = capacity;
    }

    public bool Add(int key, T item)
    {
        // 이미 등록됨
        if (ContainsKey(key))
            return false;
        
        sparse[key] = Count;
        dense[Count] = key;
        items[Count] = item;
        Count += 1;
        return true;
    }

    public bool Remove(int key)
    {
        // 등록되지 않음
        if (!ContainsKey(key))
            return false;
        
        Count -= 1;

        var last = dense[Count];
        (dense[Count], dense[sparse[key]]) = (dense[sparse[key]], dense[Count]);
        (items[Count], items[sparse[key]]) = (items[sparse[key]], items[Count]);
        sparse[last] = sparse[key];
        
        return true;
    }

    public bool ContainsKey(int key)
    {
        return 0 <= key && key < capacity && // key 경계 검사
               dense[sparse[key]] == key && // value의 유효성 검사
               sparse[key] < Count; // 삭제된 value인지 검사
    }

    public T Get(int key)
    {
        if (!ContainsKey(key))
            throw new Exception();

        return items[sparse[key]];
    }

    public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
    {
        for (int i = 0; i < capacity; i++)
        {
            if (ContainsKey(i))
            {
                yield return new KeyValuePair<int, T>(i, items[sparse[i]]);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}