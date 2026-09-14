namespace ERFWeb.Domain.Expenses;

/// <summary>Result of a per-diem allowance computation.</summary>
public readonly record struct AllowanceResult(int Multiplier, decimal TotalAmount, string ComputationText);

/// <summary>
/// Pure per-diem/allowance computation. Days chargeable are total days minus
/// excluded days, multiplied by a daily amount. Mirrors the desktop rule.
/// </summary>
public static class AllowanceCalculator
{
    public static AllowanceResult Compute(int totalDays, int minusDays, decimal dailyAmount)
    {
        if (totalDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalDays));
        }

        if (minusDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minusDays));
        }

        var multiplier = Math.Max(0, totalDays - minusDays);
        var total = multiplier * dailyAmount;
        var text = minusDays > 0
            ? $"({totalDays}Days - {minusDays}Days) * {dailyAmount:0.##}"
            : $"({totalDays}Days) * {dailyAmount:0.##}";

        return new AllowanceResult(multiplier, total, text);
    }
}
