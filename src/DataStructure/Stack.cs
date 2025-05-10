namespace DataStructure;

public sealed class Stack<T>
{
    private T[] array;

    public int Count { get; private set; }


    public Stack(int capacity)
    {
        this.array = new T[capacity];
        this.Count = 0;
    }

    public void Push(T item)
    {
        if (array.Length - Count == 1)
        {
            var newArray = new T[array.Length * 2];
            array.CopyTo(newArray, 0);
            array = newArray;
        }

        array[Count++] = item;
    }

    public T Pop()
    {
        return array[--Count];
    }

    public T Peek()
    {
        return array[Count - 1];
    }
}
