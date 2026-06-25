using System.Globalization;
using Recon.Domain;

namespace Recon.App.Ingestion;

public sealed class LedgerRunAdapter : IPaymentAdapter
{
    public bool Handles(IReadOnlyList<string> header) =>
        HeaderSignature.HasAll(header, "PostedDate", "Reference", "Payor", "Method", "Amount");

    public IEnumerable<Payment> Read(CsvTable table)
    {
        foreach (var row in table.Rows)
            yield return new Payment(
                Id: SurrogateKey.For("payment", "ledgerrun", row["Reference"]),
                PrintedRef: row["Reference"],
                StudyId: "",
                Payor: row["Payor"],
                Date: PortalMap.Date(row["PostedDate"]),
                Amount: PortalMap.Money(row["Amount"]),
                Method: PortalMap.PayMethod(row["Method"]),
                SourceVendor: Vendor.LedgerRun,
                RemittanceId: null,
                BankTxnId: null);
    }
}

public sealed class RampAdapter : IPaymentAdapter
{
    public bool Handles(IReadOnlyList<string> header) =>
        HeaderSignature.HasAll(header, "Date", "RampRef", "Vendor", "Amount");

    public IEnumerable<Payment> Read(CsvTable table)
    {
        foreach (var row in table.Rows)
            yield return new Payment(
                Id: SurrogateKey.For("payment", "ramp", row["RampRef"]),
                PrintedRef: row["RampRef"],
                StudyId: "",
                Payor: row["Vendor"],
                Date: PortalMap.Date(row["Date"]),
                Amount: PortalMap.Money(row["Amount"]),
                Method: PaymentMethod.Unknown,   // the Ramp register doesn't record a payment method
                SourceVendor: Vendor.Ramp,
                RemittanceId: null,
                BankTxnId: null);
    }
}

public sealed class EclinicalGpsAdapter : IAutopayAdapter
{
    public bool Handles(IReadOnlyList<string> header) =>
        HeaderSignature.HasAll(header, "AutopayID", "SubjectID", "Visit", "ServiceDate", "ScheduledAmount", "Status");

    public IEnumerable<Autopay> Read(CsvTable table)
    {
        foreach (var row in table.Rows)
            yield return new Autopay(
                Id: SurrogateKey.For("autopay", "eclinicalgps", row["AutopayID"]),
                PrintedRef: row["AutopayID"],
                StudyId: "",
                SubjectId: row["SubjectID"],
                VisitLabel: Text.NormalizeLabel(row["Visit"]),
                ScheduledDate: PortalMap.Date(row["ServiceDate"]),
                ScheduledAmount: PortalMap.Money(row["ScheduledAmount"]),
                Status: PortalMap.AutopayState(row["Status"]),
                SourceVendor: Vendor.EclinicalGps,
                BankTxnId: null,
                DepositedAmount: null);
    }
}

public sealed class PaymentIngestor : CsvIngestor<Payment>
{
    public PaymentIngestor(IReadOnlyList<IDocumentAdapter<Payment>>? adapters = null)
        : base(adapters ?? [new LedgerRunAdapter(), new RampAdapter()])
    { }
}

public sealed class AutopayIngestor : CsvIngestor<Autopay>
{
    public AutopayIngestor(IReadOnlyList<IDocumentAdapter<Autopay>>? adapters = null)
        : base(adapters ?? [new EclinicalGpsAdapter()])
    { }
}

internal static class PortalMap
{
    public static DateOnly Date(string iso) =>
        DateOnly.ParseExact(iso.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static decimal Money(string raw) =>
        decimal.Parse(raw.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture);

    public static PaymentMethod PayMethod(string raw) => raw.Trim().ToLowerInvariant() switch
    {
        "ach"     => PaymentMethod.Ach,
        "check"   => PaymentMethod.Check,
        "wire"    => PaymentMethod.Wire,
        "card"    => PaymentMethod.Card,
        "autopay" => PaymentMethod.Autopay,
        _         => PaymentMethod.Unknown,
    };

    public static AutopayStatus AutopayState(string raw) => raw.Trim().ToLowerInvariant() switch
    {
        "authorized" => AutopayStatus.Authorized,
        "deposited"  => AutopayStatus.Deposited,
        "failed"     => AutopayStatus.Failed,
        _            => AutopayStatus.Unknown,
    };
}
