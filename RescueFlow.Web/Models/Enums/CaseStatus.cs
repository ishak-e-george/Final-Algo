namespace RescueFlow.Web.Models.Enums;

public enum CaseStatus
{
    Pending,
    Analyzing,
    ResponsePlanGenerated,
    AutoAssigned,
    BlockedNoSafePlan,
    Escalated,
    InProgress,
    Resolved,
    Cancelled,
    Closed
}
