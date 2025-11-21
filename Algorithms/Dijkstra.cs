using Common;
using Common.Interfaces;
using GraphCore;

namespace Algorithms;

public static class Dijkstra
{
    public sealed record Result(double[] Dist, int[] Parent, long Relaxations, PQCounters Counters);

    /// <summary>
    /// Dijkstra sa bilo kojom IPriorityQueue implementacijom (binomna/fib).
    /// heapFactory dobija PQCounters i vraća novi prazan heap.
    /// </summary>
    public static Result Run(Graph g, int source, Func<PQCounters, IPriorityQueue<int>> heapFactory)
    {
        var n = g.N;
        var dist = new double[n];
        var parent = new int[n];
        var handles = new PQHandle<int>?[n];
        var visited = new bool[n];
        var counters = new PQCounters();

        for (int i = 0; i < n; i++) { dist[i] = double.PositiveInfinity; parent[i] = -1; }
        dist[source] = 0;

        var pq = heapFactory(counters);
        handles[source] = pq.Insert(source, 0.0);

        long relaxations = 0;

        while (!pq.IsEmpty)
        {
            var (u, _) = pq.ExtractMin();
            if (visited[u]) continue;
            visited[u] = true;

            foreach (var (v, w) in g.Neighbors(u))
            {
                if (visited[v]) continue;
                var nd = dist[u] + w;
                if (nd < dist[v])
                {
                    dist[v] = nd;
                    parent[v] = u;
                    relaxations++;

                    if (handles[v] is null)
                    {
                        handles[v] = pq.Insert(v, nd);
                    }
                    else
                    {
                        pq.DecreaseKey(handles[v]!, nd);
                    }
                }
            }
        }

        return new Result(dist, parent, relaxations, counters);
    }
}