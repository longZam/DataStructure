namespace DataStructure;


public sealed class Queue<T>
{
    private T[] array;
    private int front, rear, capacity;

    public bool Full => (rear + 1) % (capacity + 1) == front;
    public bool Empty => front == rear;


    public Queue(int capacity)
    {
        this.array = new T[capacity + 1];
        this.capacity = capacity;
        this.front = 0;
        this.rear = 0;
    }

    public void Enqueue(T item)
    {
        if (Full)
        {
            capacity *= 2;
            Array.Resize(ref array, capacity + 1);
        }
        
        rear = (rear + 1) % (capacity + 1);
        array[rear] = item;
    }

    public T Dequeue()
    {
        front = (front + 1) % (capacity + 1);
        return array[front];
    }

    public T Peek()
    {
        return array[(front + 1) % (capacity + 1)];
    }
}
