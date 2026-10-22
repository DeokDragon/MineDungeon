using System;
using System.Linq;

internal static class MonsterExperienceAnalysis
{
    internal struct Range { public long min, max; public double average; }
    // States are candidate indices, matching PickNormalRoom (including duplicate references).
    internal static Range Calculate(long[] values, int rooms, int boss)
    {
        if (values == null || values.Length == 0 || rooms < 1 || boss < 0 || values.Any(v => v < 0))
            throw new InvalidOperationException("일반 맵 후보/방 수/EXP 설정을 확인하세요.");
        int n = values.Length;
        var min = values.ToArray(); var max = values.ToArray();
        var probability = Enumerable.Repeat(1d / n, n).ToArray();
        var weighted = values.Select(v => v / (double)n).ToArray();
        for (int step = 1; step < rooms; step++)
        {
            var lo = Enumerable.Repeat(long.MaxValue, n).ToArray();
            var hi = new long[n]; var p = new double[n]; var w = new double[n];
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
            {
                if (n > 1 && i == j) continue;
                double chance = 1d / Math.Max(1, n - 1);
                lo[j] = Math.Min(lo[j], checked(min[i] + values[j]));
                hi[j] = Math.Max(hi[j], checked(max[i] + values[j]));
                p[j] += probability[i] * chance;
                w[j] += (weighted[i] + probability[i] * values[j]) * chance;
            }
            min = lo; max = hi; probability = p; weighted = w;
        }
        return new Range { min = checked(min.Min() + boss), max = checked(max.Max() + boss), average = weighted.Sum() + boss };
    }
}
