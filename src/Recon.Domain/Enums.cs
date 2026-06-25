namespace Recon.Domain;

public enum Vendor{
     Unknown = 0,
    RealTime, Crio, ClinicalConductor,   // CTMS (activities)
    LedgerRun, EclinicalGps, Ramp,       // payment portals
}

public enum PaymentMethod { Unknown = 0, Wire, Check, Ach, Card, Autopay }

public enum InvoiceKind { Unknown = 0, SubjectVisit, SiteFee }

public enum CtaLineKind { Unknown = 0, Visit, Procedure, SiteFee, Overhead, Holdback }

public enum MatchConfidence { Low = 0, Medium, High }

public enum InvoiceStatus { Unpaid = 0, Partial, Paid }

public enum ActivityStatus { Unknown = 0, Completed, Scheduled, Missed, AdminLogOnly }

public enum AutopayStatus { Unknown = 0, Authorized, Deposited, Failed }

// this is coming from slack, email
public enum CommFactKind { Clarifies = 0, ConfirmsPaid, ConfirmsUnpaid, Dispute }

public enum UnpaidReason { SentNotPaid = 0, AutopayNoDeposit }

public enum ExceptionKind { CapBreach = 0, WrongAmount, Dispute, Duplicate, Other }
