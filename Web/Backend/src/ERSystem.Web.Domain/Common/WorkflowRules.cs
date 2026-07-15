namespace ERSystem.Web.Domain.Common;

public static class ReportStates
{
    public const string Filed = "1";
    public const string Approved = "0";
    public const string EndorseApproved = "APPROVED";
    public const string EndorseNotApproved = "NOT APPROVED";
}

public static class FinanceStates
{
    public const string Pending = "Pending";
    public const string ReceiptsReceived = "Receipts Received";
}

public static class ApprovalTransactionStates
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Returned = "Returned";
    public const string Superseded = "Superseded";
}

public static class WorkflowEvents
{
    public const string ManagerApproved = "ManagerApproved";
    public const string ManagerReturned = "ManagerReturned";
    public const string PhysicalReceiptsReceived = "PhysicalReceiptsReceived";
}

public static class ApprovalSequence
{
    public static bool CanApprove(int managerSort, IReadOnlyCollection<int> completedSorts) =>
        managerSort > 0 && Enumerable.Range(1, managerSort - 1).All(completedSorts.Contains);
}
