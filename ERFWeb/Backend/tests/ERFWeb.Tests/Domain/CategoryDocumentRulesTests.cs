using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class CategoryDocumentRulesTests
{
    private static readonly FinanceRuleSet On = FinanceRuleSet.Default;

    [Fact]
    public void Default_RequiresTheSevenDocuments()
    {
        var required = CategoryDocumentRules.Required(On);

        Assert.Equal(
            [
                (ExpenseCategory.Sponsorship, AttachmentDocumentType.ActivityRequestForm),
                (ExpenseCategory.Demo, AttachmentDocumentType.IsoAttendanceSheet),
                (ExpenseCategory.Representation, AttachmentDocumentType.FoodReceipt),
                (ExpenseCategory.Lodging, AttachmentDocumentType.LodgingReceipt),
                (ExpenseCategory.Gas, AttachmentDocumentType.GasReceipt),
                (ExpenseCategory.VehicleEmergency, AttachmentDocumentType.VehiclePurchaseReceipt),
                (ExpenseCategory.VehicleEmergency, AttachmentDocumentType.JobOrder),
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

        Assert.Equal(["A Demo line needs an ISO-registered attendance sheet (form R94) attached. Upload it under Receipts as \"ISO attendance sheet\"."], problems);
    }

    [Theory]
    [InlineData("Document Code: R94 Page No. 1", true)]
    [InlineData("document code r94", true)]
    [InlineData("Code R 94", true)]
    [InlineData("Code R-94", true)]
    [InlineData("Code FR94", false)]
    [InlineData("Code R945", false)]
    [InlineData("Control no. 016319400", false)]
    [InlineData("", false)]
    public void ContainsFormCode_MatchesTheCodeAsAWord(string text, bool expected) =>
        Assert.Equal(expected, CategoryDocumentRules.ContainsFormCode(text, "R94"));

    [Fact]
    public void FormCode_FollowsTheRule()
    {
        var rules = On with { DemoFormCode = "R95" };

        Assert.Equal("ISO-registered attendance sheet (form R95)", CategoryDocumentRules.Required(rules).Single(r => r.Category == ExpenseCategory.Demo).Label);
        Assert.Equal("This file does not show form R95. Upload the R95 attendance sheet, or confirm that this file is form R95.",
            CategoryDocumentRules.FormCodeNotFound(rules));
    }

    [Theory]
    [InlineData("R94", true)]
    [InlineData("F-R94", true)]
    [InlineData("", false)]
    [InlineData("R 94", false)]
    [InlineData("R94R94R94R94R94R94R94", false)]
    public void DemoFormCode_IsValidated(string code, bool valid) =>
        Assert.Equal(valid, !FinanceRuleSetRules.Validate(On with { DemoFormCode = code }).ContainsKey("financeRules.demoFormCode"));

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
        Assert.Equal(
            ["A Representation line needs a food or meal receipt attached. Upload it under Receipts as \"Food receipt\"."],
            CategoryDocumentRules.FilingProblems([ExpenseCategory.Representation], [], On));
    }

    [Fact]
    public void Lodging_NeedsTheLodgingReceipt_NotAPlainReceipt()
    {
        Assert.Equal(
            "Attach the official receipt from the hotel or lodging under Receipts (as \"Lodging receipt\") before adding a Lodging line.",
            CategoryDocumentRules.LineProblem(ExpenseCategory.Lodging, [AttachmentDocumentType.Receipt], On));
        Assert.Null(CategoryDocumentRules.LineProblem(ExpenseCategory.Lodging, [AttachmentDocumentType.LodgingReceipt], On));
        Assert.Null(CategoryDocumentRules.LineProblem(ExpenseCategory.Lodging, [], On with { LodgingRequiresDocument = false }));
        Assert.Equal(
            ["A Lodging line needs an official receipt from the hotel or lodging attached. Upload it under Receipts as \"Lodging receipt\"."],
            CategoryDocumentRules.FilingProblems([ExpenseCategory.Lodging], [AttachmentDocumentType.Receipt], On));
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
    public void LodgingOff_IsDisabled_AndBlocksFiling()
    {
        var rules = On with { LodgingEnabled = false };

        Assert.Equal([ExpenseCategory.Lodging], CategoryDocumentRules.Disabled(rules));
        Assert.DoesNotContain(CategoryDocumentRules.Required(rules), r => r.Category == ExpenseCategory.Lodging);
        Assert.Equal(["Lodging claims are switched off. Remove or change those lines."],
            CategoryDocumentRules.FilingProblems([ExpenseCategory.Lodging], [], rules));
    }

    [Fact]
    public void GasAndVehicleEmergency_DefaultToVerifierAndReceipt()
    {
        Assert.True(CategoryDocumentRules.NeedsPersonVerification(On, ExpenseCategory.Gas));
        Assert.True(CategoryDocumentRules.NeedsPersonVerification(On, ExpenseCategory.VehicleEmergency));
        Assert.False(CategoryDocumentRules.NeedsPersonVerification(On, ExpenseCategory.Transportation));
        Assert.Empty(CategoryDocumentRules.FilingProblems(
            [ExpenseCategory.Gas, ExpenseCategory.VehicleEmergency],
            [AttachmentDocumentType.GasReceipt, AttachmentDocumentType.VehiclePurchaseReceipt, AttachmentDocumentType.JobOrder],
            On));
    }

    [Theory]
    [InlineData(CategoryVerification.None, false, false)]
    [InlineData(CategoryVerification.Person, true, false)]
    [InlineData(CategoryVerification.Document, false, true)]
    [InlineData(CategoryVerification.Both, true, true)]
    public void GasVerification_SetsPersonAndDocument(CategoryVerification mode, bool person, bool document)
    {
        var rules = On with { GasVerification = mode };

        Assert.Equal(person, CategoryDocumentRules.NeedsPersonVerification(rules, ExpenseCategory.Gas));
        Assert.Equal(document, CategoryDocumentRules.Required(rules).Any(r => r is { Category: ExpenseCategory.Gas, DocumentType: AttachmentDocumentType.GasReceipt }));
    }

    [Fact]
    public void VehicleEmergency_NeedsTheReceiptAndTheJobOrder()
    {
        Assert.Equal(
            "Attach the receipt for the vehicle purchase and the Job Order (JO), form R42 under Receipts "
                + "(as \"Vehicle purchase receipt\" and \"Job Order (JO)\") before adding a Vehicle emergency purchase line.",
            CategoryDocumentRules.LineProblem(ExpenseCategory.VehicleEmergency, [AttachmentDocumentType.Receipt], On));
        Assert.Equal(
            ["A Vehicle emergency purchase line needs a Job Order (JO), form R42 attached. Upload it under Receipts as \"Job Order (JO)\"."],
            CategoryDocumentRules.FilingProblems([ExpenseCategory.VehicleEmergency], [AttachmentDocumentType.VehiclePurchaseReceipt], On));
        Assert.NotNull(CategoryDocumentRules.LineProblem(ExpenseCategory.VehicleEmergency, [AttachmentDocumentType.JobOrder], On));
        Assert.Null(CategoryDocumentRules.LineProblem(
            ExpenseCategory.VehicleEmergency, [AttachmentDocumentType.VehiclePurchaseReceipt, AttachmentDocumentType.JobOrder], On));
        Assert.Empty(CategoryDocumentRules.FilingProblems(
            [ExpenseCategory.VehicleEmergency], [AttachmentDocumentType.VehiclePurchaseReceipt, AttachmentDocumentType.JobOrder], On));
    }

    [Fact]
    public void JobOrder_NeedsItsFormCode_R42ByDefault()
    {
        Assert.Equal("R42", CategoryDocumentRules.FormCodeFor(On, AttachmentDocumentType.JobOrder));
        Assert.Equal("R94", CategoryDocumentRules.FormCodeFor(On, AttachmentDocumentType.IsoAttendanceSheet));
        Assert.Null(CategoryDocumentRules.FormCodeFor(On, AttachmentDocumentType.VehiclePurchaseReceipt));
        Assert.Equal("This file does not show form R42. Upload the R42 Job Order, or confirm that this file is form R42.",
            CategoryDocumentRules.FormCodeNotFound(On, AttachmentDocumentType.JobOrder));
        Assert.Equal(["financeRules.jobOrderFormCode"], FinanceRuleSetRules.Validate(On with { JobOrderFormCode = " " }).Keys);
    }

    [Fact]
    public void GasOff_IsDisabled_AndNeedsNoDocument()
    {
        var rules = On with { GasEnabled = false };

        Assert.Equal([ExpenseCategory.Gas], CategoryDocumentRules.Disabled(rules));
        Assert.DoesNotContain(CategoryDocumentRules.Required(rules), r => r.Category == ExpenseCategory.Gas);
    }

    [Fact]
    public void UnknownVerificationMode_IsRefused()
    {
        var errors = FinanceRuleSetRules.Validate(On with { VehicleEmergencyVerification = (CategoryVerification)9 });

        Assert.Equal(["financeRules.vehicleEmergencyVerification"], errors.Keys);
    }

    [Theory]
    [InlineData("Client visit - Toyota Vios - ABC 1234", null)]
    [InlineData("Flat tire - CALMC - Toyota Vios - ABC 1234", null)]
    [InlineData("Client visit - Toyota Vios", "format")]
    [InlineData("Client visit -  - ABC 1234", "format")]
    [InlineData("Gas", "format")]
    public void VehicleParticulars_NeedReasonModelAndPlate(string particulars, string? expected)
    {
        Assert.Equal(expected is null, VehicleParticulars.Problem(ExpenseCategory.Gas, particulars) is null);
        Assert.Equal(expected is null, VehicleParticulars.Problem(ExpenseCategory.VehicleEmergency, particulars) is null);
        Assert.Null(VehicleParticulars.Problem(ExpenseCategory.Transportation, particulars));
    }

    [Fact]
    public void FilingBlockers_IncludeCategoryProblems()
    {
        var blockers = ReportWorkflowRules.FilingBlockers(
            ReportStatus.New, 1, 1, "Visit", ReportType.Reimbursement, 0m, 0m, categoryProblems: ["Needs a form."]);

        Assert.Equal(["Needs a form."], blockers);
    }
}
