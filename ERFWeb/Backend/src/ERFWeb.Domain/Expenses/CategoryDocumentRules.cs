namespace ERFWeb.Domain.Expenses;

/// <summary>A category whose lines need a document of one type attached to the report.</summary>
public sealed record RequiredDocument(ExpenseCategory Category, AttachmentDocumentType DocumentType, string Label);

/// <summary>
/// Categories switched on or off by the Finance rules, and the documents some categories need (Finance requirements 7
/// and 8, confirmed with Finance on 2026-11-11): Sponsorship needs an approved Activity Request Form, Demo an
/// ISO-registered attendance sheet, Representation the food or meal receipt. A document is
/// any attachment of the report with that type. It must be attached before such a line can be added (or changed);
/// filing checks again in case it was deleted afterwards.
/// </summary>
public static class CategoryDocumentRules
{
    public static string DocumentLabel(AttachmentDocumentType type) => type switch
    {
        AttachmentDocumentType.ActivityRequestForm => "approved Activity Request Form",
        AttachmentDocumentType.IsoAttendanceSheet => "ISO-registered attendance sheet",
        AttachmentDocumentType.FoodReceipt => "food or meal receipt",
        _ => "receipt",
    };

    public static bool IsEnabled(FinanceRuleSet rules, ExpenseCategory category)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return category switch
        {
            ExpenseCategory.Sponsorship => rules.SponsorshipEnabled,
            ExpenseCategory.Demo => rules.DemoEnabled,
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
            required.Add(Doc(ExpenseCategory.Demo, AttachmentDocumentType.IsoAttendanceSheet));
        }

        if (rules.RepresentationRequiresDocument)
        {
            required.Add(Doc(ExpenseCategory.Representation, AttachmentDocumentType.FoodReceipt));
        }

        return required;
    }

    /// <summary>Why a line in <paramref name="category"/> cannot be saved yet: its document is not attached. Null when it can.</summary>
    public static string? LineProblem(ExpenseCategory category, IEnumerable<AttachmentDocumentType> attachedTypes, FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(attachedTypes);
        var doc = Required(rules).FirstOrDefault(d => d.Category == category);
        return doc is null || attachedTypes.Contains(doc.DocumentType)
            ? null
            : $"Attach the {doc.Label} under Receipts (as \"{TypeName(doc.DocumentType)}\") before adding a {category} line.";
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
            problems.Add($"{off} claims are switched off. Remove or change those lines.");
        }

        foreach (var doc in Required(rules).Where(d => categories.Contains(d.Category) && !attached.Contains(d.DocumentType)))
        {
            problems.Add($"A {doc.Category} line needs an {doc.Label} attached. Upload it under Receipts as \"{TypeName(doc.DocumentType)}\".");
        }

        return problems;
    }

    /// <summary>The name the upload picker shows for a document type.</summary>
    public static string TypeName(AttachmentDocumentType type) => type switch
    {
        AttachmentDocumentType.ActivityRequestForm => "Activity Request Form",
        AttachmentDocumentType.IsoAttendanceSheet => "ISO attendance sheet",
        AttachmentDocumentType.FoodReceipt => "Food receipt",
        _ => "Receipt",
    };

    private static RequiredDocument Doc(ExpenseCategory category, AttachmentDocumentType type) => new(category, type, DocumentLabel(type));
}
