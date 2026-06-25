using Recon.Domain;

namespace Recon.App.Ingestion;

/// CTMS activity ingestor: the three CTMS vendor adapters, registered behind the generic router.
public sealed class CtmsIngestor : CsvIngestor<Activity>
{
    public CtmsIngestor(IReadOnlyList<IDocumentAdapter<Activity>>? adapters = null)
        : base(adapters ?? [new RealTimeAdapter(), new CrioAdapter(), new ClinicalConductorAdapter()])
    { }
}
