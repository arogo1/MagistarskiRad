using Common;
using Common.Interfaces;

namespace Heaps;

/// <summary>
/// Fibonacci gomila (min-heap): Insert O(1) amort., DecreaseKey O(1) amort., ExtractMin O(log n) amort., Meld O(1).
/// </summary>
public sealed class FibonacciHeap<T> : IPriorityQueue<T>
{
    private Node? _min;          // pokazivač na minimalni korijen
    private int _count;          // broj čvorova
    private readonly PQCounters? _counters;

    private sealed class Node
    {
        public T Item = default!;
        public double Key;
        public int Degree;       // broj djece
        public bool Mark;        // za kaskadne rezove

        public Node? Parent;     // roditelj (null za korijen)
        public Node? Child;      // jedno od djece (kružna dvostr. lista)
        public Node? Left;       // lijevi brat (kružna)
        public Node? Right;      // desni brat (kružna)

        public PQHandle<T>? Handle; // vanjska ručka ka ovom čvoru
    }

    public FibonacciHeap(PQCounters? counters = null) => _counters = counters;

    public int Count => _count;
    public bool IsEmpty => _count == 0;

    public PQHandle<T> Insert(T item, double priority)
    {
        Inc(c => c.Inserts++);

        var x = new Node { Item = item, Key = priority };
        var h = new PQHandle<T>(x);
        x.Handle = h;
        x.Left = x.Right = x;

        InsertIntoRootList(ref _min, x);
        _count++;
        return h;
    }

    public (T Item, double Priority) ExtractMin()
    {
        if (_min is null) throw new InvalidOperationException("Heap is empty");
        Inc(c => c.ExtractMins++);

        var z = _min;

        // 1) Dodaj svu djecu od z u root-listu
        if (z.Child is not null)
        {
            List<Node> children = IterateList(z.Child).ToList();
            int k = 0;
            while (k < children.Count)
            {
                var x = children[k];
                x.Parent = null;
                RemoveFromList(x);
                InsertIntoRootList(ref _min, x);
                k++;
            }
            z.Child = null;
            z.Degree = 0;
        }

        // 2) Ukloni z iz root-liste i konsoliduj
        if (ReferenceEquals(z, z.Right))
        {
            _min = null;
        }
        else
        {
            var right = z.Right!;
            RemoveFromList(z);
            _min = right; // privremeno
            Consolidate();
        }

        // 3) Održavanje brojila i ručke
        _count--;
        if (z.Handle is not null) z.Handle.NodeRef = null;

        return (z.Item, z.Key);
    }

    public void DecreaseKey(PQHandle<T> handle, double newPriority)
    {
        if (handle.NodeRef is not Node x) throw new InvalidOperationException("Invalid handle");
        if (newPriority > x.Key) throw new ArgumentException("newPriority greater than current key");

        Inc(c => c.DecreaseKeys++);

        x.Key = newPriority;
        var y = x.Parent;

        // ako je narušeno heap-svojstvo: odsijeci x i kaskadno odsijeci pretke po potrebi
        if (y is not null && x.Key < y.Key)
        {
            Cut(x, y);
            CascadingCut(y);
        }

        if (_min is null || x.Key < _min.Key)
            _min = x;
    }

    public void Clear()
    {
        _min = null;
        _count = 0;
    }

    /// <summary>
    /// Meld (union) u O(1) amort.: konkatenacija root-listi i izbor novog minimuma.
    /// </summary>
    public void Meld(FibonacciHeap<T> other)
    {
        Inc(c => c.Melds++);
        if (other._min is null) return;

        if (_min is null)
        {
            _min = other._min;
            _count = other._count;
            other._min = null;
            other._count = 0;
            return;
        }

        // konkatenacija: ubaci other's min desno od this min
        var a = _min;
        var b = other._min;

        var aRight = a.Right!;
        var bLeft  = b.Left!;

        a.Right = b;
        b.Left  = a;
        aRight.Left = bLeft;
        bLeft.Right = aRight;

        if (b.Key < a.Key) _min = b;

        _count += other._count;
        other._min = null;
        other._count = 0;
    }
    private void Inc(Action<PQCounters> a)
    {
        if (_counters is not null) a(_counters);
    }

    private static void InsertIntoRootList(ref Node? min, Node x)
    {
        if (min is null)
        {
            x.Left = x.Right = x;
            min = x;
            return;
        }
        // ubaci x desno od min
        x.Left = min;
        x.Right = min.Right;
        min.Right!.Left = x;
        min.Right = x;
        if (x.Key < min.Key) min = x;
    }

    private static void RemoveFromList(Node x)
    {
        x.Left!.Right = x.Right;
        x.Right!.Left = x.Left;
        x.Left = x.Right = x; // izoluj
    }

    private static void AddChild(Node parent, Node child)
    {
        // dodaj dijete u listu djece roditelja
        if (parent.Child is null)
        {
            child.Left = child.Right = child;
            parent.Child = child;
        }
        else
        {
            var c = parent.Child;
            child.Left = c;
            child.Right = c!.Right;
            c.Right!.Left = child;
            c.Right = child;
        }
        child.Parent = parent;
        child.Mark = false;
        parent.Degree++;
    }

    private static void LinkTrees(Node y, Node x)
    {
        // ukloni y iz root-liste i učini ga djetetom od x
        RemoveFromList(y);
        AddChild(x, y);
    }

    private static IEnumerable<Node> IterateList(Node start)
    {
        var curr = start;
        do { yield return curr; curr = curr.Right!; } while (!ReferenceEquals(curr, start));
    }

    private static int UpperBoundDegree(int n)
    {
        // gruba granica: log2(n)*2 + margin
        if (n < 1) return 0;
        return (int)Math.Floor(Math.Log(n, 2) * 2.0) + 5;
    }
    private void Consolidate()
    {
        if (_min is null) return;

        int maxD = UpperBoundDegree(_count);
        var A = new Node?[maxD + 1];

        // Snapshot trenutnih korijenova (da ne modifikujemo tokom iteracije)
        List<Node> roots = IterateList(_min).ToList();

        int i = 0;
        while (i < roots.Count)
        {
            var x = roots[i];
            int d = x.Degree;

            while (A[d] is not null)
            {
                var y = A[d]!;
                if (y.Key < x.Key) (x, y) = (y, x); // x ima manji ključ
                LinkTrees(y, x);
                A[d] = null;
                d++;
            }

            A[d] = x;
            i++;
        }

        // Rekonstruiši root-listu i nađi novi min
        _min = null;

        int j = 0;
        while (j < A.Length)
        {
            var x = A[j];
            if (x is not null)
            {
                x.Parent = null;
                x.Left = x.Right = x;
                InsertIntoRootList(ref _min, x);
            }
            j++;
        }
    }

    private void Cut(Node x, Node y)
    {
        // ukloni x iz liste djece od y
        if (y.Child == x)
        {
            if (!ReferenceEquals(x.Right, x)) y.Child = x.Right;
            else y.Child = null;
        }

        RemoveFromList(x);
        y.Degree--;

        // dodaj x u root-listu
        x.Parent = null;
        x.Mark = false;
        InsertIntoRootList(ref _min, x);
    }

    private void CascadingCut(Node y)
    {
        var z = y.Parent;
        if (z is not null)
        {
            if (!y.Mark) y.Mark = true;
            else
            {
                Cut(y, z);
                CascadingCut(z);
            }
        }
    }
}
