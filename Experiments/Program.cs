using System.Diagnostics;
using System.Globalization;
using Common;
using Heaps;
using Algorithms;
using Common.Interfaces;
using GraphCore;

static (double ms, long bytes) Time(Action action)
{
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    long before = GC.GetAllocatedBytesForCurrentThread();
    var sw = Stopwatch.StartNew();
    action();
    sw.Stop();
    long after = GC.GetAllocatedBytesForCurrentThread();
    return (sw.Elapsed.TotalMilliseconds, after - before);
}

// ======== Konfiguracija eksperimenata ========
var sizes = new[] { 500, 1000, 2000 }; // možeš povećati kasnije
var graphTypes = new[] { "ER_sparse", "ER_dense", "Grid" };
int repeats = 5; // ponavljanja radi stabilnosti

Directory.CreateDirectory("data/results");
string csvPath = Path.Combine("data", "results", $"runs_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

using var w = new StreamWriter(csvPath);
w.WriteLine("algo,heap,graph_type,n,m,seed,ms,bytes,pq_inserts,pq_extracts,pq_decrease,pq_melds,extra");

IPriorityQueue<int> Binom(PQCounters c) => new BinomialHeap<int>(c);
IPriorityQueue<int> Fib(PQCounters c)   => new FibonacciHeap<int>(c);

foreach (var n in sizes)
{
    foreach (var gt in graphTypes)
    {
        for (int r = 0; r < repeats; r++)
        {
            int seed = 1000 + r;
            Graph g = gt switch
            {
                "ER_sparse" => Generators.ERConnected(n, p: 2.0 * Math.Log(n) / n, seed: seed),
                "ER_dense"  => Generators.ERConnected(n, p: 0.1, seed: seed),
                "Grid"      => Generators.Grid((int)Math.Sqrt(n), (int)Math.Sqrt(n), seed: seed),
                _ => throw new ArgumentOutOfRangeException(nameof(gt))
            };

            // ===== Dijkstra × Binomial =====
            var counters = new PQCounters();
            var (ms, bytes) = Time(() => {
                var res = Dijkstra.Run(g, 0, _ => Binom(counters));
                // zapis dodatnih metrika (relaxations) u "extra"
                File.AppendAllText("/dev/null", string.Empty); // no-op to keep lambda Action
            });
            w.WriteLine(string.Join(',', new object[] {
                "Dijkstra","Binomial", gt, n, g.M, seed,
                ms.ToString("F3", CultureInfo.InvariantCulture), bytes,
                counters.Inserts, counters.ExtractMins, counters.DecreaseKeys, counters.Melds,
                "relaxations" // dodatne specifične metrike možeš naknadno proširiti
            }));

            // ===== Dijkstra × Fibonacci =====
            counters = new PQCounters();
            (ms, bytes) = Time(() => { var _ = Dijkstra.Run(g, 0, _ => Fib(counters)); });
            w.WriteLine(string.Join(',', new object[] {
                "Dijkstra","Fibonacci", gt, n, g.M, seed,
                ms.ToString("F3", CultureInfo.InvariantCulture), bytes,
                counters.Inserts, counters.ExtractMins, counters.DecreaseKeys, counters.Melds,
                "relaxations"
            }));

            // ===== Prim × Binomial =====
            counters = new PQCounters();
            (ms, bytes) = Time(() => { var _ = Prim.Run(g, _ => Binom(counters), 0); });
            w.WriteLine(string.Join(',', new object[] {
                "Prim","Binomial", gt, n, g.M, seed,
                ms.ToString("F3", CultureInfo.InvariantCulture), bytes,
                counters.Inserts, counters.ExtractMins, counters.DecreaseKeys, counters.Melds,
                "mst_weight" // MST težina je u Prim.Result, možeš je dodati ako želiš
            }));

            // ===== Prim × Fibonacci =====
            counters = new PQCounters();
            (ms, bytes) = Time(() => { var _ = Prim.Run(g, _ => Fib(counters), 0); });
            w.WriteLine(string.Join(',', new object[] {
                "Prim","Fibonacci", gt, n, g.M, seed,
                ms.ToString("F3", CultureInfo.InvariantCulture), bytes,
                counters.Inserts, counters.ExtractMins, counters.DecreaseKeys, counters.Melds,
                "mst_weight"
            }));
        }
    }
}

Console.WriteLine($"CSV: {csvPath}");
Console.WriteLine("Gotovo. Možeš otvoriti CSV i crtati grafove (npr. u Pythonu, Excelu ili naknadno dodati ScottPlot).");

Experiments.PlotResults.MakeAll(csvPath);
