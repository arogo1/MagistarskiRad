using Common;
using Common.Interfaces;

namespace Heaps;

/// <summary>
/// Binomna gomila (min-heap) sa DecreaseKey i Meld.
/// Insert: O(1) amort., ExtractMin: O(log n), DecreaseKey: O(log n), Meld: O(log n)
/// </summary>
public sealed class BinomialHeap<T> : IPriorityQueue<T>
{
    private Node? _head;                 // početak root-liste (sortirane po stepenu)
    private int _count;                  // broj elemenata u gomili
    private readonly PQCounters? _counters;

    private sealed class Node
    {
        public T Item = default!;
        public double Key;
        public int Degree;               // broj djece (stepen binomnog stabla)
        public Node? Parent;
        public Node? Child;              // lijevo-najdublje dijete
        public Node? Sibling;            // sljedeći korijen ili brat u listi
        public PQHandle<T>? Handle;      // vanjska ručka koja pokazuje na ovaj čvor
    }

    public BinomialHeap(PQCounters? counters = null) => _counters = counters;

    public int Count => _count;
    public bool IsEmpty => _count == 0;

    // ————————————————————————————————————————————————————————————————
    // Pomoćni brojač (idiomatski .NET, bez ?.++)
    // ————————————————————————————————————————————————————————————————
    private void Inc(Action<PQCounters> action)
    {
        if (_counters is not null)
            action(_counters);
    }

    // ————————————————————————————————————————————————————————————————
    // JAVNE OPERACIJE
    // ————————————————————————————————————————————————————————————————
    public PQHandle<T> Insert(T item, double priority)
    {
        Inc(c => c.Inserts++);

        var node = new Node { Item = item, Key = priority };
        var handle = new PQHandle<T>(node);
        node.Handle = handle;

        // Kreiraj jednokorijensku gomilu i spoji (Meld)
        var single = new BinomialHeap<T>(_counters) { _head = node, _count = 1 };
        MeldInternal(single);

        _count++; // dodali smo jedan element
        return handle;
    }

    public (T Item, double Priority) ExtractMin()
    {
        if (_head is null)
            throw new InvalidOperationException("Heap is empty");

        Inc(c => c.ExtractMins++);

        // 1) Nađi minimalni korijen u root-listi
        Node? prevMin = null;
        Node? min = _head;
        Node? prev = null;

        Node? cur = _head;
        while (cur is not null)
        {
            if (cur.Key < min!.Key)
            {
                min = cur;
                prevMin = prev;
            }
            prev = cur;
            cur = cur.Sibling;
        }

        // 2) Ukloni ga iz root-liste
        if (prevMin is null)
            _head = min!.Sibling;
        else
            prevMin.Sibling = min!.Sibling;

        // 3) Obrni listu djece minimalnog korijena (postaju nova root-lista)
        Node? rev = null;
        Node? child = min!.Child;
        while (child is not null)
        {
            Node? next = child.Sibling;
            child.Parent = null;
            child.Sibling = rev;
            rev = child;
            child = next;
        }

        // 4) Stopi preostalu gomilu sa “reverznom” dječijom listom
        var h2 = new BinomialHeap<T>(_counters) { _head = rev };
        MeldInternal(h2);

        // 5) Održavanje brojila i ručke
        _count--;
        if (min.Handle is not null)
            min.Handle.NodeRef = null;

        return (min.Item, min.Key);
    }

    public void DecreaseKey(PQHandle<T> handle, double newPriority)
    {
        if (handle.NodeRef is not Node x)
            throw new InvalidOperationException("Invalid handle");
        if (newPriority > x.Key)
            throw new ArgumentException("New key must be smaller than current key.");

        Inc(c => c.DecreaseKeys++);

        // “Bubble-up” – zamjena sadržaja sa roditeljem dok se ne zadovolji heap-svojstvo
        x.Key = newPriority;
        Node y = x;
        Node? z = y.Parent;

        while (z is not null && y.Key < z.Key)
        {
            // zamijeni: ključ, vrijednost i ručke
            (y.Item, z.Item) = (z.Item, y.Item);
            (y.Key, z.Key) = (z.Key, y.Key);
            (y.Handle, z.Handle) = (z.Handle, y.Handle);

            if (y.Handle is not null) y.Handle.NodeRef = y;
            if (z.Handle is not null) z.Handle.NodeRef = z;

            y = z;
            z = y.Parent;
        }
    }

    public void Clear()
    {
        _head = null;
        _count = 0;
    }

    public void Meld(BinomialHeap<T> other)
    {
        Inc(c => c.Melds++);
        _count += other._count;      // zbir elemenata (jednostavno održavanje)
        MeldInternal(other);
        other._head = null;
        other._count = 0;
    }

    // ————————————————————————————————————————————————————————————————
    // PRIVATNO: spajanje (meld) i pomoćne funkcije
    // ————————————————————————————————————————————————————————————————
    /// <summary>
    /// Spaja root-liste this i other, zatim konsoliduje stabla istog stepena.
    /// </summary>
    private void MeldInternal(BinomialHeap<T> other)
    {
        _head = MergeRootLists(_head, other._head);
        other._head = null;

        if (_head is null)
            return;

        Node? prev = null;
        Node curr = _head;
        Node? next = curr.Sibling;

        // CLRS: prolaz kroz root-listu i konsolidacija binomnih stabala istog stepena
        while (next is not null)
        {
            bool degreesDiffer = curr.Degree != next.Degree;
            bool threeInARowSameDegree = next.Sibling is not null && next.Sibling.Degree == curr.Degree;

            if (degreesDiffer || threeInARowSameDegree)
            {
                // slučaj 1: samo napreduj (nema spajanja)
                prev = curr;
                curr = next;
            }
            else if (curr.Key <= next.Key)
            {
                // slučaj 2: next postaje dijete od curr
                curr.Sibling = next.Sibling;
                LinkTrees(next, curr);
            }
            else
            {
                // slučaj 3: curr postaje dijete od next
                if (prev is null) _head = next;
                else prev.Sibling = next;

                LinkTrees(curr, next);
                curr = next;
            }

            next = curr.Sibling;
        }
    }

    /// <summary>
    /// Spaja dvije root-liste sortirane po stepenu u jednu (bez konsolidacije).
    /// </summary>
    private static Node? MergeRootLists(Node? a, Node? b)
    {
        if (a is null) return b;
        if (b is null) return a;

        Node head;
        Node tail;

        if (a.Degree <= b.Degree)
        {
            head = a;
            a = a.Sibling;
        }
        else
        {
            head = b;
            b = b.Sibling;
        }

        tail = head;

        while (a is not null && b is not null)
        {
            if (a.Degree <= b.Degree)
            {
                tail.Sibling = a;
                a = a.Sibling;
            }
            else
            {
                tail.Sibling = b;
                b = b.Sibling;
            }
            tail = tail.Sibling!;
        }

        tail.Sibling = a ?? b;
        return head;
    }

    /// <summary>
    /// Učini childRoot djetetom od parentRoot (pretpostavlja se isti stepen).
    /// </summary>
    private static void LinkTrees(Node childRoot, Node parentRoot)
    {
        childRoot.Parent = parentRoot;
        childRoot.Sibling = parentRoot.Child;
        parentRoot.Child = childRoot;
        parentRoot.Degree++;
    }
}
