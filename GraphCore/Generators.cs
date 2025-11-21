namespace GraphCore;

public static class Generators
{
    /// <summary>Generiše povezani Erdos–Renyi graf sa n čvorova i vjerovatnoćom p.</summary>
    public static Graph ERConnected(int n, double p, int seed)
    {
        var rnd = new Random(seed);
        var g = new Graph(n);

        // prvo napravi stablo da bude povezan
        for (int v = 1; v < n; v++)
            g.AddEdge(v, rnd.Next(v), 1 + rnd.NextDouble());

        // zatim dodaj dodatne ivice s vjerovatnoćom p
        for (int u = 0; u < n; u++)
        for (int v = u + 1; v < n; v++)
            if (rnd.NextDouble() < p)
                g.AddEdge(u, v, 1 + rnd.NextDouble());

        return g;
    }

    /// <summary>Generiše mrežu (grid) širine w i visine h.</summary>
    public static Graph Grid(int w, int h, int seed)
    {
        var rnd = new Random(seed);
        var g = new Graph(w * h);
        int Id(int x, int y) => y * w + x;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int u = Id(x, y);
            if (x + 1 < w) g.AddEdge(u, Id(x + 1, y), 1 + rnd.NextDouble());
            if (y + 1 < h) g.AddEdge(u, Id(x, y + 1), 1 + rnd.NextDouble());
        }
        return g;
    }
}