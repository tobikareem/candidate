using Recon.Domain;
using Recon.App.Store;

namespace Recon.App.Classification;

public sealed class Classifier
{
    private readonly ReconStore _store;
    private readonly StudyResolver _resolver;

    public Classifier(ReconStore store, StudyResolver resolver)
    {
        _store = store;
        _resolver = resolver;
    }

    public IReadOnlyList<EntityScope> BuildEntityScopes()
    {
        var scopes = new List<EntityScope>();

        foreach (var invoice in _store.Invoices)
            scopes.Add(new EntityScope(EntityKind.Invoice, invoice.Id, ResolveInvoice(invoice).StudyId, SiteId: null, Investigator: null));

        foreach (var payment in _store.Payments)
            scopes.Add(new EntityScope(EntityKind.Payment, payment.Id, _resolver.Resolve(null, payment.Payor, null).StudyId, SiteId: null, Investigator: null));

        foreach (var remittance in _store.Remittances)
            scopes.Add(new EntityScope(EntityKind.Remittance, remittance.Id, ResolveRemittance(remittance).StudyId, SiteId: null, Investigator: null));

        foreach (var activity in _store.Activities)
            scopes.Add(new EntityScope(EntityKind.Activity, activity.Id, _resolver.Resolve(activity.SubjectId, null, null).StudyId, SiteId: null, Investigator: null));

        return scopes;
    }

    public IReadOnlyDictionary<string, string> StudyByEntityId()
    {
        var map = new Dictionary<string, string>();

        foreach (var invoice in _store.Invoices)
            map[invoice.Id] = ResolveInvoice(invoice).StudyId;

        foreach (var payment in _store.Payments)
            map[payment.Id] = _resolver.Resolve(null, payment.Payor, null).StudyId;

        foreach (var remittance in _store.Remittances)
            map[remittance.Id] = ResolveRemittance(remittance).StudyId;

        foreach (var activity in _store.Activities)
            map[activity.Id] = _resolver.Resolve(activity.SubjectId, null, null).StudyId;

        return map;
    }


    private StudyScope ResolveInvoice(Invoice invoice) =>
        _resolver.Resolve(invoice.SubjectId, null, invoice.PrintedStudyCode);

    private StudyScope ResolveRemittance(Remittance remittance)
    {
        var scope = _resolver.Resolve(null, remittance.Payor, null);
        if (scope.StudyId.Length != 0 || remittance.Lines.Count == 0)
            return scope;

        var line = remittance.Lines[0];
        var invoice = _store.Invoices.SingleOrDefault(
            inv => inv.PrintedNumber == line.InvoiceRef && inv.FaceAmount == line.Gross);

        return invoice is null ? scope : ResolveInvoice(invoice);
    }
}
