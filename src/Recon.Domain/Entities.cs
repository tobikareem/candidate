namespace Recon.Domain;

public record Study(
    string Id,
    string StudyCode, // such as "MRD-204-017"
    string Protocol, // "HORIZON"
    string OutSlug,
    decimal HoldbackPct,
    decimal OverheadPct,
    int PaymentTermsDays,
    string Sponsor,
    string? Cro,
    IReadOnlyDictionary<string, decimal> Caps)
{
    public string SourceDocument { get; init; } = "";
    public string? SiteId { get; init; }
}

public record Site(string Id, string Name, string? Address, string? Number)
{
    public string SourceDocument { get; init; } = "";
}

public record Investigator(string Id, string Name, string StudyId)
{
    public string SourceDocument { get; init; } = "";
}

public record CtaBudgetLine(
    string Id,
    string StudyId,
    string VisitLabel,
    string? Procedure,
    decimal BaseAmount,
    CtaLineKind Kind,
    decimal? Cap)
{
    public string SourceDocument { get; init; } = "";
}

public record Activity(
    string Id,
    string StudyId,
    string SubjectId,
    string VisitLabelRaw,
    string VisitLabelNorm,
    DateOnly ServiceDate,
    ActivityStatus Status,
    Vendor SourceVendor,
    string? CtaLineId)
{
    public string SourceDocument { get; init; } = "";
}

public record Invoice(
    string Id,
    string PrintedNumber,
    string PrintedStudyCode,
    string Payor,
    string StudyId,
    string? SubjectId,
    DateOnly IssueDate,
    DateOnly ServiceDate,
    decimal FaceAmount,
    InvoiceKind Kind,
    IReadOnlyList<InvoiceLine> Lines)
{
    public string SourceDocument { get; init; } = "";
}

public record InvoiceLine(
    string Id,
    string InvoiceId,
    string? VisitLabel,
    string? ActivityId,
    decimal Amount);

public record Payment(
    string Id,
    string PrintedRef,
    string StudyId,
    string Payor,
    DateOnly Date,
    decimal Amount,
    PaymentMethod Method,
    Vendor SourceVendor,
    string? RemittanceId,
    string? BankTxnId)
{
    public string SourceDocument { get; init; } = "";
}

public record Remittance(
    string Id,
    string PrintedRef,
    string Payor,
    string StudyId,
    DateOnly Date,
    decimal NetPaid,
    IReadOnlyList<RemittanceLine> Lines)
{
    public string SourceDocument { get; init; } = "";
}

public record RemittanceLine(
    string Id,
    string RemittanceId,
    string? InvoiceRef,
    decimal Gross,
    decimal Adjustment,
    decimal Paid,
    string? ActivityId);

public record Autopay(
    string Id,
    string PrintedRef,
    string StudyId,
    string? SubjectId,
    string? VisitLabel,
    DateOnly ScheduledDate,
    decimal ScheduledAmount,
    AutopayStatus Status,
    Vendor SourceVendor,
    string? BankTxnId,
    decimal? DepositedAmount)
{
    public string SourceDocument { get; init; } = "";
}

public record BankTransaction(
    string Id,
    string ExternalId,
    DateOnly Date,
    string RawName,
    string Category,
    decimal Amount,
    bool IsDeposit,
    string? MatchedStudyId,
    string? MatchedTo)
{
    public string SourceDocument { get; init; } = "";
}

public record Comm(
    string Id,
    string Channel,
    DateOnly Date,
    string Body,
    IReadOnlyList<CommFact> Facts)
{
    public string SourceDocument { get; init; } = "";
}

public record CommFact(
    CommFactKind Kind,
    string TargetRef,
    string Note);
