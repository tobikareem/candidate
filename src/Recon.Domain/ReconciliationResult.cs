namespace Recon.Domain;

public enum UnpaidRefType { Invoice = 0, Autopay }

/// dashboard.json : the per-study summary.
public record Dashboard(
    string StudyId,
    string? SiteId,
    string? Investigator,
    decimal TotalBilled,
    decimal TotalCollected,
    decimal OutstandingAr,
    decimal HoldbackWithheld,
    decimal UnbilledEstimate,
    int ExceptionsCount,
    decimal? AvgDaysToPayment);

/// One unbilled.json row: work with evidence it happened but no invoice and no autopay. 
public record UnbilledItem(
    string SubjectId,
    string? ProposedVisitLabel,
    decimal EstimatedAmount,
    string? CtaBasis,
    string? Evidence,
    MatchConfidence? Confidence);

/// One unpaid.json row: an invoice or autopay expected but never collected. 
public record UnpaidItem(
    UnpaidRefType RefType,
    string RefId,
    decimal AmountExpected,
    int? AgeDays,
    UnpaidReason Reason,
    string? Evidence,
    MatchConfidence? Confidence);

public record ReconException(
    ExceptionKind Kind,
    string TargetRef,
    string Evidence);

/// The whole reconciliation for one study:Carries the full Study
public record ReconciliationResult(
    Study Study,
    string? SiteId,
    string? Investigator,
    IReadOnlyList<PaymentToRemittance> PaymentToRemittance,
    IReadOnlyList<InvoiceToPayment> InvoiceToPayment,
    IReadOnlyList<InvoiceToActivities> InvoiceToActivities,
    IReadOnlyList<RemittanceToActivities> RemittanceToActivities,
    IReadOnlyList<ActivityToCta> ActivityToCta,
    IReadOnlyList<EntityScope> EntityScope,
    Dashboard Dashboard,
    IReadOnlyList<UnbilledItem> Unbilled,
    IReadOnlyList<UnpaidItem> Unpaid,
    IReadOnlyList<ReconException> Exceptions);
