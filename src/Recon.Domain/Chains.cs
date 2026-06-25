namespace Recon.Domain;

public enum EntityKind { Invoice = 0, Payment, Remittance, Activity }

/// Chain 1 — payment maps to the remittance
public record PaymentToRemittance(
    string PaymentId,
    IReadOnlyList<string> RemittanceIds);

/// Chain 2 — invoice aggregates the payments that cleared it
public record InvoiceToPayment(
    string InvoiceId,
    IReadOnlyList<string> PaymentIds,
    decimal InvoiceAmount,
    decimal AmountSettled,
    InvoiceStatus Status);

/// Chain 3 — invoice links to the activities it bills.
public record InvoiceToActivities(
    string InvoiceId,
    IReadOnlyList<string> ActivityIds);

/// Chain 4 — remittance splits across activities.
public record RemittanceToActivities(
    string RemittanceId,
    IReadOnlyList<RemittanceAllocation> Lines);

public record RemittanceAllocation(
    string? ActivityId,
    string? InvoiceId,
    decimal AmountAllocated);

/// Chain 5 — activity maps to a CTA budget line. 
public record ActivityToCta(
    string ActivityId,
    string? CtaVisitLabel,
    decimal? CtaAmount,
    MatchConfidence? MatchConfidence);

/// Chain 6 — which study (and site/investigator) each entity was resolved to. 
/// This is the output of classification: the audit trail proving every document was scoped to the right place.
public record EntityScope(
    EntityKind EntityType,
    string EntityId,
    string StudyId,
    string? SiteId,
    string? Investigator);
