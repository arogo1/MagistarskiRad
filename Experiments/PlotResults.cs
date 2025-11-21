using System.Globalization;
using ScottPlot;

namespace Experiments;

public static class PlotResults
{
    public sealed record Row(
        string Algo,
        string Heap,
        string GraphType,
        int N,
        int M,
        int Seed,
        double Ms,
        long Bytes,
        long PQInserts,
        long PQExtracts,
        long PQDecrease,
        long PQMelds,
        string Extra
    );

    public static List<Row> LoadCsv(string path)
    {
        var rows = new List<Row>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var c = line.Split(',');
            if (c.Length < 13) continue;
            var r = new Row(
                Algo: c[0],
                Heap: c[1],
                GraphType: c[2],
                N: int.Parse(c[3]),
                M: int.Parse(c[4]),
                Seed: int.Parse(c[5]),
                Ms: double.Parse(c[6], CultureInfo.InvariantCulture),
                Bytes: long.Parse(c[7], CultureInfo.InvariantCulture),
                PQInserts: long.Parse(c[8], CultureInfo.InvariantCulture),
                PQExtracts: long.Parse(c[9], CultureInfo.InvariantCulture),
                PQDecrease: long.Parse(c[10], CultureInfo.InvariantCulture),
                PQMelds: long.Parse(c[11], CultureInfo.InvariantCulture),
                Extra: c[12]
            );
            rows.Add(r);
        }
        return rows;
    }

    // ScottPlot 5: Plot nema dimenzije; veličina se zadaje tek pri snimanju (SavePng)
    private static void Save(Plot plt, string path, int w = 900, int h = 550)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        plt.SavePng(path, w, h);
    }

    // ===== Linijski graf: vrijeme (ms) vs N za dati algoritam i tip grafa, uporedba gomila =====
    public static void TimeVsNByHeap(List<Row> rows, string algo, string graphType, string outPath)
    {
        var subset = rows.Where(r => r.Algo == algo && r.GraphType == graphType);
        var byHeap = subset
            .GroupBy(r => r.Heap)
            .ToDictionary(g => g.Key, g => g.GroupBy(x => x.N)
                                            .Select(h => new { N = h.Key, MsAvg = h.Average(x => x.Ms) })
                                            .OrderBy(x => x.N)
                                            .ToList());

        var plt = new Plot();
        plt.Title($"{algo} – {graphType}: vrijeme vs N");
        plt.XLabel("Broj čvorova N");
        plt.YLabel("Vrijeme (ms), prosjek po N");

        foreach (var (heap, list) in byHeap)
        {
            double[] xs = list.Select(x => (double)x.N).ToArray();
            double[] ys = list.Select(x => x.MsAvg).ToArray();
            var sc = plt.Add.Scatter(xs, ys);
            sc.LegendText = heap; // ScottPlot5: label se postavlja preko svojstva
        }
        plt.Legend.IsVisible = true;
        Save(plt, outPath);
    }

    // ===== "Grouped markers" umjesto bar chart (ScottPlot5-friendly): prosječne PQ operacije =====
    public static void PQOpsBars(List<Row> rows, string algo, string graphType, int n, string outPath)
    {
        var subset = rows.Where(r => r.Algo == algo && r.GraphType == graphType && r.N == n)
                         .GroupBy(r => r.Heap)
                         .Select(g => new {
                             Heap = g.Key,
                             Ins = g.Average(r => (double)r.PQInserts),
                             Ext = g.Average(r => (double)r.PQExtracts),
                             Dec = g.Average(r => (double)r.PQDecrease),
                             Mel = g.Average(r => (double)r.PQMelds)
                         })
                         .OrderBy(x => x.Heap)
                         .ToList();

        string[] labels = subset.Select(x => x.Heap).ToArray();
        double[] centers = Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray();
        double gap = 0.18;

        double[] xIns = centers.Select(x => x - 1.5 * gap).ToArray();
        double[] xExt = centers.Select(x => x - 0.5 * gap).ToArray();
        double[] xDec = centers.Select(x => x + 0.5 * gap).ToArray();
        double[] xMel = centers.Select(x => x + 1.5 * gap).ToArray();

        double[] yIns = subset.Select(x => x.Ins).ToArray();
        double[] yExt = subset.Select(x => x.Ext).ToArray();
        double[] yDec = subset.Select(x => x.Dec).ToArray();
        double[] yMel = subset.Select(x => x.Mel).ToArray();

        var plt = new Plot();
        plt.Title($"{algo} – {graphType} – N={n}: prosj. broj PQ operacija");
        plt.YLabel("Broj operacija (prosjek)");

        var s1 = plt.Add.Scatter(xIns, yIns); s1.LegendText = "Insert"; s1.LineStyle.Width = 0; s1.MarkerSize = 8;
        var s2 = plt.Add.Scatter(xExt, yExt); s2.LegendText = "ExtractMin"; s2.LineStyle.Width = 0; s2.MarkerSize = 8;
        var s3 = plt.Add.Scatter(xDec, yDec); s3.LegendText = "DecreaseKey"; s3.LineStyle.Width = 0; s3.MarkerSize = 8;
        var s4 = plt.Add.Scatter(xMel, yMel); s4.LegendText = "Meld"; s4.LineStyle.Width = 0; s4.MarkerSize = 8;

        // Kategorizirane oznake na X osi
        plt.Axes.SetLimitsX(-0.75, labels.Length - 0.25);
        plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(centers, labels);
        plt.Legend.IsVisible = true;
        Save(plt, outPath);
    }

    // ===== Linijski graf: memorija (alokacije) vs N =====
    public static void BytesVsNByHeap(List<Row> rows, string algo, string graphType, string outPath)
    {
        var subset = rows.Where(r => r.Algo == algo && r.GraphType == graphType);
        var byHeap = subset
            .GroupBy(r => r.Heap)
            .ToDictionary(g => g.Key, g => g.GroupBy(x => x.N)
                                            .Select(h => new { N = h.Key, BAvg = h.Average(x => (double)x.Bytes) })
                                            .OrderBy(x => x.N)
                                            .ToList());

        var plt = new Plot();
        plt.Title($"{algo} – {graphType}: alokacije (B) vs N");
        plt.XLabel("Broj čvorova N");
        plt.YLabel("Alocirani bajtovi (prosjek)");

        foreach (var (heap, list) in byHeap)
        {
            double[] xs = list.Select(x => (double)x.N).ToArray();
            double[] ys = list.Select(x => x.BAvg).ToArray();
            var sc = plt.Add.Scatter(xs, ys);
            sc.LegendText = heap;
        }
        plt.Legend.IsVisible = true;
        Save(plt, outPath);
    }

    // ===== Orkestrator =====
    public static void MakeAll(string csvPath)
    {
        var rows = LoadCsv(csvPath);
        string figDir = Path.Combine("figures", Path.GetFileNameWithoutExtension(csvPath));
        Directory.CreateDirectory(figDir);

        string[] algos = rows.Select(r => r.Algo).Distinct().OrderBy(x => x).ToArray();
        string[] types  = rows.Select(r => r.GraphType).Distinct().OrderBy(x => x).ToArray();
        int[] Ns        = rows.Select(r => r.N).Distinct().OrderBy(x => x).ToArray();

        foreach (var a in algos)
        foreach (var t in types)
        {
            TimeVsNByHeap(rows, a, t, Path.Combine(figDir, $"time_vs_n_{a}_{t}.png"));
            BytesVsNByHeap(rows, a, t, Path.Combine(figDir, $"bytes_vs_n_{a}_{t}.png"));
            if (Ns.Length > 0)
            {
                var maxN = Ns.Max();
                PQOpsBars(rows, a, t, maxN, Path.Combine(figDir, $"pq_ops_{a}_{t}_N{maxN}.png"));
            }
        }
    }
}
