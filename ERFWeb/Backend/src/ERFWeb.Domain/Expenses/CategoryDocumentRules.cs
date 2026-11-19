using System.Text.RegularExpressions;

namespace ERFWeb.Domain.Expenses;

/// <summary>A document of one type that a category's lines need attached to the report; a category may need several.</summary>
public sealed record RequiredDocument(ExpenseCategory Category, AttachmentDocumentType DocumentType, string Label);

/// <summary>
/// Categories switched on or off by the Finance rules, and the documents some categories need (Finance requirements 7
/// and 8, confirmed with Finance on 2026-11-11): Sponsorship needs an approved Activity Request Form, Demo an
/// ISO-registered attendance sheet, Representation the food or meal receipt, Lodging the official receipt from the
/// hotel or lodging (added 2026-11-19); Gas and Vehicle emergency purchase their receipt when the rules verify
/// them by document (requirement 6). A document is
/// any attachment of the report with that type. It must be attached before such a line can be added (or changed);
/// filing checks again in case it was deleted afterwards. An attendance sheet counts only when its upload showed
/// the form code (<see cref="FinanceRuleSet.DemoFormCode"/>, R94) or the employee confirmed it; a Job Order likewise
/// (<see cref="FinanceRuleSet.JobOrderFormCode"/>, R42).
/// </summary>
public static class CategoryDocumentRules
{
    public static string DocumentLabel(AttachmentDocumentType type) => type switch
    {
        AttachmentDocumentType.ActivityRequestForm => "approved Activity Request Form",
        AttachmentDocumentType.IsoAttendanceSheet => "ISO-registered attendance sheet",
        AttachmentDocumentType.FoodReceipt => "food or meal receipt",
        AttachmentDocumentType.LodgingReceipt => "official receipt from the hotel or lodging",
        AttachmentDocumentType.GasReceipt => "gas station receipt",
        AttachmentDocumentType.VehiclePurchaseReceipt => "receipt for the vehicle purchase",
        AttachmentDocumentType.JobOrder => "Job Order (JO)",
        _ => "receipt",
    };

