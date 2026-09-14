namespace ERFWeb.Domain.Reporting;

/// <summary>
/// Places a line's amounts into the R47 columns. Rule set:
/// <list type="bullet">
/// <item>Local-travel lines go to the LOCAL TRAVEL sub-column matching their category
/// (Meals/Transp./Toll/Parking/Others).</item>
/// <item><see cref="ExpenseCategory.Representation"/> goes to the Representation column.</item>
/// <item>Non-local (foreign) lines that are not Representation go to the "Oth" column.</item>
/// <item>Columns carry the line total (amount × multiplier); VAT is its own memo column; GRAND TOTAL is the line total.</item>
/// </list>
/// </summary>
public static class R47Bucketing
{
    public static R47Buckets Bucket(R47Line line)
    {
        ArgumentNullException.ThrowIfNull(line);

        decimal meals = 0, transp = 0, toll = 0, parking = 0, others = 0, representation = 0, oth = 0;

        if (line.Category == ExpenseCategory.Representation)
        {
            representation = line.TotalAmount;
        }
        else if (line.ExpenseType == ExpenseType.Local)
        {
            switch (line.Category)
            {
                case ExpenseCategory.Meals: meals = line.TotalAmount; break;
                case ExpenseCategory.Transportation: transp = line.TotalAmount; break;
                case ExpenseCategory.Toll: toll = line.TotalAmount; break;
                case ExpenseCategory.Parking: parking = line.TotalAmount; break;
                default: others = line.TotalAmount; break;
            }
        }
        else
        {
            oth = line.TotalAmount;
        }

        return new R47Buckets(meals, transp, toll, parking, others, representation, oth, line.VatAmount, line.TotalAmount);
    }

    /// <summary>
    /// The location text shown in the group header: "Location" alone, or
    /// "Location - Instrument - Serial" when instrument/serial/service details are present.
    /// </summary>
    public static string LocationText(R47Line line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var location = line.Location ?? string.Empty;
        if (IsMissing(line.Instrument) || IsMissing(line.SerialNumber) || IsMissing(line.ServiceNumber))
        {
            return location;
        }

        return $"{location} - {line.Instrument} - {line.SerialNumber}";
    }

    private static bool IsMissing(string? value) =>
        string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "N/A", StringComparison.OrdinalIgnoreCase);
}
