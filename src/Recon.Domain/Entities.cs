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
    IReadOnlyDictionary<string, decimal> Caps); 

public record Site(string Id, string Name, string? Address, string? Number);

public record Investigator(string Id, string Name, string StudyId);

public record CtaBudgetLine(
    string Id,
    string StudyId,
    string VisitLabel,
    string? Procedure,
    decimal BaseAmount, // CTA list price before any overhead markup
    CtaLineKind Kind,
    decimal? Cap);

public record Activity(
    string Id,
    string StudyId,
    string SubjectId,
    string VisitLabelRaw,   
    string VisitLabelNorm,
    DateOnly ServiceDate,
    ActivityStatus Status,
    Vendor SourceVendor,
    string? CtaLineId);

public record Invoice(
    string Id,
    string PrintedNumber,
    string StudyId,
    string? SubjectId,
    DateOnly IssueDate,
    decimal FaceAmount,
    InvoiceKind Kind,
    IReadOnlyList<InvoiceLine> Lines);

public record InvoiceLine(
    string Id,
    string InvoiceId,
    string? VisitLabel,
    string? ActivityId, 
    decimal Amount);

public record Payment(
    string Id,
    string StudyId,
    DateOnly Date,
    decimal Amount,
    PaymentMethod Method,
    string? RemittanceId,
    string? BankTxnId);

public record Remittance(
    string Id,
    string StudyId,
    DateOnly Date,
    decimal NetPaid,
    IReadOnlyList<RemittanceLine> Lines);

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
    string StudyId,
    string? SubjectId,
    DateOnly ScheduledDate,
    decimal ScheduledAmount,
    AutopayStatus Status,
    string? BankTxnId,
    decimal? DepositedAmount);


public record BankTransaction(
    string Id,
    DateOnly Date,
    string RawName,
    decimal Amount,
    bool IsDeposit,
    string? MatchedStudyId,
    string? MatchedTo); 

public record Comm(
    string Id,
    string Channel,
    DateOnly Date,
    string Body,
    IReadOnlyList<CommFact> Facts);

public record CommFact(
    CommFactKind Kind,
    string TargetRef, 
    string Note);
