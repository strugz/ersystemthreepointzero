using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class CategoryDocumentRulesTests
{
    private static readonly FinanceRuleSet On = FinanceRuleSet.Default;

    [Fact]
    public void Default_RequiresTheThreeDocuments()
    {
        var required = CategoryDocumentRules.Required(On);

        Assert.Equal(
            [
                (ExpenseCategory.Sponsorship, AttachmentDocumentType.ActivityRequestForm),
                (ExpenseCategory.Demo, AttachmentDocumentType.IsoAttendanceSheet),
                (ExpenseCategory.Representation, AttachmentDocumentType.FoodReceipt),
            ],
            required.Select(r => (r.Category, r.DocumentType)));
        Assert.Empty(CategoryDocumentRules.Disabled(On));
    }

    [Fact]
    public void SponsorshipWithoutForm_Blocks_WithForm_Files()
    {
        Assert.Single(CategoryDocumentRules.FilingProblems([ExpenseCategory.Sponsorship], [AttachmentDocumentType.Receipt], On));
        Assert.Empty(CategoryDocumentRules.FilingProblems([ExpenseCategory.Sponsorship], [AttachmentDocumentType.ActivityRequestForm], On));
    }

    [Fact]
    public void DemoNeedsTheAttendanceSheet_NotTheForm()
    {
        var problems = CategoryDocumentRules.FilingProblems([ExpenseCategory.Demo], [AttachmentDocumentType.ActivityRequestForm], On);

        Assert.Equal(["A Demo line needs an ISO-registered attendance sheet attached. Upload it under Receipts as \"ISO attendance sheet\"."], problems);
    }

    [Fact]
    public void LineProblem_NeedsTheDocumentBeforeTheLine()
    {
        Assert.Equal(
            "Attach the approved Activity Request Form under Receipts (as \"Activity Request Form\") before adding a Sponsorship line.",
            CategoryDocumentRules.LineProblem(ExpenseCategory.Sponsorship, [AttachmentDocumentType.Receipt], On));
        Assert.Null(CategoryDocumentRules.LineProblem(ExpenseCategory.Sponsorship, [AttachmentDocumentType.ActivityRequestForm], On));
        Assert.NotNull(CategoryDocumentRules.LineProblem(ExpenseCategory.Demo, [AttachmentDocumentType.ActivityRequestForm], On));
        Assert.Null(CategoryDocumentRules.LineProblem(ExpenseCategory.Meals, [], On));
        Assert.Null(CategoryDocumentRules.LineProblem(ExpenseCategory.Sponsorship, [], On with { SponsorshipRequiresDocument = false }));
    }

    [Fact]
    public void Representation_NeedsTheFoodReceipt_UnlessSwitchedOff()
    {
        Assert.Equal(
            "Attach the food or meal receipt under Receipts (as \"Food receipt\") before adding a Representation line.",
            CategoryDocumentRules.LineProblem(ExpenseCategory.Representation, [AttachmentDocumentType.Receipt], On));
        Assert.Null(CategoryDocumentRules.LineProblem(ExpenseCategory.Representation, [AttachmentDocumentType.FoodReceipt], On));
        Assert.Null(CategoryDocumentRules.LineProblem(ExpenseCategory.Representation, [], On with { RepresentationRequiresDocument = false }));
    }

    [Fact]
    public void OtherCategories_NeedNothing() =>
        Assert.Empty(CategoryDocumentRules.FilingProblems([ExpenseCategory.Meals, ExpenseCategory.Others], [], On));

    [Fact]
    public void DocumentRuleOff_FilesWithoutIt()
    {
        var rules = On with { SponsorshipRequiresDocument = false };

        Assert.Empty(CategoryDocumentRules.FilingProblems([ExpenseCategory.Sponsorship], [], rules));
    }

    [Fact]
    public void CategoryOff_IsHiddenAndBlocksExistingLines()
    {
        var rules = On with { DemoEnabled = false };

        Assert.False(CategoryDocumentRules.IsEnabled(rules, ExpenseCategory.Demo));
        Assert.Equal([ExpenseCategory.Demo], CategoryDocumentRules.Disabled(rules));
        Assert.DoesNotContain(CategoryDocumentRules.Required(rules), r => r.Category == ExpenseCategory.Demo);
        Assert.Equal(["Demo claims are switched off. Remove or change those lines."],
            CategoryDocumentRules.FilingProblems([ExpenseCategory.Demo], [AttachmentDocumentType.IsoAttendanceSheet], rules));
    }

    [Fact]
    public void FilingBlockers_IncludeCategoryProblems()
    {
        var blockers = ReportWorkflowRules.FilingBlockers(
            ReportStatus.New, 1, 1, "Visit", ReportType.Reimbursement, 0m, 0m, categoryProblems: ["Needs a form."]);

        Assert.Equal(["Needs a form."], blockers);
    }
}
