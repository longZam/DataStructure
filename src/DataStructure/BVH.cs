using System.Numerics;
using System.Runtime.InteropServices;

namespace DataStructure;


public readonly struct Bounds(Vector3 center, float radius)
{
    public readonly Vector3 Center = center;
    public readonly float Radius = radius;
    public Vector3 Min => Center - new Vector3(Radius, Radius, Radius);
    public Vector3 Max => Center + new Vector3(Radius, Radius, Radius);


    public static Bounds Union(Bounds a, Bounds b)
    {
        var dv = b.Center - a.Center;
        var d = dv.Length();
        
        // 한 원이 다른 원을 완전히 포함하면
        if (MathF.Abs(a.Radius - b.Radius) > d)
            return a.Radius > b.Radius ? a : b;

        var r = (d + a.Radius + b.Radius) / 2;
        var ratio = (r - a.Radius) / d;
        var c = new Vector3(
            a.Center.X + dv.X * ratio,
            a.Center.Y + dv.Y * ratio,
            a.Center.Z + dv.Z * ratio
        );

        return new(c, r);
    }

    public bool Contains(Bounds other)
    {
        return Radius - other.Radius >= Vector3.Distance(Center, other.Center);
    }
}

public sealed class BVH<T>
    where T : struct
{
    private class Node(T item = default)
    {
        public readonly T Item = item;
        public Bounds bounds;
        public Node? left, right, parent;
    }


    private readonly Dictionary<T, Node> leafNodeMap;
    private Node? root;


    public BVH()
    {
        this.root = null;
        this.leafNodeMap = new Dictionary<T, Node>();
    }

    public void Traversal(Func<Bounds, bool> predicate, Action<T> callback)
    {
        if (root == null)
            return;
        
        DFS(predicate, callback, root);
    }

    private static void DFS(Func<Bounds, bool> predicate, Action<T> callback, Node current)
    {
        if (!predicate(current.bounds))
            return;

        if (current.left == null || current.right == null)
            callback(current.Item);
        else
        {
            DFS(predicate, callback, current.right);
            DFS(predicate, callback, current.left);
        }
    }

    public void Add(T item, Bounds bounds)
    {
        var newLeaf = new Node(item)
        {
            bounds = bounds
        };

        leafNodeMap.Add(item, newLeaf);
        Insert(newLeaf);
    }

    private void Insert(Node node)
    {
        if (root == null)
        {
            root = node;
            return;
        }
        
        var current = root;

        while (current.left != null && current.right != null)
        {
            // SAH 표면적 휴리스틱
            // 구의 표면적은 반지름과 비례함을 이용

            var lb = Bounds.Union(node.bounds, current.left.bounds);
            var rb = Bounds.Union(node.bounds, current.right.bounds);
            current = lb.Radius < rb.Radius ? current.left : current.right;    
        }

        // 현재 노드는 leaf node임
        if (current == root)
        {
            root = new Node();
            root.left = current;
            root.right = node;
            current.parent = root;
            node.parent = root;

            RefitBounds(root);
        }
        else
        {
            // parent는 반드시 internal임. root가 아닌데 parent가 없으면 오류임
            var parent = current.parent ?? throw new Exception();
            var left = parent.left == current;
            var newInternal = new Node();

            if (left)
                parent.left = newInternal;
            else
                parent.right = newInternal;
            
            newInternal.left = left ? current : node;
            newInternal.right = left ? node : current;
            newInternal.parent = parent;
            current.parent = newInternal;
            node.parent = newInternal;

            RefitBounds(newInternal);
        }
    }

    public bool Remove(T item)
    {
        if (!leafNodeMap.Remove(item, out var node))
            return false;
        
        if (node == root)
        {
            root = null;
            return true;
        }

        var parent = node.parent ?? throw new Exception();
        var left = parent.left == node;
        var other = (left ? parent.right : parent.left) ?? throw new Exception();
        other.parent = parent.parent;

        // root
        if (parent.parent == null)
            root = other;
        else
        {
            if (parent.parent.left == parent)
                parent.parent.left = other;
            else
                parent.parent.right = other;

            RefitBounds(parent.parent);
        }

        return true;
    }

    public void Move(T item, Bounds newBounds)
    {
        var leaf = leafNodeMap[item];
        leaf.bounds = newBounds;
        var parent = leaf.parent ?? throw new Exception();

        if (parent.bounds.Contains(leaf.bounds))
            return;
        
        RefitBounds(parent);
    }

    // 노드 최적화
    public void Optimize()
    {
        if (root == null)
            return;
        
        RecursiveUnlink(root);
        root = null;

        foreach (var node in leafNodeMap.Values)
            Insert(node);
    }

    private static void RecursiveUnlink(Node current)
    {
        current.parent = null;

        if (current.left == null || current.right == null)
            return;

        RecursiveUnlink(current.left);
        RecursiveUnlink(current.right);

        current.left = null;
        current.right = null;
    }

    private static void RefitBounds(Node start)
    {
        Node? current = start;

        while (current?.left != null && current?.right != null)
        {
            var bounds = Bounds.Union(current.left.bounds, current.right.bounds);
            current.bounds = new Bounds(bounds.Center, bounds.Radius * 1.1f);
            current = current.parent;
        }
    }
}