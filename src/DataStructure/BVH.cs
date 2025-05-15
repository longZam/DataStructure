using System.Diagnostics;
using System.Numerics;

namespace DataStructure;


public readonly struct Bounds(Vector3 center, Vector3 size)
{
    public readonly Vector3 Center = center;
    public readonly Vector3 Size = size;
    public Vector3 Extends => Size * 0.5f;
    public Vector3 Min => Center - Extends;
    public Vector3 Max => Center + Extends;


    public static Bounds Union(Bounds a, Bounds b)
    {
        var min = Vector3.Min(a.Min, b.Min);
        var max = Vector3.Max(a.Max, b.Max);
        var center = (max + min) * 0.5f;
        var size = max - min;

        return new Bounds(center, size);
    }

    public bool Contains(Bounds other)
    {
        return Min.X <= other.Min.X && other.Max.X <= Max.X &&
                Min.Y <= other.Min.Y && other.Max.Y <= Max.Y &&
                Min.Z <= other.Min.Z && other.Max.Z <= Max.Z;
    }

    public bool Overlaps(Bounds other)
    {
        return !(Max.X < other.Min.X || Min.X > other.Max.X ||
                 Max.Y < other.Min.Y || Min.Y > other.Max.Y ||
                 Max.Z < other.Min.Z || Min.Z > other.Max.Z);
    }

    public float SurfaceArea()
    {
        return Size.X * Size.Y + Size.X * Size.Z + Size.Y * Size.Z * 2;
    }
}

public sealed class BVH<T> where T : notnull
{

    private struct Node
    {
        public Bounds bounds;
        public T item;
        public int parent, left, right;
        public bool isLeaf;
    }


    private const int NULL = -1;

    private readonly Dictionary<T, int> leafNodeMap;
    private readonly System.Collections.Generic.Queue<int> freeMemoryOffsets;

    private Node[] nodes;
    private int root;

    public int Capacity { get; private set; }


    public BVH(int capacity)
    {
        this.leafNodeMap = new Dictionary<T, int>(capacity);
        this.freeMemoryOffsets = new(Enumerable.Range(0, capacity));
        this.nodes = new Node[capacity];
        this.Capacity = capacity;
        this.root = NULL;
    }

    public void Traversal(Func<Bounds, bool> predicate, Action<T> callback)
    {
        if (root == -1)
            return;
        
        Traversal(predicate, callback, root);
    }

    public int cnt = 0;

    private void Traversal(Func<Bounds, bool> predicate, Action<T> callback, int current)
    {   
        cnt += 1;
        if (!predicate(nodes[current].bounds))
        {
            return;
        }

        if (nodes[current].left != -1 && nodes[current].right != -1)
        {
            Traversal(predicate, callback, nodes[current].left);
            Traversal(predicate, callback, nodes[current].right);
        }
        else
        {
            callback(nodes[current].item);
        }
    }

    public void BottomUp()
    {
        var queue = new System.Collections.Generic.Queue<int>(Capacity / 2);
        // 1. 모든 interior 노드 제거
        
        foreach (var leaf in leafNodeMap.Values)
            RemoveInteriors(nodes[leaf].parent);

        // Z-Order 정렬해서 큐에 삽입
        var zOrder = leafNodeMap.Values.OrderBy(i => Morton3D.Encode(nodes[i].bounds.Center));

        foreach (var leaf in zOrder)
            queue.Enqueue(leaf);

        while (queue.Count > 1)
        {
            var left = queue.Dequeue();
            var right = queue.Dequeue();
            var interior = Allocate();

            nodes[interior] = new()
            {
                bounds = Bounds.Union(nodes[left].bounds, nodes[right].bounds),
                left = left,
                right = right,
                isLeaf = false,
                parent = NULL
            };

            nodes[left].parent = interior;
            nodes[right].parent = interior;

            queue.Enqueue(interior);
        }

        root = queue.Dequeue();
    }

    private void RemoveInteriors(int parent)
    {
        if (parent == NULL)
            return;
        
        nodes[nodes[parent].left].parent = NULL;
        nodes[nodes[parent].right].parent = NULL;
        RemoveInteriors(nodes[parent].parent);
        Free(parent);
    }

    public bool Insert(T item, Bounds bounds)
    {
        if (leafNodeMap.ContainsKey(item))
            return false;
        
        var leaf = Allocate();
        nodes[leaf] = new()
        {
            bounds = bounds,
            isLeaf = true,
            item = item,
            left = NULL,
            parent = NULL,
            right = NULL,
        };

        leafNodeMap.Add(item, leaf);
        
        var slibing = PickBest(bounds);

        if (slibing == NULL)
        {
            root = leaf;
            return true;
        }

        var newInterior = Allocate();

        nodes[newInterior] = new()
        {
            left = slibing,
            right = leaf,
            parent = nodes[slibing].parent,
            isLeaf = false
        };

        nodes[leaf].parent = newInterior;
        nodes[slibing].parent = newInterior;
        
        var ancestor = nodes[newInterior].parent;

        if (ancestor == NULL)
            root = newInterior;
        else if (nodes[ancestor].left == slibing)
            nodes[ancestor].left = newInterior;
        else
            nodes[ancestor].right = newInterior;

        Refit(newInterior);

        return true;
    }

    public bool Remove(T item)
    {
        if (!leafNodeMap.Remove(item, out var leaf))
            return false;
        
        var parent = nodes[leaf].parent;

        if (parent == NULL)
        {
            root = NULL;
        }
        else
        {
            // 형제 노드와 부모의 부모 간 연결
            // 부모의 부모가 NULL이면, 루트가 되어야 한다는 뜻
            var ancestor = nodes[parent].parent;
            var slibing = nodes[parent].left == leaf ? nodes[parent].right : nodes[parent].left;
            nodes[slibing].parent = ancestor;
            
            if (ancestor == NULL)
                root = slibing;
            else if (nodes[ancestor].left == parent)
                nodes[ancestor].left = slibing;
            else
                nodes[ancestor].right = slibing;

            Free(parent);
        }

        Free(leaf);
        return true;
    }

    private int PickBest(Bounds bounds)
    {
        if (root == NULL)
            return NULL;
        
        var current = root;

        while (!nodes[current].isLeaf)
        {
            var left = nodes[current].left;
            var right = nodes[current].right;

            var lu = Bounds.Union(bounds, nodes[left].bounds).SurfaceArea();
            var ru = Bounds.Union(bounds, nodes[right].bounds).SurfaceArea();

            current = lu < ru ? left : right;
        }

        return current;
    }

    private int Allocate()
    {
        if (!freeMemoryOffsets.TryDequeue(out var offset))
        {
            offset = Capacity;

            for (int i = Capacity + 1; i < Capacity * 2; ++i)
                freeMemoryOffsets.Enqueue(i);
            
            Capacity *= 2;
            Array.Resize(ref nodes, Capacity);
        }

        return offset;
    }

    private void Free(int offset)
    {
#if DEBUG
        nodes[offset] = new();
#endif

        freeMemoryOffsets.Enqueue(offset);
    }

    private void Refit(int current)
    {
        while (current != -1)
        {
            var currNode = nodes[current];
            var left = nodes[currNode.left].bounds;
            var right = nodes[currNode.right].bounds;
            var union = Bounds.Union(left, right);

            if (currNode.bounds.Contains(union))
                break;

            nodes[current].bounds = union;
            current = nodes[current].parent;
        }
    }
}