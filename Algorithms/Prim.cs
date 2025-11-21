using Common;
using Common.Interfaces;
using GraphCore;

namespace Algorithms;

public static class Prim
{
    public sealed record Result(double TotalWeight, int[] Parent, PQCounters Counters);

    /// <summary>
    /// Prim (MST) sa bilo kojom IPriorityQueue implementacijom.
    /// heapFactory dobija PQCounters i vraća novi prazan heap.
    /// </summary>
    public static Result Run(Graph g, Func<PQCounters, IPriorityQueue<int>> heapFactory, int start = 0)
    {
        var n = g.N;
        var inMst = new bool[n];
        var key = new double[n];
        var parent = new int[n];
        var handles = new PQHandle<int>?[n];
        var counters = new PQCounters();

        for (int i = 0; i < n; i++) { key[i] = double.PositiveInfinity; parent[i] = -1; }

        var pq = heapFactory(counters);
        key[start] = 0.0;
        handles[start] = pq.Insert(start, 0.0);

        double total = 0.0;
        int picked = 0;

        while (!pq.IsEmpty && picked < n)
        {
            var (u, ku) = pq.ExtractMin();
            if (inMst[u]) continue;

            inMst[u] = true;
            total += ku;
            picked++;

            foreach (var (v, w) in g.Neighbors(u))
            {
                if (inMst[v]) continue;
                if (w < key[v])
                {
                    key[v] = w;
                    parent[v] = u;

                    if (handles[v] is null)
                    {
                        handles[v] = pq.Insert(v, w);
                    }
                    else
                    {
                        pq.DecreaseKey(handles[v]!, w);
                    }
                }
            }
        }

        return new Result(total, parent, counters);
    }
}