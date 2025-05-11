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

public sealed class BVH<T> where T : struct
{
    private struct Node
    {
        public enum Type
        {
            None,
            Leaf,
            Interior
        }

        public Bounds bounds;
        public T item;
        public Type type;
        public int height;
    }

    private readonly Dictionary<T, int> leafNodes;
    private Node[] nodes;

    public int Capacity { get; private set; }
    public int Count { get; private set; }


    public BVH(int capacity)
    {
        this.leafNodes = new Dictionary<T, int>(capacity);
        this.nodes = new Node[capacity];
        this.Capacity = capacity;
        this.Count = 0;
    }

    public void Insert(T item, Bounds bounds)
    {
        Debug.Assert(!leafNodes.ContainsKey(item));
        Count += 1;
        leafNodes.Add(item, 0);

        if (nodes[0].type == Node.Type.None)
        {
            nodes[0] = new Node
            {
                bounds = bounds,
                item = item,
                type = Node.Type.Leaf
            };
            leafNodes[item] = 0;
            return;
        }

        var current = 0;

        while (nodes[current].type == Node.Type.Interior)
        {
            // 표면적 휴리스틱
            var lb = Bounds.Union(bounds, nodes[current * 2 + 1].bounds).SurfaceArea() * nodes[current * 2 + 1].height * 5;
            var rb = Bounds.Union(bounds, nodes[current * 2 + 2].bounds).SurfaceArea() * nodes[current * 2 + 2].height * 5;

            current = (current * 2) + (lb < rb ? 1 : 2);
        }

        if (Capacity <= current * 2 + 2)
        {
            Capacity *= 2;
            Array.Resize(ref nodes, Capacity);
        }

        nodes[current * 2 + 1] = nodes[current];
        nodes[current * 2 + 2] = new Node
        {
            bounds = bounds,
            item = item,
            type = Node.Type.Leaf
        };
        nodes[current].type = Node.Type.Interior;
        Refit(current);

        leafNodes[nodes[current * 2 + 1].item] = current * 2 + 1;
        leafNodes[item] = current * 2 + 2;
    }

    public bool Remove(T item)
    {
        if (!leafNodes.Remove(item, out var index))
            return false;

        nodes[index].type = Node.Type.None;

        if (index != 0)
            RebuildSubtree((index - 1) / 2);

        return true;
    }

    private readonly System.Collections.Generic.Queue<Node> rebuildQueue = new System.Collections.Generic.Queue<Node>(8192);
    private readonly System.Collections.Generic.Stack<int> rebuildStack = new System.Collections.Generic.Stack<int>(8192);

    private void RebuildSubtree(int subtree)
    {
        // 서브 트리 내 모든 leaf 노드 수집
        GetLeafsFromSubtree(subtree);

        // 서브 트리 루트만 남기고 모두 제거
        RemoveSubtree(subtree);

        while (rebuildQueue.TryDequeue(out var node))
        {
            var current = subtree;

            if (nodes[current].type == Node.Type.None)
            {
                nodes[current] = node;
                leafNodes[node.item] = current;
                continue;
            }

            while (nodes[current].type == Node.Type.Interior)
            {
                // 표면적 휴리스틱
                var lb = Bounds.Union(node.bounds, nodes[current * 2 + 1].bounds).SurfaceArea() * nodes[current * 2 + 1].height * 5;
                var rb = Bounds.Union(node.bounds, nodes[current * 2 + 2].bounds).SurfaceArea() * nodes[current * 2 + 2].height * 5;

                current = (current * 2) + (lb < rb ? 1 : 2);
            }

            nodes[current * 2 + 1] = nodes[current];
            nodes[current * 2 + 2] = node;
            nodes[current].type = Node.Type.Interior;
            Refit(current);

            leafNodes[nodes[current * 2 + 1].item] = current * 2 + 1;
            leafNodes[node.item] = current * 2 + 2;
        }
    }

    private void GetLeafsFromSubtree(int subtree)
    {
        if (nodes[subtree].type == Node.Type.Interior)
        {
            GetLeafsFromSubtree(subtree * 2 + 1);
            GetLeafsFromSubtree(subtree * 2 + 2);
        }
        else if (nodes[subtree].type == Node.Type.Leaf)
        {
            rebuildQueue.Enqueue(nodes[subtree]);
        }
    }

    private void RemoveSubtree(int subtree)
    {
        if (nodes[subtree].type == Node.Type.Interior)
        {
            RemoveSubtree(subtree * 2 + 1);
            RemoveSubtree(subtree * 2 + 2);
        }

        nodes[subtree].type = Node.Type.None;
        nodes[subtree].height = 0;
    }

    public void Traversal(Func<Bounds, bool> predicate, Action<T> callback, int current = 0)
    {
        if (nodes[current].type == Node.Type.None)
            return;

        if (!predicate(nodes[current].bounds))
            return;

        if (nodes[current].type == Node.Type.Interior)
        {
            Traversal(predicate, callback, current * 2 + 1);
            Traversal(predicate, callback, current * 2 + 2);
        }
        else
        {
            callback(nodes[current].item);
        }
    }

    private void Refit(int current)
    {
        current = current * 2 + 1;

        while (current != 0)
        {
            current = (current - 1) / 2;
            nodes[current].bounds = Bounds.Union(nodes[current * 2 + 1].bounds, nodes[current * 2 + 2].bounds);
            nodes[current].height = Math.Max(nodes[current * 2 + 1].height, nodes[current * 2 + 2].height) + 1;
        }
    }
}