    public static bool IsEnabled(FinanceRuleSet rules, ExpenseCategory category)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return category switch
        {
            ExpenseCategory.Sponsorship => rules.SponsorshipEnabled,
            ExpenseCategory.Demo => rules.DemoEnabled,
            ExpenseCategory.Lodging => rules.LodgingEnabled,
            ExpenseCategory.Gas => rules.GasEnabled,
            ExpenseCategory.VehicleEmergency => rules.VehicleEmergencyEnabled,
            _ => true,
        };
    }

    /// <summary>Categories employees may not pick now.</summary>
    public static IReadOnlyList<ExpenseCategory> Disabled(FinanceRuleSet rules) =>
        Enum.GetValues<ExpenseCategory>().Where(c => !IsEnabled(rules, c)).ToList();

    /// <summary>The documents the rules require, per category that is on.</summary>
    public static IReadOnlyList<RequiredDocument> Required(FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var required = new List<RequiredDocument>();
        if (rules.SponsorshipEnabled && rules.SponsorshipRequiresDocument)
        {
            required.Add(Doc(ExpenseCategory.Sponsorship, AttachmentDocumentType.ActivityRequestForm));
        }

        if (rules.DemoEnabled && rules.DemoRequiresDocument)
        {
            required.Add(new RequiredDocument(
                ExpenseCategory.Demo,
                AttachmentDocumentType.IsoAttendanceSheet,
                $"{DocumentLabel(AttachmentDocumentType.IsoAttendanceSheet)} (form {rules.DemoFormCode.Trim()})"));
        }

        if (rules.RepresentationRequiresDocument)
        {
            required.Add(Doc(ExpenseCategory.Representation, AttachmentDocumentType.FoodReceipt));
        }

        if (rules.LodgingEnabled && rules.LodgingRequiresDocument)
        {
            required.Add(Doc(ExpenseCategory.Lodging, AttachmentDocumentType.LodgingReceipt));
        }

        if (rules.GasEnabled && rules.GasVerification is CategoryVerification.Document or CategoryVerification.Both)
        {
            required.Add(Doc(ExpenseCategory.Gas, AttachmentDocumentType.GasReceipt));
        }

        if (rules.VehicleEmergencyEnabled && rules.VehicleEmergencyVerification is CategoryVerification.Document or CategoryVerification.Both)
        {
            // Both: the receipt for what was bought and the ISO Job Order (R42) for the job.
            required.Add(Doc(ExpenseCategory.VehicleEmergency, AttachmentDocumentType.VehiclePurchaseReceipt));
            required.Add(new RequiredDocument(
                ExpenseCategory.VehicleEmergency,
                AttachmentDocumentType.JobOrder,
                $"{DocumentLabel(AttachmentDocumentType.JobOrder)}, form {rules.JobOrderFormCode.Trim()}"));
        }

        return required;
    }

    /// <summary>
    /// Whether lines in <paramref name="category"/> get a verification request when the report is fully approved
    /// (Gas or Vehicle emergency purchase verified by a person).
    /// </summary>
    public static bool NeedsPersonVerification(FinanceRuleSet rules, ExpenseCategory category)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var mode = category switch
        {
            ExpenseCategory.Gas => rules.GasVerification,
            ExpenseCategory.VehicleEmergency => rules.VehicleEmergencyVerification,
            _ => CategoryVerification.None,
        };
        return mode is CategoryVerification.Person or CategoryVerification.Both;
    }

    /// <summary>The reason recorded on an automatic verification request.</summary>
    public static string AutomaticVerificationReason(ExpenseCategory category) =>
        $"Automatic: {CategoryName(category)} lines are verified before Finance clears the report (Finance rule).";

    /// <summary>Why a line in <paramref name="category"/> cannot be saved yet: a document it needs is not attached. Null when it can.</summary>
    public static string? LineProblem(ExpenseCategory category, IEnumerable<AttachmentDocumentType> attachedTypes, FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(attachedTypes);
        var attached = attachedTypes.ToHashSet();
        var missing = Required(rules).Where(d => d.Category == category && !attached.Contains(d.DocumentType)).ToList();
        return missing.Count == 0
            ? null
            : $"Attach {string.Join(" and ", missing.Select(d => $"the {d.Label}"))} under Receipts "
                + $"(as {string.Join(" and ", missing.Select(d => $"\"{TypeName(d.DocumentType)}\""))}) before adding a {CategoryName(category)} line.";
    }

    /// <summary>
    /// Why the report cannot be filed: a line in a category that is switched off, or a category whose document is
    /// not attached. Empty when it can.
    /// </summary>
    public static IReadOnlyList<string> FilingProblems(
        IEnumerable<ExpenseCategory> activeLineCategories, IEnumerable<AttachmentDocumentType> attachedTypes, FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(activeLineCategories);
        ArgumentNullException.ThrowIfNull(attachedTypes);
        var categories = activeLineCategories.ToHashSet();
        var attached = attachedTypes.ToHashSet();
        var problems = new List<string>();

        foreach (var off in categories.Where(c => !IsEnabled(rules, c)).Order())
        {
            problems.Add($"{CategoryName(off)} claims are switched off. Remove or change those lines.");
        }

        foreach (var doc in Required(rules).Where(d => categories.Contains(d.Category) && !attached.Contains(d.DocumentType)))
        {
            problems.Add($"A {CategoryName(doc.Category)} line needs {Article(doc.Label)} {doc.Label} attached. Upload it under Receipts as \"{TypeName(doc.DocumentType)}\".");
        }

        return problems;
    }

    /// <summary>
    /// Whether <paramref name="text"/> (a PDF's extracted text) shows <paramref name="code"/> as a word of its own, any
    /// case, allowing spaces or a hyphen between its characters ("R94", "r 94", "R-94"; not "FR94" or "R945").
    /// </summary>
    public static bool ContainsFormCode(string? text, string? code)
    {
        var trimmed = code?.Trim();
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        var pattern = "(?<![A-Za-z0-9])" + string.Join(@"[\s-]*", trimmed.Select(c => Regex.Escape(c.ToString()))) + "(?![A-Za-z0-9])";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// The ISO form code an upload of <paramref name="type"/> must show (attendance sheet R94, Job Order R42), or null
    /// when the type has none. Such an upload counts only once the code was found or the employee confirmed it.
    /// </summary>
    public static string? FormCodeFor(FinanceRuleSet rules, AttachmentDocumentType type)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return type switch
        {
            AttachmentDocumentType.IsoAttendanceSheet => rules.DemoFormCode.Trim(),
            AttachmentDocumentType.JobOrder => rules.JobOrderFormCode.Trim(),
            _ => null,
        };
    }

    /// <summary>Why a form upload is refused: the code was not found and the employee did not confirm it.</summary>
    public static string FormCodeNotFound(FinanceRuleSet rules, AttachmentDocumentType type = AttachmentDocumentType.IsoAttendanceSheet)
    {
        var code = FormCodeFor(rules, type) ?? string.Empty;
        var form = type == AttachmentDocumentType.JobOrder ? "Job Order" : "attendance sheet";
        return $"This file does not show form {code}. Upload the {code} {form}, or confirm that this file is form {code}.";
    }

    /// <summary>The name the upload picker shows for a document type.</summary>
    public static string TypeName(AttachmentDocumentType type) => type switch
    {
        AttachmentDocumentType.ActivityRequestForm => "Activity Request Form",
        AttachmentDocumentType.IsoAttendanceSheet => "ISO attendance sheet",
        AttachmentDocumentType.FoodReceipt => "Food receipt",
        AttachmentDocumentType.LodgingReceipt => "Lodging receipt",
        AttachmentDocumentType.GasReceipt => "Gas receipt",
        AttachmentDocumentType.VehiclePurchaseReceipt => "Vehicle purchase receipt",
        AttachmentDocumentType.JobOrder => "Job Order (JO)",
        _ => "Receipt",
    };

    /// <summary>The category as messages name it.</summary>
    public static string CategoryName(ExpenseCategory category) =>
        category == ExpenseCategory.VehicleEmergency ? "Vehicle emergency purchase" : category.ToString();

    private static string Article(string label) => label.Length > 0 && "aeiouAEIOU".Contains(label[0]) ? "an" : "a";

    private static RequiredDocument Doc(ExpenseCategory category, AttachmentDocumentType type) => new(category, type, DocumentLabel(type));
}
