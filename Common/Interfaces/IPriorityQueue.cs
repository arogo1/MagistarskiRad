namespace Common.Interfaces;

public interface IPriorityQueue<T>
{
    int Count { get; }
    bool IsEmpty { get; }

    /// <summary>Dodaje element sa zadatim prioritetom (manji = bolji).</summary>
    PQHandle<T> Insert(T item, double priority);

    /// <summary>Vraća i uklanja element sa najmanjim prioritetom.</summary>
    (T Item, double Priority) ExtractMin();

    /// <summary>Smanjuje prioritet postojećeg elementa.</summary>
    void DecreaseKey(PQHandle<T> handle, double newPriority);

    void Clear();
}