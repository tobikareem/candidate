using Recon.App.Ingestion;
using Recon.Domain;

namespace Recon.Tests;

public class PortalBankIngestionTests
{
    private static readonly PaymentIngestor Payments = new();
    private static readonly AutopayIngestor Autopays = new();
    private static readonly BankIngestor Bank = new();

    [Fact]
    public void Ledgerrun_payment_captures_payor_method_and_amount()
    {
        const string csv =
            "PostedDate,Reference,Payor,Method,Amount,Status\n" +
            "2022-09-29,LR-001,Meridian Therapeutics,ACH,7354.69,Posted\n";

        var payment = Payments.Read(csv).Single();

        Assert.Equal(Vendor.LedgerRun, payment.SourceVendor);
        Assert.Equal("LR-001", payment.PrintedRef);
        Assert.Equal("Meridian Therapeutics", payment.Payor);
        Assert.Equal(PaymentMethod.Ach, payment.Method);
        Assert.Equal(7354.69m, payment.Amount);
    }

    [Fact]
    public void Ramp_payment_uses_vendor_as_payor()
    {
        const string csv =
            "Date,RampRef,Vendor,Amount\n" +
            "2026-01-20,RAMP-N00,Calyx Pharma,6284.50\n";

        var payment = Payments.Read(csv).Single();

        Assert.Equal(Vendor.Ramp, payment.SourceVendor);
        Assert.Equal("Calyx Pharma", payment.Payor);
        Assert.Equal(6284.50m, payment.Amount);
    }

    // --- Autopays 

    [Fact]
    public void Eclinicalgps_autopay_is_authorized_with_subject_and_amount()
    {
        const string csv =
            "AutopayID,SubjectID,Visit,ServiceDate,ScheduledAmount,Status\n" +
            "AP-009,S-33-003,V3,2026-04-20,523.10,Authorized\n";

        var autopay = Autopays.Read(csv).Single();

        Assert.Equal(Vendor.EclinicalGps, autopay.SourceVendor);
        Assert.Equal("S-33-003", autopay.SubjectId);
        Assert.Equal(523.10m, autopay.ScheduledAmount);
        Assert.Equal(AutopayStatus.Authorized, autopay.Status);
    }

    // --- Bank -

    [Fact]
    public void Plaid_marks_positive_as_deposit_and_negative_as_outflow()
    {
        const string csv =
            "date,name,amount,iso_currency_code,category,account_id,transaction_id\n" +
            "2022-07-05,Meridian Therapeutics,1937.82,USD,\"Transfer, Deposit\",acct_arp_operating_0001,BT-002\n" +
            "2022-05-02,ADP PAYROLL,-28450.00,USD,Payroll,acct_arp_operating_0001,PL-X001\n";

        var txns = Bank.Read(csv);

        var deposit = txns.Single(t => t.ExternalId == "BT-002");
        Assert.True(deposit.IsDeposit);
        Assert.Equal(1937.82m, deposit.Amount);
        Assert.Equal("Transfer, Deposit", deposit.Category);   // quoted comma survived parsing

        var payroll = txns.Single(t => t.ExternalId == "PL-X001");
        Assert.False(payroll.IsDeposit);
        Assert.Equal(-28450.00m, payroll.Amount);
    }

    // --- Real committed CSVs: verify entity counts -

    [Fact]
    public void Loads_real_payment_registers_with_expected_counts()
    {
        var ledger = Payments.Read(File.ReadAllText(Path.Combine(DocumentsDir(), "ledger_run_payment_register.csv")));
        var ramp = Payments.Read(File.ReadAllText(Path.Combine(DocumentsDir(), "ramp_bill_pay_register.csv")));

        Assert.Equal(11, ledger.Count);
        Assert.All(ledger, p => Assert.Equal(Vendor.LedgerRun, p.SourceVendor));
        Assert.Equal(2, ramp.Count);
        Assert.All(ramp, p => Assert.Equal(Vendor.Ramp, p.SourceVendor));
    }

    [Fact]
    public void Loads_real_autopay_register_with_expected_count()
    {
        var autopays = Autopays.Read(File.ReadAllText(Path.Combine(DocumentsDir(), "eclinicalgps_autopay_register.csv")));

        Assert.Equal(12, autopays.Count);
        Assert.All(autopays, a => Assert.Equal(Vendor.EclinicalGps, a.SourceVendor));
    }

    [Fact]
    public void Loads_real_bank_feed_and_distinguishes_deposits()
    {
        var txns = Bank.Read(File.ReadAllText(Path.Combine(DocumentsDir(), "plaid_transactions.csv")));

        Assert.Equal(67, txns.Count);
        Assert.Contains(txns, t => t.RawName == "Meridian Therapeutics" && t.Amount == 1937.82m && t.IsDeposit);
        Assert.Contains(txns, t => t.RawName == "ADP PAYROLL" && !t.IsDeposit);
    }

    private static string DocumentsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "documents")))
            dir = dir.Parent;

        return dir is not null
            ? Path.Combine(dir.FullName, "documents")
            : throw new DirectoryNotFoundException("Could not locate the repo's documents/ folder.");
    }
}
