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
    }


    private readonly Dictionary<T, int> leafNodeIndexMap;
    private Node[] nodeArray;
    private int count;
    private int root;

    public int Capacity { get; private set; }


    public BVH(int capacity)
    {
        this.leafNodeIndexMap = new Dictionary<T, int>(capacity);
        this.nodeArray = new Node[capacity];
        this.Capacity = capacity;
        this.count = 0;
        this.root = -1;
    }

    public void Traversal(Func<Bounds, bool> predicate, Action<T> callback)
    {
        if (root == -1)
            return;
        
        Traversal(predicate, callback, root);
    }

    private void Traversal(Func<Bounds, bool> predicate, Action<T> callback, int current)
    {   
        if (!predicate(nodeArray[current].bounds))
        {
            return;
        }

        if (nodeArray[current].left != -1 && nodeArray[current].right != -1)
        {
            Traversal(predicate, callback, nodeArray[current].left);
            Traversal(predicate, callback, nodeArray[current].right);
        }
        else
        {
            callback(nodeArray[current].item);
        }
    }

    public bool Insert(T item, Bounds bounds)
    {
        if (leafNodeIndexMap.ContainsKey(item))
            return false;

        var newLeafNode = CreateNode();
        nodeArray[newLeafNode].item = item;
        leafNodeIndexMap.Add(item, newLeafNode);
        nodeArray[newLeafNode].bounds = bounds;
        
        // insert root
        if (newLeafNode == 0)
        {
            root = 0;
            return true;
        }
        
        int current = root;

        while (current < count)
        {
            // 해당 노드가 leaf 노드면
            if (nodeArray[current].left == -1 && nodeArray[current].right == -1)
                break;
            
            // 해당 노드가 interior 노드면
            var lb = Bounds.Union(bounds, nodeArray[nodeArray[current].left].bounds).SurfaceArea();
            var rb = Bounds.Union(bounds, nodeArray[nodeArray[current].right].bounds).SurfaceArea();

            current = lb < rb ? nodeArray[current].left : nodeArray[current].right;
        }

        // 해당 leaf 노드와 새로운 interior 노드를 구성해야 함.
        
        var newInteriorNode = CreateNode(current,
                                         newLeafNode,
                                         false);
                
        var parent = nodeArray[current].parent;
        nodeArray[newInteriorNode].parent = parent;
        nodeArray[current].parent = newInteriorNode;
        nodeArray[newLeafNode].parent = newInteriorNode;

        if (parent == -1)
            root = newInteriorNode;
        else if (nodeArray[parent].left == current)
            nodeArray[parent].left = newInteriorNode;
        else
            nodeArray[parent].right = newInteriorNode;

        Refit(newInteriorNode);
        
        return true;
    }

    private int CreateNode(int left = -1, int right = -1, bool leaf = true)
    {
        if (Capacity <= count)
        {
            Capacity *= 2;
            Array.Resize(ref nodeArray, Capacity);
        }

        nodeArray[count] = new()
        {
            parent = -1,
            left = left,
            right = right
        };

        return count++;
    }

    private void Refit(int current)
    {
        while (current != -1)
        {
            var currNode = nodeArray[current];
            var left = nodeArray[currNode.left].bounds;
            var right = nodeArray[currNode.right].bounds;
            var union = Bounds.Union(left, right);

            if (currNode.bounds.Contains(union))
                break;

            nodeArray[current].bounds = union;
            current = nodeArray[current].parent;
        }
    }
}