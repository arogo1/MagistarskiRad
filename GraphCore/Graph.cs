namespace GraphCore;

/// <summary>
/// Nedirektni, težinski graf predstavljen listama susjedstva.
/// </summary>
public sealed class Graph
{
    private readonly List<(int to, double w)>[] _adj;

    /// <summary>Broj čvorova.</summary>
    public int N => _adj.Length;

    /// <summary>Broj ivica (nedirektnih). Svaki AddEdge povećava M za 1.</summary>
    public int M { get; private set; }

    /// <summary>
    /// Kreira graf sa n čvorova (0..n-1).
    /// </summary>
    public Graph(int n)
    {
        if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "n mora biti > 0");
        _adj = new List<(int, double)>[n];
        for (int i = 0; i < n; i++)
            _adj[i] = new List<(int, double)>();
    }

    /// <summary>
    /// Enumerator susjeda: (v, w) za svaku ivicu u–v sa težinom w.
    /// </summary>
    public IEnumerable<(int to, double w)> Neighbors(int u)
    {
        if (u < 0 || u >= N) throw new ArgumentOutOfRangeException(nameof(u));
        return _adj[u];
    }

    /// <summary>
    /// Dodaje nedirektnu ivicu u–v sa težinom w.
    /// Vraća false ako je u==v; baca izuzetak na indeksima van opsega.
    /// Dozvoljava višestruke ivice (multigraf) — korisno za eksperimente.
    /// </summary>
    public bool AddEdge(int u, int v, double w)
    {
        if (u < 0 || u >= N) throw new ArgumentOutOfRangeException(nameof(u));
        if (v < 0 || v >= N) throw new ArgumentOutOfRangeException(nameof(v));
        if (u == v) return false;

        _adj[u].Add((v, w));
        _adj[v].Add((u, w));
        M++;
        return true;
    }

    /// <summary>
    /// (Opcionalno) Stepen čvora u.
    /// </summary>
    public int Degree(int u)
    {
        if (u < 0 || u >= N) throw new ArgumentOutOfRangeException(nameof(u));
        return _adj[u].Count;
    }
}